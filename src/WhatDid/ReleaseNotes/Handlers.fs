module ReleaseNotes.Handlers

open FSharp.Control
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe.Core
open Giraffe.ResponseWriters
open GitHub.Client
open GitHub.Types
open Microsoft.AspNetCore.Authentication
open System
open System.Threading.Tasks
open Types
open Types.Temp

let private _getTokenAsync (ctx: Microsoft.AspNetCore.Http.HttpContext) =
  task {
    let! t = ctx.GetTokenAsync "access_token"

    if String.IsNullOrWhiteSpace t then return None
    else return Some t
  }

let rec private _disambiguatePartsAsync oauthToken (rawParts: RawParts) =
  match rawParts with
  | UseDefaultBranch (owner, repo, baseRev) ->
      task {
        let! repoObjOpt = GitHub.Client.tryGetRepoAsync oauthToken owner repo

        match repoObjOpt with
        | None -> return! Task.FromException<FullParts> (exn "FIXME")
        | Some repoObj ->
            let headRev = Some repoObj.default_branch
            return! _disambiguatePartsAsync oauthToken { rawParts with headRev = headRev }
      }
  | Full (owner, repo, baseRev, headRev) ->
      let disambiguateAsync = GitHub.Client.disambiguateAsync oauthToken owner repo
      task {
        let tasks = (disambiguateAsync baseRev, disambiguateAsync headRev)
        let! uBaseOpt = fst tasks
        let! uHeadOpt = snd tasks

        match uBaseOpt, uHeadOpt with
        | _, None
        | None, _ -> return! Task.FromException<FullParts> (exn "FIXME")
        | Some baseRev, Some headRev ->
            return
              { owner = owner
                repo = repo
                baseRevision = baseRev
                headRevision = headRev }
      }
  | _ ->
      raise (notFullExn rawParts)


let private _getPrNumFromCommitMessage (message: string) =
  message
  |> Seq.skipWhile (fun ch -> ch <> '#')
  |> Seq.skipWhile (fun ch -> ch = '#')
  |> Seq.takeWhile (fun ch -> ch <> ' ')
  |> Seq.toArray
  |> String
  |> Int32.Parse

let private _getPRsAsync oauthToken (parts: FullParts) =
  async {
    let! prs =
      parts
      |> GitHub.Client.getAllPrMergeCommitsInRange oauthToken
      |> AsyncSeq.toBlockingSeq
      |> Seq.collect id
      |> Seq.map (fun x ->
          x.commit.message
          |> _getPrNumFromCommitMessage
          |> tryGetPullRequestAsync oauthToken parts.owner parts.repo
      )
      |> Seq.map Async.AwaitTask
      |> Async.Parallel

    return
      prs
      |> Seq.choose id
      |> Seq.sort
      |> List.ofSeq
  }
  |> Async.StartAsTask

let private _tempFilterAndWarn (prs: PullRequestResp list) =
  let titleOrUrlIsNull (pr: PullRequestResp) = isNull pr.title || isNull pr.html_url
  if List.exists titleOrUrlIsNull prs then
    eprintfn "WARNING: Fetched at least one Pull Request with null title or html_url"
  List.filter (not << titleOrUrlIsNull) prs

let notesHandler parts : HttpHandler = (fun next ctx ->
  task {
    let! oauthToken = _getTokenAsync ctx
    let! parts' = _disambiguatePartsAsync oauthToken parts
    let! prs = _getPRsAsync oauthToken parts'
    let xmlNode = Views.notes parts' (_tempFilterAndWarn prs)

    return! (htmlView xmlNode next ctx)
  }
)
let formHandler parts = parts |> (Views.form >> htmlView)
