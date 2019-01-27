open Fake.Core
open Fake.Core
#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "netstandard"
  #r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open ASeward.MiscTools
open ASeward.MiscTools.ActivePatterns
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open System

let appPath = Path.getFullName "./src/WhatDid"
let deployDir = Path.getFullName "./deploy"

let private _getOrPromptAndSetRequiredEnvar name =
  let rec promptIfNecessaryUntilProvided =
    function
    | Some str -> str
    | None ->
        Trace.traceImportantfn "Missing env var '%s'" name
        Trace.tracef "Please provide a value now: "
        Console.ReadLine ()
        |> function
            | NullOrWhiteSpace -> promptIfNecessaryUntilProvided None
            | str -> str

  let value =
    name
    |> Environment.environVar
    |> Option.ofString
    |> promptIfNecessaryUntilProvided

  // Will be unnecessary in some cases but oh well for now
  Environment.setEnvironVar name value

  value

module Util =
  let runDotNet cmd workingDir =
    let result = DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

  let runTool cmd args workingDir =
    let result =
      Process.execSimple (fun info ->
        { info with
            FileName = cmd
            WorkingDirectory = workingDir
            Arguments = args })
        TimeSpan.MaxValue
    if result <> 0 then failwithf "'%s %s' failed" cmd args

  let openBrowser url =
    let result =
      //https://github.com/dotnet/corefx/issues/10361
      Process.execSimple (fun info ->
        { info with
            FileName = url
            UseShellExecute = true })
        TimeSpan.MaxValue
    if result <> 0 then failwithf "opening browser failed"

open Util

Target.create "Clean" (fun _ ->
  !! "src/**/bin"
  ++ "src/**/obj"
  ++ deployDir
  |> Shell.cleanDirs
)

Target.create "Build" (fun _ ->
  !! "src/**/*.*proj"
  |> Seq.iter (DotNet.build id)
)

Target.create "Run" (fun _ ->
  let server = async {
    runDotNet "watch run" appPath
  }
  let browser = async {
    do! Async.Sleep 5000
    openBrowser "http://localhost:8085"
  }

  [server; browser]
  |> Async.Parallel
  |> Async.RunSynchronously
  |> ignore
)

Target.create "Bundle:Web" (fun _ ->
  let appDeployDir = Path.combine deployDir "WhatDid"

  runDotNet (sprintf "publish -c Release -o \"%s\"" appDeployDir) appPath
)

Target.create "Heroku:Web" (fun _ ->
  let remote = _getOrPromptAndSetRequiredEnvar "HEROKU_REMOTE"
  let heroku args = runTool "heroku" (sprintf "%s --remote=%s" args remote) "."

  heroku "container:login"
  heroku "container:push web --recursive"
  heroku "container:release web"
  heroku "open"
)

"Clean"
  ==> "Build"
  ==> "Bundle:Web"

Target.runOrList ()
