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

let private (|HasBareMinimum|_|) (parts: Parts) =
  match parts with
  | { owner = Some owner
      repo = Some repo
      baseRev = Some baseRev } -> Some (owner, repo, baseRev)
  | _ -> None

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
  |> fun x ->
      printfn "____________ count: %i" (x |> Seq.length)
      printfn "____________ values: %A" x;

      x
  |> List.ofSeq
  |> List.tryFind (fun str -> str.EndsWith "rel=\"next\"")
  |> Option.map (fun str ->
      str
      |> Seq.skipWhile (fun ch -> ch = '<')
      |> Seq.takeWhile (fun ch -> ch <> '>')
      |> Seq.toArray
      |> String
      |> Uri
  )
  |> fun x -> printfn "____________ next: %A" x; x

let private _getCommits oauthToken (state: PaginatedState) =
  match state.next with
  | None -> Task.FromResult { state with items = [] }
  | Some next ->
      task {
        use request = new HttpRequestMessage (HttpMethod.Get, next)
        oauthToken
        |> Option.map (sprintf "token %s")
        |> Option.iter (fun t -> request.Headers.Add ("Authorization", t))

        use! response = _httpClient.SendAsync (request, HttpCompletionOption.ResponseHeadersRead)
        let! json = response.Content.ReadAsStringAsync ()

        return
          { items = JsonConvert.DeserializeObject<CommitListOuterObj list> json
            next = _tryGetNextUrl response.Headers }
      }

let rec private _getCommitsRec oauthToken (state: PaginatedState) =
  asyncSeq {
    match! _getCommits oauthToken state |> Async.AwaitTask with
    | { next = Some _ } as nextState ->
        yield nextState.items
        yield! _getCommitsRec oauthToken nextState
    | nextState ->
        yield nextState.items
  }

let getAllCommitsInRange (oauthToken: string option) (parts: Parts) =
  match parts with
  | HasBareMinimum (owner, repo, baseRev) ->
      let initialState =
        sprintf "https://api.github.com/repos/%s/%s/commits?sha=%s&page=1&per_page=%u" owner repo baseRev _perPage
        |> Uri
        |> PaginatedState.Init

      initialState
      |> _getCommitsRec oauthToken
      |> AsyncSeq.map (fun xs ->
          xs |> List.filter (fun c -> c.commit.message.Contains "Merge pull request #")
      )
      |> AsyncSeq.filter (not << List.isEmpty)
      |> AsyncSeq.iter (printfn "!!!!! commits: %A")
  | _ ->
      eprintfn "WARNING: Must have values for owner, repo, baseRev. %A" parts
      failwith "FIXME"

let PLACEHOLDER_getCommitJson (oauthToken: string option) (parts: Parts) =
  match parts with
  | { owner = Some owner
      repo = Some repo
      baseRev = Some baseRev } ->
        getAllCommitsInRange oauthToken parts
        |> Async.RunSynchronously
        |> ignore

        task {
          let url = sprintf "https://api.github.com/repos/%s/%s/commits/%s" owner repo baseRev
          use message = new HttpRequestMessage (HttpMethod.Get, url)
          oauthToken
          |> Option.map (sprintf "token %s")
          |> Option.iter (fun t -> message.Headers.Add ("Authorization", t))
          use! resp = _httpClient.SendAsync (message, HttpCompletionOption.ResponseHeadersRead)
          let! json = resp.Content.ReadAsStringAsync ()

          if json.Length <= 100 then return json
          else
            return json.Substring (0, 100) |> sprintf "%s..."
        }
  | _ -> Task.FromResult "FIXME"
