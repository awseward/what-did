module Index

open Giraffe.GiraffeViewEngine
open System
open Types

let private _fancyCompareUrl owner repository baseRev headRevOpt =
  let headRev = headRevOpt |> Option.defaultValue "master"

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

let index owner repository baseRev headRevOpt notesNode = [
  pre [] [
    yield! (_fancyCompareUrl owner repository baseRev headRevOpt)
    yield br []
    yield br []
    yield notesNode
  ]
]

let private ``_or?`` = Option.defaultValue "?"

let layout (parts: RawParts) notesText =
  App.layout <|
    index
      (``_or?`` parts.owner)
      (``_or?`` parts.repo)
      (``_or?`` parts.baseRev)
      parts.headRev
      (rawText notesText)
