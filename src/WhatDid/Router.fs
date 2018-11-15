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

let defaultView = router {
    get "/" (htmlView Index.layout)
}

let browserRouter = router {
    not_found_handler (setStatusCode 404 >=> htmlView NotFound.layout)
    pipe_through browser

    forward "" defaultView
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
