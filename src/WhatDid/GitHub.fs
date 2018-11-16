module GitHub

open FSharp.Control.Tasks.V2.ContextInsensitive
open Types
open System
open System.Net.Http
open System.Threading.Tasks

let private _httpClient =
  let c = new HttpClient()
  c.DefaultRequestHeaders.Add ("User-Agent", "whatdid")
  c.DefaultRequestHeaders.Add ("Accept", "application/vnd.github.v3+json")
  c
let private _perPage = 100

let PLACEHOLDER_getCommitJson (oauthToken: string option) (parts: Parts) =
  match parts with
  | { owner = Some owner
      repo = Some repo
      baseRev = Some baseRev } ->
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
