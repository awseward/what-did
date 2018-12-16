module GitHub.Http

open FSharp.Control
open FSharp.Control.Tasks.V2.ContextInsensitive
open Newtonsoft.Json
open Types
open System
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Net
open System.Collections.Concurrent
open Envars
open StackExchange.Redis


let (|Http2xx|_|) (response: HttpResponseMessage) =
  let statusCode = int response.StatusCode
  if 200 <= statusCode && statusCode < 300 then Some (response.StatusCode)
  else
    None
let (|Http304|_|) (response: HttpResponseMessage) =
  if (int response.StatusCode) = 304 then Some (response.StatusCode)
  else
    None

let inline (!>) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> ^b) x)

module RedisCache =
  let tryGet (db: IDatabase) (uri: Uri) : (DateTimeOffset * string) option =
    raise (exn "TODO")
  let addOrUpdate (db: IDatabase) (uri: Uri) (value': (DateTimeOffset * string)) : unit =
    raise (exn "TODO")

  let tryRead db uri (req: HttpRequestMessage) =
    uri
    |> tryGet db
    |> Option.map (fun (lastModified, json) ->
        req.Headers.IfModifiedSince <- Nullable lastModified
        json
    )
  let tryWrite db uri json (resp: HttpResponseMessage) =
    resp.Content.Headers.LastModified
    |> Option.ofNullable
    |> Option.iter (fun lastModified -> addOrUpdate db uri (lastModified, json))

[<Obsolete("Prefer RedisCache")>]
module Cache =
  let dict = ConcurrentDictionary<Uri, DateTimeOffset * string> ()
  let tryGet uri =
    let mutable value = (DateTimeOffset.MinValue, null)
    if dict.TryGetValue (uri, &value)
    then Some value
    else None
  let addOrUpdate (uri: Uri) ((lastModified', _) as value') =
    dict.AddOrUpdate (
      uri,
      value',
      (fun _ (lastModified, _ as value) ->
        if lastModified' > lastModified then value'
        else value
      )
    )
    |> ignore

  let tryRead uri (req: HttpRequestMessage) =
    uri
    |> tryGet
    |> Option.map (fun (lastModified, json) ->
        req.Headers.IfModifiedSince <- Nullable lastModified
        json
    )
  let tryWrite uri json (resp: HttpResponseMessage) =
    resp.Content.Headers.LastModified
    |> Option.ofNullable
    |> Option.iter (fun lastModified -> addOrUpdate uri (lastModified, json))

module RedisPaginatedCache =

  let tryGet (db: IDatabase) uri =
    match db.HashGetAll (!> uri.ToString()) with
    | [||] -> None
    | values ->
        let lastModified =
          values
          |> Array.find (fun e -> e.Name = !> "lastModified")
          |> fun e -> e.Value.ToString()
          |> DateTimeOffset.Parse
        let nextUri =
          values
          |> Array.find (fun e -> e.Name = !> "nextUri")
          |> fun e ->
              if e.Value.HasValue
              then Some (e.Value.ToString() |> Uri)
              else None
        let json =
          values
          |> Array.find (fun e -> e.Name = !> "json")
          |> fun e -> e.Value.ToString()

        Some (lastModified, nextUri, json)

  let addOrUpdate (db: IDatabase) (uri: Uri) (lastModified': DateTimeOffset, nextUri: Uri option, json: string) =
    let hKey: RedisKey = !> uri.ToString()
    let nextUri' = nextUri |> Option.map (fun u -> u.ToString()) |> Option.defaultValue null

    db.HashGet (hKey, !> "lastModified")
    |> fun redisValue -> if redisValue.HasValue then Some (redisValue.ToString()) else None
    |> Option.map DateTimeOffset.Parse
    |> Option.defaultValue DateTimeOffset.MinValue
    |> fun lastModified ->
        if lastModified' > lastModified then
          let hValue = [|
            new HashEntry (!> "lastModified", !> lastModified'.ToString("o"))
            new HashEntry (!> "nextUri", !> nextUri')
            new HashEntry (!> "json", !> json)
          |]
          db.HashSet (hKey, hValue)
          db.KeyExpire (hKey, Nullable (TimeSpan.FromHours(1.))) |> ignore

  let tryRead (db: IDatabase) uri (req: HttpRequestMessage) =
    uri
    |> tryGet db
    |> Option.map (fun (lastModified, nextUri, json) ->
        req.Headers.IfModifiedSince <- Nullable lastModified
        (nextUri, json)
    )

  let tryWrite (db: IDatabase) uri json nextUri (resp: HttpResponseMessage) =
    resp.Content.Headers.LastModified
    |> Option.ofNullable
    |> Option.iter (fun lastModified -> addOrUpdate db uri (lastModified, nextUri, json))

let client =
  let c = new HttpClient()
  c.DefaultRequestHeaders.Add ("User-Agent", "whatdid")
  c.DefaultRequestHeaders.Add ("Accept", "application/vnd.github.v3+json")
  c

let perPage = 100

let createGet oauthToken (uri: Uri) =
  let req = new HttpRequestMessage (HttpMethod.Get, uri)
  oauthToken |> Option.iter (fun token -> req.Headers.Add ("Authorization", sprintf "token %s" token))
  req

let sendAsync request =
  client.SendAsync (request, HttpCompletionOption.ResponseHeadersRead)

let private _deserialize<'a> = JsonConvert.DeserializeObject<'a>

let deserializeAsJsonAsync<'a> (response: HttpResponseMessage) =
  task {
    let! json = response.Content.ReadAsStringAsync ()
    return _deserialize<'a> json
  }

// FIXME: Remove this additional connection here.
//        Only doing it right now for convenience.
let redis =
  let config = Config.getConfig ()
  let uri = Uri config.redisUrl
  let authority = uri.Authority
  let configStr =
    match uri.UserInfo.Split ([|':'|]) with
    | [|user; pass|] -> sprintf "%s,name=%s,password=%s" authority user pass
    | _ -> authority
  ConnectionMultiplexer.Connect configStr
// EMXIF

let tryGetAsync<'a> oauthToken uri =
  task {
    printfn "GET %A" uri
    let db = redis.GetDatabase ()
    use req = createGet oauthToken uri
    let cachedJson = RedisCache.tryRead db uri req
    use! response = sendAsync req

    match response with
    | Http2xx _ ->
        let! json = response.Content.ReadAsStringAsync ()
        let item = _deserialize<'a> json

        RedisCache.tryWrite db uri json response

        return Some item

    | Http304 status ->
        printfn "HTTP %i (GET %A)" (int status) uri
        return cachedJson |> Option.map _deserialize

    | _ ->
        eprintfn "WARNING: tryGetAsync: HTTP %i (GET %A)" (int response.StatusCode) uri
        return None
  }

module Pagination =
  let private _tryGetLink (headers: HttpResponseHeaders) =
    let mutable values: System.Collections.Generic.IEnumerable<string> = null
    if headers.TryGetValues ("Link", &values)
    then Some values
    else None
  /// From GitHub docs (https://developer.github.com/v3/#pagination):
  ///
  /// Link: <https://api.github.com/resource?page=2>; rel="next",
  ///       <https://api.github.com/resource?page=5>; rel="last"
  let tryGetNextUri (headers: HttpResponseHeaders) : Uri option =
    headers
    |> _tryGetLink
    |> Option.bind (fun values ->
        values
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
    )


  let getPaginated (reqF: Uri -> HttpRequestMessage) (deserialize: string -> 'a list) initialUri =
    initialUri
    |> Some
    |> AsyncSeq.unfoldAsync (
        function
        | None -> async { return None }
        | Some (uri: Uri) ->
            task {
              let db = redis.GetDatabase ()
              use req = reqF uri
              printfn "%s %A" req.Method.Method uri
              let cacheEntry = RedisPaginatedCache.tryRead db uri req
              use! response = sendAsync req

              match response with
              | Http2xx _ ->
                  let! json = response.Content.ReadAsStringAsync ()
                  let items = deserialize json
                  let nextUri = tryGetNextUri response.Headers
                  RedisPaginatedCache.tryWrite db uri json nextUri response
                  return Some (items, nextUri)

              | Http304 status ->
                  printfn "HTTP %i (GET %A)" (int status) uri
                  return
                    cacheEntry
                    |> Option.map (fun (nextUri, json) -> (deserialize json, nextUri))

              | _ ->
                  eprintfn "WARNING: getPaginated: HTTP %i (GET %A)" (int response.StatusCode) uri
                  return None
            }
            |> Async.AwaitTask
     )

let getPaginated<'a> oauthToken =
  Pagination.getPaginated (createGet oauthToken) _deserialize<'a list>
