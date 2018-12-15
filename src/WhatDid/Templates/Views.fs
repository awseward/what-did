module Views

open Giraffe.GiraffeViewEngine
open GitHub.Types
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

  let previewLink (parts: RawParts) =
    let owner = parts.owner |> Option.defaultValue ":owner:"
    let repo = parts.repo |> Option.defaultValue ":repo:"
    let baseRev = parts.baseRev |> Option.defaultValue ":base:"
    let headRev = parts.headRev |> Option.defaultValue ":head:"

    a [stimTarget "link"; _href ""] [
      span [_class "secondary"; stimTarget "linkAuthority"] [(* Will have to be filled in by JS *)]
      span [_class "secondary"]                             [rawText "/"]
      span [_class "primary"; stimTarget "linkOwner"]       [rawText owner]
      span [_class "secondary"]                             [rawText "/"]
      span [_class "primary"; stimTarget "linkRepo"]        [rawText repo]
      span [_class "secondary"]                             [rawText "/compare/"]
      span [_class "primary"; stimTarget "linkBase"]        [rawText baseRev]
      span [stimTarget "linkHeadContainer"] [
        span [_class "secondary"]                           [rawText "..."]
        span [_class "primary"; stimTarget "linkHead"]      [rawText headRev]
      ]
    ]

  let render (parts: RawParts) =
    let stimUpdateOnInput = _dataAction "input->form#update"
    App.layout [
      div [_class "form-container"] [
        form [stimController; _dataAction "keyup@window->form#tryNavigateOnEnter"] [
          section [] [
            div [_class "form-link-container"] [
              previewLink parts
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
          section [_style "display:none;"] [
            div[_class "checkbox-container"] [
              input [stimTarget "live"; _dataAction "change->form#updateLiveUrl"; _type "checkbox"]
              label [] [rawText "Update the browser URL in real time"]
            ]
          ]
        ]
      ]
    ]

module private Notes =
  let private _represent =
    function
    | Tag (TagName name, _)
    | Branch (BranchName name, _) -> name
    | Commit commitSha            -> commitSha.FullSha

  let private _rawCompareUrl ({ FullParts.owner = owner; repo = repo } as parts) =
    let represent = _represent
    let baseRev = _represent parts.baseRevision
    let headRev = _represent parts.headRevision
    sprintf "https://github.com/%s/%s/compare/%s...%s" owner repo baseRev headRev

  let private _richCompareUrl ({ FullParts.owner = owner; repo = repo } as parts) =
    let represent = _represent
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

  let private _rawNotes =
    function
    | [] -> []
    | (prs: PullRequestResp list) ->
        let maxTitleLength =
          prs
          |> List.map (fun { title = t } -> t.Length)
          |> List.max

        prs
        |> List.map (fun pr ->
            let padString = String (Array.replicate (maxTitleLength - pr.title.Length) ' ')

            sprintf "* %s%s %s" pr.title padString pr.html_url
        )

  let private _richNotes =
    function
    | [] -> []
    | (prs: PullRequestResp list) ->
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

  let private _secretInput (parts: FullParts) prs =
    textarea [_class "secret"; _dataTarget "notes.secretInput"] [
      [
        yield! [_rawCompareUrl parts]
        yield  ""
        yield! _rawNotes prs
      ]
      |> String.concat Environment.NewLine
      |> rawText
    ]

  let private _clipboardPieces parts prs =
    [
      _secretInput parts prs
      button [_class "button-clipboard no-select"; _dataAction "notes#copyToClipboard"] [rawText "Copy to clipboard"]
    ]

  let render (parts: FullParts) prs =
    App.layout [
      pre [_dataController "notes"] [
        if List.isEmpty prs then
          yield div [_class "empty-container no-select"] [rawText "Looks like there were no PRs merged in this range..."]
        else
          yield _richCompareUrl parts
          yield br []
          yield br []
          yield div [_class "notes-container"] (_richNotes prs)

          yield! _clipboardPieces parts prs
      ]
    ]

let form = Form.render
let notes = Notes.render
