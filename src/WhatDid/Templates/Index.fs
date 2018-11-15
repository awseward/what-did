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

let index' = [
  pre [] [
    span [_class "static"] [ rawText "https://github.com/compare/"]
    span [_class "dynamic"] [rawText "owner"]
    span [_class "static"] [rawText "/"]
    span [_class "dynamic"] [rawText "repository"]
    span [_class "static"] [rawText "/compare/"]
    span [_class "dynamic"] [rawText "base"]
    span [_class "static"] [rawText "..."]
    span [_class "dynamic"] [rawText "master"]
    br []
    br []
    rawText exampleNotes
  ]
]

let layout = App.layout index'
