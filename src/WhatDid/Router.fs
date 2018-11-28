module Router

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

  let private _getReleaseNotes (oauthToken: string option) (parts: Parts) =
    parts
    |> GitHub.getAllCommitsInRange oauthToken
    |> AsyncSeq.toBlockingSeq
    |> Seq.collect id
    |> Seq.map (fun x -> x.commit.message)
    |> String.concat Environment.NewLine

  let getHandler parts : HttpHandler = (fun next ctx ->
    task {
      let! oauthToken = _getTokenAsync ctx
      let notes = _getReleaseNotes oauthToken parts
      let xmlNode = Index.layout parts notes

      return! (htmlView xmlNode next ctx)
    }
  )

type RouterBuilder with
  [<CustomOperation("render")>]
  member __.Render (state, partsFn) =
    let handler =
      Parts.Empty
      |> partsFn
      |> TempHandler.getHandler

    state
    |> fun s -> __.Get (s, "", handler)
    |> fun s -> __.Get (s, "/", handler)

let rangeRouter owner repo (baseRev, headRev) = router {
  render (fun x ->
    { x with
        owner = Some owner
        repo = Some repo
        baseRev = Some baseRev
        headRev = Some headRev }
  )
}

let rangeRouterNoHead owner repo baseRev = router {
  render (fun x ->
    { x with
        owner = Some owner
        repo = Some repo
        baseRev = Some baseRev
        headRev = None }
  )
}

let repoRouter owner repo = router {
  forwardf "/compare/%s...%s" (rangeRouter owner repo)
  forwardf "/compare/%s" (rangeRouterNoHead owner repo)

  render (fun x ->
    { x with
        owner = Some owner
        repo = Some repo }
  )
}

let ownerRouter owner = router {
  forwardf "/%s" (repoRouter owner)

  render (fun x ->
    { x with owner = Some owner }
  )
}

let browserRouter = router {
  not_found_handler (setStatusCode 404 >=> htmlView NotFound.layout)
  pipe_through browser
  // TODO: Only require authentication if we seem to need it. Shouldn't need to
  // auth if you're just using on a public repo.
  pipe_through ( pipeline { requires_authentication (Giraffe.Auth.challenge "GitHub") } )
  get "/signin-github-oauth" (redirectTo false "/")

  forwardf "/%s" ownerRouter
  render id
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
