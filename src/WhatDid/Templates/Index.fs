module Index

open ASeward.MiscTools
open Giraffe.GiraffeViewEngine
open System
open Types

let private _fancyCompareUrl ({ FullParts.owner = owner; repo = repo } as parts) =
  let represent =
    function
    | Tag (TagName name, _)
    | Branch (BranchName name, _) -> name
    | Commit commitSha            -> commitSha.ShortSha
  let baseRev = represent parts.baseRevision
  let headRev = represent parts.headRevision
  let href = sprintf "https://github.com/%s/%s/compare/%s...%s" owner repo baseRev headRev

  a [_href href] [
    span [_class "secondary"] [rawText "https://github.com/"]
    span [_class "primary"]   [rawText owner]
    span [_class "secondary"] [rawText "/"]
    span [_class "primary"]   [rawText repo]
    span [_class "secondary"] [rawText "/compare/"]
    span [_class "primary"]   [rawText baseRev]
    span [_class "secondary"] [rawText "..."]
    span [_class "primary"]   [rawText headRev]
  ]

let private _formattedNotes (prs: ReleaseNotes.GitHub.PullRequest list) : XmlNode list =
  let maxTitleLength =
    prs
    |> List.map (fun { title = t } -> t.Length)
    |> List.max

  prs
  |> List.collect (fun pr ->
      let padString = String (Array.replicate (maxTitleLength - pr.title.Length) ' ')

      [
        rawText <| sprintf "* %s%s " pr.title padString
        a [_href pr.html_url] [rawText pr.html_url]
        br []
      ]
  )

let index (parts: FullParts) prs = [
  pre [] [
    yield _fancyCompareUrl parts
    yield br []
    yield br []
    yield! _formattedNotes prs
  ]
]

let layout (parts: FullParts) prs =
  App.layout <| index parts prs
