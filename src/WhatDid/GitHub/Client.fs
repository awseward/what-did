module GitHub.Client

open FSharp.Control
open FSharp.Control.Tasks.V2.ContextInsensitive
open GitHub.Http
open GitHub.Types
open System
open Types

let tryGetRepoAsync oauthToken owner repo =
  sprintf "https://api.github.com/repos/%s/%s" owner repo
  |> Uri
  |> tryGetAsync<Repo> oauthToken

let tryGetPullRequestAsync oauthToken owner repo prNumber =
  sprintf "https://api.github.com/repos/%s/%s/pulls/%i" owner repo prNumber
  |> Uri
  |> tryGetAsync<PullRequestResp> oauthToken

let getAllPrMergeCommitsInRange oauthToken parts =
  let { FullParts.owner = owner; repo = repo } = parts
  let baseRev = Revision.GetSha parts.baseRevision
  let headRev = Revision.GetSha parts.headRevision

  let isBaseRev (c: CommitListOuterObj) = c.sha.StartsWith baseRev
  let isPullMerge (c: CommitListOuterObj) = c.commit.message.StartsWith "Merge pull request #"

  sprintf "https://api.github.com/repos/%s/%s/commits?_base_=%s&_head_=sha&sha=%s&page=1&per_page=%u" owner repo baseRev headRev perPage
  |> Uri
  |> getPaginated oauthToken
  |> AsyncSeq.takeWhileInclusive (not << (List.exists isBaseRev))
  |> AsyncSeq.map (fun commits ->
      commits
      |> List.takeWhile (not << isBaseRev)
      |> List.filter isPullMerge
  )
  |> AsyncSeq.filter (not << List.isEmpty)

/// This method tries to resolve some type of revision using `rawRevisionName`.
///
/// Tags or commit SHAs are preferred, as they're less ambiguous than branch
/// names. It should be more or less safe to assume that there won't be
/// collisions between the two, as that would likely make git usage problematic
/// in general.
///
/// If `rawRevisionName` does not resolve to either a SHA or a tag, we fall back
/// to assume that `rawRevisionName` must then be a branch name, and we attempt
/// to fetch the SHA for the head of that branch.
let disambiguateAsync oauthToken owner repo rawRevisionName =
  // FIXME: will probably need to get some kind of error handling in place
  // on the HTTP requests in here.
  let tryShowCommitAsync () = task {
    let uri = Uri <| sprintf "https://api.github.com/repos/%s/%s/commits/%s" owner repo rawRevisionName
    let! objOpt = uri |> tryGetAsync<HasSha> oauthToken

    return
      objOpt
      |> Option.map (fun { HasSha.sha = sha } -> Commit (CommitSha sha))
  }
  let tryShowTagAsync () = task {
    let uri = Uri <| sprintf "https://api.github.com/repos/%s/%s/git/refs/tags/%s" owner repo rawRevisionName
    let! objOpt = uri |> tryGetAsync<TagResp> oauthToken

    return
      objOpt
      |> Option.map (fun { object = { sha = sha } } -> Tag (TagName rawRevisionName, CommitSha sha))
  }
  let tryShowBranchAsync () = task {
    let uri = Uri <| sprintf "https://api.github.com/repos/%s/%s/branches/%s" owner repo rawRevisionName
    let! objOpt = uri |> tryGetAsync<BranchResp> oauthToken

    return
      objOpt
      |> Option.map (fun { commit = { sha = sha } } -> Branch (BranchName rawRevisionName, CommitSha sha))
  }

  task {
    let tasks = (tryShowCommitAsync(), tryShowTagAsync(), tryShowBranchAsync())
    let! commit =
      let task, _, _ = tasks
      task
    let! tag =
      let _, task, _ = tasks
      task

    match commit, tag with
    | Some _ as result, None               -> return result
    // Apparently GitHub will happily give you back a commit even if you do something like this:
    // ```
    // GET https://api.github.com/repos/:owner:/:repo:/commits/:tag_name:
    // ```
    | _               , (Some _ as result) -> return result
    | None            , None               ->
        let _, _, task = tasks
        return! task
  }
