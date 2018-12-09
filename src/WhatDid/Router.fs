module Router

open ASeward.MiscTools
open FSharp.Control
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe.Core
open Giraffe.ResponseWriters
open HttpHandlers
open Microsoft.AspNetCore.Authentication
open Saturn
open System
open System.Threading.Tasks
open Types

let browser = pipeline {
  plug (requireHttps true)
  plug acceptHtml
  plug putSecureBrowserHeaders
  plug fetchSession
  set_header "x-pipeline-type" "Browser"
}

module TempHandler =
  let private _getTokenAsync (ctx: Microsoft.AspNetCore.Http.HttpContext) =
    task {
      let! t = ctx.GetTokenAsync "access_token"

      if String.IsNullOrWhiteSpace t then return None
      else return Some t
    }

  let private _getPrNumFromCommitMessage (message: string) =
    message
    |> Seq.skipWhile (fun ch -> ch <> '#')
    |> Seq.skipWhile (fun ch -> ch = '#')
    |> Seq.takeWhile (fun ch -> ch <> ' ')
    |> Seq.toArray
    |> String
    |> Int32.Parse

  open GitHub.Temp

  let private _disambiguatePartsAsync oauthToken (rawParts: RawParts) =
    match rawParts with
    | HasEverything (owner, repo, baseRev, headRev) ->
        let disambiguateAsync = GitHub.disambiguateAsync oauthToken owner repo
        task {
          let! uBaseOpt = disambiguateAsync baseRev
          let! uHeadOpt = disambiguateAsync headRev

          match uBaseOpt, uHeadOpt with
          | _, None
          | None, _ -> return! Task.FromException<FullParts> (exn "FIXME")
          | Some baseRev, Some headRev ->
              return
                { owner = owner
                  repo = repo
                  baseRevision = baseRev
                  headRevision = headRev }
        }
    | _ ->
        raise <| failMissingPieces rawParts

  let private _getReleaseNotesAsync (oauthToken: string option) (parts: FullParts) =
    match oauthToken with
    | None -> Task.FromException<string> (exn "FIXME")
    | Some oauthToken ->
        async {
          let! prs =
            parts
            |> GitHub.getAllPrMergeCommitsInRange (Some oauthToken)
            |> AsyncSeq.toBlockingSeq
            |> Seq.collect id
            |> Seq.map (fun x ->
                x.commit.message
                |> _getPrNumFromCommitMessage
                |> ReleaseNotes.GitHub.getPullRequestAsync
                      oauthToken
                      parts.owner
                      parts.repo
            )
            |> Async.Parallel

          return
            prs
            |> Array.map (fun pr -> sprintf "* %s %s" pr.title pr.html_url)
            |> Seq.sort
            |> String.concat Environment.NewLine
        }
        |> Async.StartAsTask

  let private _getPRsAsync oauthToken (parts: FullParts) =
    match oauthToken with
    | None -> Task.FromException<ReleaseNotes.GitHub.PullRequest list> (exn "FIXME")
    | Some oauthToken ->
        async {
          let! prs =
            parts
            |> GitHub.getAllPrMergeCommitsInRange (Some oauthToken)
            |> AsyncSeq.toBlockingSeq
            |> Seq.collect id
            |> Seq.map (fun x ->
                x.commit.message
                |> _getPrNumFromCommitMessage
                |> ReleaseNotes.GitHub.getPullRequestAsync
                      oauthToken
                      parts.owner
                      parts.repo
            )
            |> Async.Parallel

          return
            prs
            |> Seq.sort
            |> List.ofSeq
        }
        |> Async.StartAsTask

  let notesHandler parts : HttpHandler = (fun next ctx ->
    task {
      let! oauthToken = _getTokenAsync ctx
      let! parts' = _disambiguatePartsAsync oauthToken parts
      let! prs = _getPRsAsync oauthToken parts'
      let xmlNode = Views.notes parts' prs

      return! (htmlView xmlNode next ctx)
    }
  )
  let formHandler = Views.form >> htmlView

let renderNotes owner repo baseRev headRev =
  TempHandler.notesHandler
    { RawParts.Empty with
        owner = Some owner
        repo = Some repo
        baseRev = Some baseRev
        headRev = headRev |> Option.orElse (Some "master") }
let baseOnlyRange (owner, repo, baseRev) = renderNotes owner repo baseRev None
let fullySpecifiedRange (owner, repo, baseRev, headRev) =
  renderNotes owner repo baseRev (Some headRev)
let rangeForm (owner, repo) =
  TempHandler.formHandler { RawParts.Empty with owner = Some owner; repo = Some repo }
let repoForm owner = TempHandler.formHandler { RawParts.Empty with owner = Some owner }
let ownerForm = TempHandler.formHandler RawParts.Empty

let browserRouter = router {
  not_found_handler (setStatusCode 404 >=> htmlView NotFound.layout)
  pipe_through browser

  // TODO: Only require authentication if we seem to need it. Shouldn't need to
  // auth if you're just using on a public repo.
  pipe_through ( pipeline { requires_authentication (Giraffe.Auth.challenge "GitHub") } )
  get "/signin-github-oauth" (redirectTo false "/")

  getf "/%s/%s/compare/%s...%s" fullySpecifiedRange
  getf "/%s/%s/compare/%s" baseOnlyRange
  // NOTE: This is a bit of a hack, but necessary so that funkiness can't
  // happen by navigating to an incomplete path like this:
  // /:owner:/:repo:/compare
  getf "/%s/%s/%s" (fun (owner, repo, _) -> rangeForm (owner, repo))
  getf "/%s/%s/" rangeForm
  getf "/%s/%s" rangeForm

  getf "/%s/" repoForm
  getf "/%s" repoForm

  get  "/" ownerForm
}

//Other scopes may use different pipelines and error handlers

// let api = pipeline {
//     plug acceptJson
//     set_header "x-pipeline-type" "Api"
// }

// let apiRouter = router {
//     pipe_through api

//     forward "/someApi" someScopeOrController
// }

let appRouter = router {
    // forward "/api" apiRouter
    forward "" browserRouter
}
