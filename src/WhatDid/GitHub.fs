module GitHub

open FSharp.Control.Tasks.V2.ContextInsensitive
open Types
open System
open System.Net.Http
open System.Threading.Tasks
open FSharp.Control

let private _httpClient =
  let c = new HttpClient()
  c.DefaultRequestHeaders.Add ("User-Agent", "whatdid")
  c.DefaultRequestHeaders.Add ("Accept", "application/vnd.github.v3+json")
  c
let private _perPage = 100

let private (|HasEverything|_|) (parts: Parts) =
  match parts with
  | { owner = Some owner
      repo = Some repo
      baseRev = Some baseRev
      headRev = Some headRev } -> Some (owner, repo, baseRev, headRev)
  | _ -> None

let private failMissingPieces (parts: Parts) =
  eprintfn "WARNING: Must have values for owner, repo, baseRev, headRev. %A" parts
  exn "FIXME"

type CommitListNestedObj = { message: string }
type CommitListOuterObj = { sha: string; commit: CommitListNestedObj }

type PaginatedState =
  { items: CommitListOuterObj list
    next: Uri option }
  with
    static member Init uri =
      { items = []
        next = Some uri }

open Newtonsoft.Json
open System
open System.Net.Http.Headers

/// From GitHub docs (https://developer.github.com/v3/#pagination):
/// Link: <https://api.github.com/resource?page=2>; rel="next",
///       <https://api.github.com/resource?page=5>; rel="last"
let private _tryGetNextUrl (headers: HttpHeaders) =
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

let private _getPaginated (oauthToken: string option) (initialUri: Uri) =
  initialUri
  |> Some
  |> AsyncSeq.unfoldAsync
      (function
       | None -> async { return None }
       | Some (uri: Uri) ->
           task {
             use request = new HttpRequestMessage (HttpMethod.Get, uri)
             oauthToken
             |> Option.map (sprintf "token %s")
             |> Option.iter (fun t -> request.Headers.Add ("Authorization", t))
             printfn "GET %A" uri
             use! response = _httpClient.SendAsync (request, HttpCompletionOption.ResponseHeadersRead)
             let! json = response.Content.ReadAsStringAsync ()
             let commits = JsonConvert.DeserializeObject<CommitListOuterObj list> json
             let nextUri = _tryGetNextUrl response.Headers

             return Some (commits, nextUri)
           }
           |> Async.AwaitTask
      )

let getAllPrMergeCommitsInRange (oauthToken: string option) (parts: Parts) =
  match parts with
  | HasEverything (owner, repo, baseRev, headRev) ->
      sprintf "https://api.github.com/repos/%s/%s/commits?sha=%s&page=1&per_page=%u" owner repo headRev _perPage
      |> Uri
      |> _getPaginated oauthToken
      |> AsyncSeq.takeWhileInclusive (fun commits ->
          commits
          |> List.exists (fun x -> x.sha.StartsWith baseRev)
          |> not
      )
      |> AsyncSeq.map (fun commits ->
          commits

          |> List.takeWhile (fun c ->
              match parts.headRev with
              | None -> true
              | Some headRev -> not <| c.sha.StartsWith baseRev
          )
          |> List.filter (fun c -> c.commit.message.Contains "Merge pull request #")
      )
      |> AsyncSeq.filter (not << List.isEmpty)
  | _ ->
      raise <| failMissingPieces parts
