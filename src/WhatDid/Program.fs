module Program

open dotenv.net
open FSharp.Control.Tasks.ContextInsensitive
open Saturn
open System

DotEnv.Config (
  throwOnError = false,
  filePath = "../../.env")

let port =
  try
    UInt16.Parse (Environment.GetEnvironmentVariable "PORT")
  with
  | _ -> 8085us

let private _isProduction = ("production" = Environment.GetEnvironmentVariable "ENV")

let endpointPipe = pipeline {
    plug head
    plug requestId
}

let app = application {
    pipe_through endpointPipe

    error_handler (fun ex _ -> pipeline { set_status_code 500; render_html (InternalError.layout _isProduction ex) })
    use_router Router.appRouter
    url (sprintf "http://0.0.0.0:%d/" port)
    memory_cache
    use_static "static"
    use_gzip
    // use_config (fun _ -> { connectionString = _connectionString })
    use_turbolinks
}

[<EntryPoint>]
let main _ =
    printfn "Working directory - %s" (System.IO.Directory.GetCurrentDirectory())
    run app
    0
