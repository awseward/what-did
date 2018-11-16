module Router

open Saturn
open Giraffe.Core
open Giraffe.ResponseWriters
open WhatDid.HttpHandlers
open System

let browser = pipeline {
  plug (requireHttps true)
  plug acceptHtml
  plug putSecureBrowserHeaders
  plug fetchSession
  set_header "x-pipeline-type" "Browser"
}

type RouterBuilder with
  [<CustomOperation("render")>]
  member __.Render (state: RouterState, partsFn: Index.Parts -> Index.Parts) =
    Index.Parts.Empty
    |> partsFn
    |> Index.layout'
    |> htmlView
    |> fun handler -> __.Get (state, "", handler)
  [<CustomOperation("renderRoot")>]
  member __.RenderRoot (state: RouterState, partsFn: Index.Parts -> Index.Parts) =
    Index.Parts.Empty
    |> partsFn
    |> Index.layout'
    |> htmlView
    |> fun handler -> __.Get (state, "/", handler)


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

  forwardf "/%s" ownerRouter
  renderRoot id
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
