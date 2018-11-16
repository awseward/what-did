module Program

open Config
open dotenv.net
open Saturn
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
}

[<EntryPoint>]
let main _ =
  printfn "Working directory - %s" (System.IO.Directory.GetCurrentDirectory())
  run app
  0
