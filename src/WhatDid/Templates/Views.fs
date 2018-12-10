module Views

open ASeward.MiscTools
open Giraffe.GiraffeViewEngine
open System
open Types
open ViewUtils.Stimulus

module private Form =
  let controllerName = "form"
  let stimController = _dataController controllerName
  let stimTarget = _dataTarget << (sprintf "%s.%s" controllerName)

  let textField name helpText isOptional (placeholderOpt: string option) (inputAttrs: XmlAttribute list) value =
    let containerClass = if isOptional then "field-container optional" else "field-container"

    div [_class containerClass] [
      label [_class "field-label"] [rawText name]
      label [_class "field-label-hint"] [rawText helpText]
      input [
        yield _type "text"
        yield! inputAttrs
        if placeholderOpt.IsSome then yield _placeholder placeholderOpt.Value
        yield _value value
      ]
    ]

  let requiredTextField name helpText =
    textField name helpText false None

  let optionalTextField name helpText placeholder =
    textField name helpText true (Some placeholder)

  let render (parts: RawParts) =
    let stimUpdateOnInput = _dataAction "input->form#update"
    App.layout [
      div [_class "form-container"] [
        form [stimController; _dataAction "keyup@window->form#tryNavigateOnEnter"] [
          section [] [
            div [_class "form-link-container"] [
              a [stimTarget "link"; _href ""] []
            ]
          ]
          section [] [
            requiredTextField
              "Owner"
              "User or organization that owns the repository"
              [stimTarget "owner"; stimUpdateOnInput]
              (parts.owner |> Option.defaultValue null)
            requiredTextField
              "Repository"
              "Name of the repository"
              [stimTarget "repo"; stimUpdateOnInput]
              (parts.repo |> Option.defaultValue null)
            requiredTextField
              "Base Revision"
              "SHA, tag, or branch marking the beginning of the diff"
              [stimTarget "base"; stimUpdateOnInput]
              (parts.baseRev |> Option.defaultValue null)
            optionalTextField
              "Head Revision"
              "SHA, tag, or branch marking the end of the diff"
              "master"
              [stimTarget "head"; stimUpdateOnInput]
              (parts.headRev |> Option.defaultValue null)
          ]
          section [] [
            div[_class "checkbox-container"] [
              input [stimTarget "live"; _dataAction "change->form#updateLiveUrl"; _type "checkbox"]
              label [] [rawText "Update the browser URL in real time"]
            ]
          ]
        ]
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
