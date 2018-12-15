module Config

open Envars
open System

type Config =
  {
    env: string
    port: uint16
    githubOauthClientId: string
    githubOauthClientSecret: string
  }

let getPort () =
  try
    "PORT"
    |> get
    |> UInt16.Parse
  with
  | _ -> 8085us

let getConfig () = {
  env = "ENV" |> getOr "development"
  port = getPort ()
  githubOauthClientId = get "GITHUB_OAUTH_CLIENT_ID"
  githubOauthClientSecret = get "GITHUB_OAUTH_CLIENT_SECRET"
}

let isProduction { env = env } = env = "production"
