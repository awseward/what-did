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

let isProduction = (config |> Config.isProduction)

let endpointPipe = pipeline {
  plug head
  plug requestId
}

let app = application {
  pipe_through endpointPipe

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
