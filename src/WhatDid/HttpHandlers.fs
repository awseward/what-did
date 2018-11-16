module HttpHandlers

open Giraffe
open Giraffe.Core
open Saturn.Pipeline
open System
open System.Threading.Tasks

let passThrough : HttpHandler = fun next ctx -> next ctx

let setHsts : HttpHandler = setHttpHeader "Strict-Transport-Security" "max-age=31536000; includeSubDomains"

let redirectHttps : HttpHandler =
  fun next ctx ->
    let headerValues = ctx.Request.Headers.["X-Forwarded-Proto"]
    if headerValues.Count > 0 && headerValues.[0] = "http" then
      let path = if ctx.Request.Path.HasValue then ctx.Request.Path.Value else ""
      let builder = new System.UriBuilder ("https", ctx.Request.Host.Value, -1, path)
      let redirectUri = builder.Uri.GetComponents (UriComponents.Scheme ||| UriComponents.Host ||| UriComponents.PathAndQuery, UriFormat.SafeUnescaped)
      redirectTo true redirectUri next ctx
    else
      next ctx

let userIsAuthenticated : HttpHandler =
  fun next ctx ->
    if (isNull ctx.User || isNull ctx.User.Identity || not ctx.User.Identity.IsAuthenticated) then Task.FromResult None
    else
      next ctx

let requireHttps isRequired =
  if isRequired then setHsts >=> redirectHttps
  else
    passThrough

type PipelineBuilder with
  [<CustomOperation("require_https")>]
  member __.RequireHttps (state, isRequired) : HttpHandler = state >=> (requireHttps isRequired)
