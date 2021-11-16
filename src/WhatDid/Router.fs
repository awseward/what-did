module Router

open Giraffe.Core
open Giraffe.ResponseWriters
open HttpHandlers
open ReleaseNotes.Handlers
open Saturn
open System
open Types

let browser = pipeline {
  plug (requireHttps true)
  plug acceptHtml
  plug putSecureBrowserHeaders
  plug fetchSession
  set_header "x-pipeline-type" "Browser"
}

let renderNotes headRev (owner, repo, baseRev) =
  notesHandler
    { RawParts.Empty with
        owner = Some owner
        repo = Some repo
        baseRev = Some baseRev
        headRev = headRev }
let fullySpecifiedRange (owner, repo, baseRev, headRev) =
  renderNotes (Some headRev) (owner, repo, baseRev)
let rangeForm (owner, repo) =
  formHandler { RawParts.Empty with owner = Some owner; repo = Some repo }
let repoForm owner = formHandler { RawParts.Empty with owner = Some owner }
let ownerForm = formHandler RawParts.Empty

let browserRouter = router {
  not_found_handler (setStatusCode 404 >=> htmlView NotFound.layout)
  pipe_through browser

  // TODO: Only require authentication if we seem to need it. Shouldn't need to
  // auth if you're just using on a public repo.
  pipe_through ( pipeline { requires_authentication (Giraffe.Auth.challenge "GitHub") } )
  get "/signin-github-oauth" (redirectTo false "/")

  getf "/%s/%s/compare/%s...%s" fullySpecifiedRange
  getf "/%s/%s/compare/%s" (renderNotes None)
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
