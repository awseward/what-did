module OAuthStuff

open FSharp.Control.Tasks.ContextInsensitive
open Giraffe
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OAuth
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Newtonsoft.Json.Linq
open Saturn.Application
open System
open System.Net.Http
open System.Net.Http.Headers
open System.Text.Encodings.Web
open System.Threading.Tasks

let private _addCookie (state: ApplicationState) (c : AuthenticationBuilder) = if not state.CookiesAlreadyAdded then c.AddCookie () |> ignore
let private _makeHttpsUnlessLocalhost (url: string) =
  let builder = UriBuilder url
  if builder.Host <> "localhost" then
    builder.Scheme <- "https"
    builder.Uri.GetComponents (UriComponents.Scheme ||| UriComponents.Host ||| UriComponents.PathAndQuery, UriFormat.Unescaped)
  else
    builder.Uri.AbsoluteUri

type MyOAuthHandler (options: IOptionsMonitor<OAuthOptions>, logger: ILoggerFactory, encoder: UrlEncoder, clock: ISystemClock) =
  inherit OAuthHandler<OAuthOptions> (options, logger, encoder, clock)

  /// Based on: https://github.com/aspnet/Security/blob/32177cad1eb1eb6be8ad89a7dba1a1637c0d0786/src/Microsoft.AspNetCore.Authentication/AuthenticationHandler.cs#L117-L118
  member private __._BuildRedirectUri (targetPath: string) =
    let scheme =
      if __.Request.Headers.["X-Forwarded-Proto"].Count > 0 then
        __.Request.Headers.["X-Forwarded-Proto"].[0]
      else
        __.Request.Scheme
    let host =
      if __.Request.Headers.["X-Forwarded-Host"].Count > 0 then
        __.Request.Headers.["X-Forwarded-Host"].[0]
      else
        __.Request.Host.Value // FIXME
    scheme + "://" + host + __.OriginalPathBase + targetPath

  /// Based on: https://github.com/aspnet/Security/blob/32177cad1eb1eb6be8ad89a7dba1a1637c0d0786/src/Microsoft.AspNetCore.Authentication.OAuth/OAuthHandler.cs#L193-L208
  override __.HandleChallengeAsync properties =
    if String.IsNullOrEmpty properties.RedirectUri then
      properties.RedirectUri <- __.CurrentUri

    __.GenerateCorrelationId properties

    let authorizationEndpoint = __.BuildChallengeUrl (properties, __._BuildRedirectUri (__.Options.CallbackPath.Value)) // FIXME: Don't call .Value
    let redirectContext =
      new RedirectContext<OAuthOptions> (
        __.Context,
        __.Scheme,
        __.Options,
        properties,
        authorizationEndpoint
      )

    let doRedirect = __.Events.RedirectToAuthorizationEndpoint
    task {
      let! _ = doRedirect redirectContext
      return ()
    }
    :> Task

  /// Based on: https://github.com/aspnet/Security/blob/32177cad1eb1eb6be8ad89a7dba1a1637c0d0786/src/Microsoft.AspNetCore.Authentication.OAuth/OAuthHandler.cs#L148-L175
  override __.ExchangeCodeAsync (code, redirectUri) =
    // NOTE: The `X-Forwarded-Proto` approach from before doesn't seem to work
    // here, so for now just assuming we want https unless it's localhost
    let redirectUri' = _makeHttpsUnlessLocalhost redirectUri

    base.ExchangeCodeAsync (code, redirectUri')

type ApplicationBuilder with
  /// Enables GitHub OAuth authentication in an SSL termination-friendly way.
  /// Based on: https://github.com/SaturnFramework/Saturn/blob/67b5a0ffce2f72d348c4e712363a4be4922d99ae/src/Saturn.Extensions.Authorization/OAuth.fs#L87-L132
  /// Uses approach described here, but with the errors cleaned up: https://blogs.msdn.microsoft.com/wushuai/2018/03/10/extend-microsoft-aspnetcore-authentication-oauth-for-reverse-proxy/
  [<CustomOperation("use_github_oauth_ssl_termination_friendly")>]
  member __.UseGithubAuthSslTerminationFriendly(state: ApplicationState, clientId : string, clientSecret : string, callbackPath : string, jsonToClaimMap : (string * string) seq) =
    let middleware (app : IApplicationBuilder) =
      app.UseAuthentication()

    let service (s : IServiceCollection) =
      let c = s.AddAuthentication(fun cfg ->
        cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
        cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
        cfg.DefaultChallengeScheme <- "GitHub")

      _addCookie state c
      c.AddOAuth<OAuthOptions, MyOAuthHandler>("GitHub", fun (opt: Authentication.OAuth.OAuthOptions) ->
        opt.ClientId <- clientId
        opt.ClientSecret <- clientSecret
        opt.CallbackPath <- PathString (callbackPath)
        opt.AuthorizationEndpoint <-  "https://github.com/login/oauth/authorize"
        opt.TokenEndpoint <- "https://github.com/login/oauth/access_token"
        opt.UserInformationEndpoint <- "https://api.github.com/user"
        opt.SaveTokens <- true
        opt.Scope.Add "repo"

        jsonToClaimMap |> Seq.iter (fun (k, v) -> opt.ClaimActions.MapJsonKey(v, k))

        opt.Events.OnCreatingTicket <-
          fun ctx ->
            let tsk = task {
              let req = new HttpRequestMessage (HttpMethod.Get, ctx.Options.UserInformationEndpoint)
              req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
              req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", ctx.AccessToken)
              let! (response : HttpResponseMessage) = ctx.Backchannel.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.HttpContext.RequestAborted)
              response.EnsureSuccessStatusCode () |> ignore
              let! cnt = response.Content.ReadAsStringAsync()
              let user = JObject.Parse cnt
              ctx.RunClaimActions user
            }
            Task.Factory.StartNew (fun () -> tsk.Result)

      ) |> ignore
      s

    { state with
        ServicesConfig = service::state.ServicesConfig
        AppConfigs = middleware::state.AppConfigs
        CookiesAlreadyAdded = true
    }
