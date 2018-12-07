module Index

open Giraffe.GiraffeViewEngine
open System
open Types

let private _fancyCompareUrl ({ FullParts.owner = owner; repo = repository } as parts) =
  let represent =
    function
    | Tag (TagName name, _)
    | Branch (BranchName name, _) -> name
    | Commit commitSha            -> commitSha.ShortSha
  let baseRev = represent parts.baseRevision
  let headRev = represent parts.headRevision

  [
    span [_class "secondary"] [ rawText "https://github.com/"]
    span [_class "primary"] [rawText owner]
    span [_class "secondary"] [rawText "/"]
    span [_class "primary"] [rawText repository]
    span [_class "secondary"] [rawText "/compare/"]
    span [_class "primary"] [rawText baseRev]
    span [_class "secondary"] [rawText "..."]
    span [_class "primary"] [rawText headRev]
  ]

let index (parts: FullParts) notesNode = [
  pre [] [
    yield! (_fancyCompareUrl parts)
    yield br []
    yield br []
    yield notesNode
  ]
]

let layout (parts: FullParts) notesText =
  App.layout <| index parts (rawText notesText)
