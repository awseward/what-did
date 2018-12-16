module Program

open dotenv.net
open Giraffe.Core
open OAuthStuff
open Saturn
open StackExchange.Redis
open System

DotEnv.Config (
  throwOnError = false,
  filePath = "../../.env"
)

let config = Config.getConfig ()

let redis =
  let uri = Uri config.redisUrl
  let authority = uri.Authority
  let configStr =
    match uri.UserInfo.Split ([|':'|]) with
    | [|user; pass|] -> sprintf "%s,name=%s,password=%s" authority user pass
    | _ -> authority
  ConnectionMultiplexer.Connect configStr

let isProduction = (config |> Config.isProduction)

let tryRedisThings : HttpHandler = (fun next ctx ->
  try
    let db = redis.GetDatabase ()
    let hashKey = RedisKey.op_Implicit "https://api.github.com/:org/:repo/compare/:base...:head"
    let hashValue = [|
      new HashEntry (RedisValue.op_Implicit "lastModified", RedisValue.op_Implicit (DateTimeOffset.UtcNow.ToString "o"))
      new HashEntry (RedisValue.op_Implicit "nextUri", RedisValue.op_Implicit (string null))
      new HashEntry (RedisValue.op_Implicit "json", RedisValue.op_Implicit "{\"foo\":true}")
    |]
    db.HashSet (hashKey, hashValue)

    printfn "!!! hashKey !!! %A" hashKey
    printfn "!!! hashValue' !!! %A" (db.HashGetAll hashKey)
  with
  | e -> printfn "ERROR: %A" e

  next ctx
)

let endpointPipe = pipeline {
  plug head
  plug requestId
}

let app = application {
  pipe_through endpointPipe
  pipe_through (pipeline { plug tryRedisThings })

  error_handler (fun ex _ -> pipeline { set_status_code 500; render_html (InternalError.layout isProduction ex) })
  use_router Router.appRouter
  url (sprintf "http://0.0.0.0:%d/" config.port)
  memory_cache
  use_static "static"
  use_gzip
  use_config (fun _ -> config)
  use_turbolinks
  use_github_oauth_ssl_termination_friendly config.githubOauthClientId config.githubOauthClientSecret "/signin-github-oauth" [("login", "githubUsername"); ("name", "fullName")]
}

[<EntryPoint>]
let main _ =
  printfn "Working directory - %s" (System.IO.Directory.GetCurrentDirectory())
  run app
  0
