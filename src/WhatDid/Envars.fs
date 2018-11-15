module internal Envars

open System

module Option =
  let ofString (str: string) = if (String.IsNullOrWhiteSpace str) then None else Some str

let tryGet = Environment.GetEnvironmentVariable >> Option.ofString

let get name =
  name
  |> tryGet
  |> Option.defaultWith (fun () -> invalidArg name "Missing required environment variable")

let getEnv () =
  "ENV"
  |> tryGet
  |> Option.defaultValue "development"
