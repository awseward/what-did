module App

open Giraffe.GiraffeViewEngine
open ViewUtils.Stimulus

let layout (content: XmlNode list) =
    html [_class "has-navbar-fixed-top"] [
        head [] [
            meta [_charset "utf-8"]
            meta [_name "viewport"; _content "width=device-width, initial-scale=1" ]
            title [] [encodedText "What did we do?"]
            link [_rel "stylesheet"; _href "/app.css" ]
            script [_src "https://cdnjs.cloudflare.com/ajax/libs/turbolinks/5.1.1/turbolinks.js"] []
            script [_src "https://unpkg.com/stimulus@2.0.0/dist/stimulus.umd.js"] []
            script [_defer; _src "/app.js"] []
        ]

        body [_class "no-select"] [
            yield h1 [] [rawText "What did we do?"]
            yield div [_class "main-container text-select"] content
        ]
    ]
