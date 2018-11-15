module Index

open Giraffe.GiraffeViewEngine

let index = [
  span [_class "static"] [ rawText "https://github.com/compare/"]
  input [_placeholder "owner"; _type "text"]
  span [_class "static"] [rawText "/"]
  input [_placeholder "repository"; _type "text"]
  span [_class "static"] [rawText "/compare/"]
  input [_placeholder "base"; _type "text"]
  span [_class "static"] [rawText "..."]
  input [_placeholder "master"; _type "text"]
]

let layout = App.layout index
