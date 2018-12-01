module GitHub

open FSharp.Control
open FSharp.Control.Tasks.V2.ContextInsensitive
open Newtonsoft.Json
open Types
open System
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks

let private _httpClient =
  let c = new HttpClient()
  c.DefaultRequestHeaders.Add ("User-Agent", "whatdid")
  c.DefaultRequestHeaders.Add ("Accept", "application/vnd.github.v3+json")
  c
let private _perPage = 100
let private _createGet (oauthToken: string option) (uri: Uri) =
  let req = new HttpRequestMessage (HttpMethod.Get, uri)
  oauthToken |> Option.iter (fun token -> req.Headers.Add ("Authorization", sprintf "token %s" token))
  req
let private _sendAsync request =
  _httpClient.SendAsync (request, HttpCompletionOption.ResponseHeadersRead)
let private _deserializeAsJsonAsync<'a> (response: HttpResponseMessage) =
  task {
    let! json = response.Content.ReadAsStringAsync ()
    return JsonConvert.DeserializeObject<'a> json
  }
let private _getAsync<'a> (oauthToken: string option) (uri: Uri) =
  task {
    use req = _createGet oauthToken uri
    let! response = _sendAsync req
    return! _deserializeAsJsonAsync<'a> response
  }

module private Temp =
  let (|HasEverything|_|) (parts: Parts) =
    match parts with
    | { owner = Some owner
        repo = Some repo
        baseRev = Some baseRev
        headRev = Some headRev } -> Some (owner, repo, baseRev, headRev)
    | _ -> None
  let failMissingPieces (parts: Parts) =
    eprintfn "WARNING: Must have values for owner, repo, baseRev, headRev. %A" parts
    exn "FIXME"

open Temp

type CommitListNestedObj = { message: string }
type CommitListOuterObj = { sha: string; commit: CommitListNestedObj }

module Pagination =
  /// From GitHub docs (https://developer.github.com/v3/#pagination):
  /// Link: <https://api.github.com/resource?page=2>; rel="next",
  ///       <https://api.github.com/resource?page=5>; rel="last"
  let tryGetNextUrl (headers: HttpHeaders) : Uri option =
    "Link"
    |> headers.GetValues
    |> Seq.exactlyOne
    |> fun str -> str.Split (',', StringSplitOptions.RemoveEmptyEntries)
    |> Seq.map (fun str -> str.Trim ())
    |> Seq.tryFind (fun str -> str.EndsWith "rel=\"next\"")
    |> Option.map (fun str ->
        str
        |> Seq.skipWhile (fun ch -> ch = '<')
        |> Seq.takeWhile (fun ch -> ch <> '>')
        |> Seq.toArray
        |> String
        |> Uri
    )

  let getPaginated (reqF: Uri -> HttpRequestMessage) (deserializeAsync: HttpResponseMessage -> Task<'a list>) (initialUri: Uri) =
    initialUri
    |> Some
    |> AsyncSeq.unfoldAsync
        (function
         | None -> async { return None }
         | Some (uri: Uri) ->
             task {
               use req = reqF uri
               printfn "%s %A" req.Method.Method uri
               use! response = _sendAsync req
               let! items = deserializeAsync response

               return Some (items, tryGetNextUrl response.Headers)
             }
             |> Async.AwaitTask
         )

let private _getPaginated<'a> (oauthToken: string option) =
  Pagination.getPaginated
    (_createGet oauthToken)
    _deserializeAsJsonAsync<'a list>

let getAllPrMergeCommitsInRange (oauthToken: string option) (parts: Parts) =
  match parts with
  | HasEverything (owner, repo, baseRev, headRev) ->
      let isBaseRev c = c.sha.StartsWith baseRev
      let isPullMerge c = c.commit.message.Contains "Merge pull request #"

      sprintf "https://api.github.com/repos/%s/%s/commits?sha=%s&page=1&per_page=%u" owner repo headRev _perPage
      |> Uri
      |> _getPaginated oauthToken
      |> AsyncSeq.takeWhileInclusive (not << (List.exists isBaseRev))
      |> AsyncSeq.map (fun commits ->
          commits
          |> List.takeWhile (not << isBaseRev)
          |> List.filter isPullMerge
      )
      |> AsyncSeq.filter (not << List.isEmpty)
  | _ ->
      raise <| failMissingPieces parts

type private HasSha = { sha: string }
type private BranchResp = { commit: HasSha }
type private TagResp = { object: HasSha }

let resolveCommitShaAsync (oauthToken: string option) (owner: string) (repo: string) =
  function
  | Commit sha -> Task.FromResult sha
  | Branch (BranchName name) ->
      let uri = Uri <| sprintf "https://api.github.com/repos/%s/%s/branches/%s" owner repo name
      task {
        let! { commit = { sha = sha } } = uri |> _getAsync<BranchResp> oauthToken
        return CommitSha sha
      }
  | Tag (TagName name) ->
      let uri = Uri <| sprintf "https://api.github.com/repos/%s/%s/git/refs/tags/%s" owner repo name
      task {
        let! { object = { sha = sha } } = uri |> _getAsync<TagResp> oauthToken
        return CommitSha sha
      }
