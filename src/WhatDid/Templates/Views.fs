module Views

open ASeward.MiscTools
open Giraffe.GiraffeViewEngine
open System
open Types

module private Form =
  let render (parts: RawParts) =
    let onKeyUp = attr "onkeyup" "updateCompareUrl()"
    App.layout [
      h3 [] [rawText "We'll need just a little more information..."]

      form [] [
        div [] [
          label [_class "required"] [rawText "Owner"]
          input [_name "owner"; _value (parts.owner |> Option.defaultValue null); onKeyUp]
        ]
        div [] [
          label [_class "required"] [rawText "Repository"]
          input [_name "repo"; _value (parts.repo |> Option.defaultValue null); onKeyUp]
        ]
        div [] [
          label [_class "required"] [rawText "Base Revision"]
          input [_name "base"; _value (parts.baseRev |> Option.defaultValue null); onKeyUp]
        ]
        div [] [
          label [] [rawText "Head Revision"]
          input [_name "head"; _placeholder "master"; _value (parts.headRev |> Option.defaultValue null); onKeyUp]
        ]

        div [] [a [_href ""; _id "form-compare-link"] []]
      ]
    ]

module private Notes =
  let private _fancyCompareUrl ({ FullParts.owner = owner; repo = repo } as parts) =
    let represent =
      function
      | Tag (TagName name, _)
      | Branch (BranchName name, _) -> name
      | Commit commitSha            -> commitSha.FullSha
    let baseRev = represent parts.baseRevision
    let headRev = represent parts.headRevision
    let href = sprintf "https://github.com/%s/%s/compare/%s...%s" owner repo baseRev headRev

    a [_href href; _class "compare-url"] [
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
    |> List.map (fun pr ->
        let padString = String (Array.replicate (maxTitleLength - pr.title.Length) ' ')

        span [] [
          rawText <| sprintf "* %s%s " pr.title padString
          a [_class "pr-url"; _href pr.html_url] [rawText pr.html_url]
          br []
        ]
    )

  let render (parts: FullParts) prs =
    App.layout [
      pre [] [
        yield _fancyCompareUrl parts
        yield br []
        yield br []
        yield div [_class "notes-container"] (_formattedNotes prs)
      ]
    ]

let form = Form.render
let notes = Notes.render
