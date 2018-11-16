module Config

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
    |> Envars.get
    |> UInt16.Parse
  with
  | _ -> 8085us

let getConfig () = {
  env = "ENV" |> Envars.getOr "development"
  port = getPort ()
  githubOauthClientId = Envars.get "GITHUB_OAUTH_CLIENT_ID"
  githubOauthClientSecret = Envars.get "GITHUB_OAUTH_CLIENT_SECRET"
}

let isProduction { env = env } = env = "production"
