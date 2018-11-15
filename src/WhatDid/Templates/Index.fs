module Index

open Giraffe.GiraffeViewEngine
open System

let private exampleNotes =
  @"
feat:     Feature for person        https://github.com/owner/repository/pull/12
fix:      Some dumb bug             https://github.com/owner/repository/pull/21
fix:      Another bug               https://github.com/owner/repository/pull/18
fix:      So many bugs              https://github.com/owner/repository/pull/30
refactor: Rewrite in $LATEST_THING  https://github.com/owner/repository/pull/8
style:    Make it look alright      https://github.com/owner/repository/pull/20"
    .Trim()

let private _fancyCompareUrl owner repository baseRev headRevOpt =
  let headRev = headRevOpt |> Option.defaultValue "master"

  [
    span [_class "secondary"] [ rawText "https://github.com/compare/"]
    span [_class "primary"] [rawText owner]
    span [_class "secondary"] [rawText "/"]
    span [_class "primary"] [rawText repository]
    span [_class "secondary"] [rawText "/compare/"]
    span [_class "primary"] [rawText baseRev]
    span [_class "secondary"] [rawText "..."]
    span [_class "primary"] [rawText headRev]
  ]


let index = [
  pre [] [
    yield! (_fancyCompareUrl "owner" "repository" "base" (Some "master"))
    yield br []
    yield br []
    yield rawText exampleNotes
  ]
]

let layout = App.layout index
