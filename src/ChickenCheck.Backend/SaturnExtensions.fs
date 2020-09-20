module ChickenCheck.Backend.SaturnExtensions

open System
open System.Security.Claims
open System.Text
open System.IO
open System.Net.Http.Headers
open ChickenCheck.Backend.Configuration
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.CookiePolicy
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.IdentityModel.Tokens
open Microsoft.Net.Http.Headers
open Saturn
open ChickenCheck.Shared
open FSharp.Control.Tasks.V2.ContextInsensitive

type Credentials =
     { Username: string
       Password: string }

let getCreds headerValue =
    let value = AuthenticationHeaderValue.Parse headerValue
    let bytes = Convert.FromBase64String value.Parameter
    let creds = (Encoding.UTF8.GetString bytes).Split([|':'|])

    { Username = creds.[0]
      Password = creds.[1] }

type BasicAuthHandler(options, logger, encoder, clock) =
    inherit AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)

    override this.HandleAuthenticateAsync() =
        let request = this.Request
        match request.Headers.TryGetValue "Authorization" with
        | (true, headerValue) ->
            task {
            let creds = getCreds headerValue.[0]

            if creds.Username = CompositionRoot.config.Authentication.ApiUsername && creds.Password = CompositionRoot.config.Authentication.ApiPassword then
                
                let claims = [| Claim(ClaimTypes.NameIdentifier, creds.Username); Claim(ClaimTypes.Name, creds.Username) |]
                let identity = ClaimsIdentity(claims, this.Scheme.Name)
                let principal = ClaimsPrincipal identity
                let ticket = AuthenticationTicket(principal, this.Scheme.Name)
                return AuthenticateResult.Success ticket
            else
                return AuthenticateResult.Fail("Invalid Username or Password") }
        | (false, _) ->
            task { return AuthenticateResult.Fail("Missing Authorization Header") }

type ApplicationBuilder with
    [<CustomOperationAttribute("use_basic_auth")>]
    member __.UseBasicAuth state =
        let middleware (app : IApplicationBuilder) =
            app.UseAuthentication()

        let service (s : IServiceCollection) =
            s.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuthentication", null)
                |> ignore
            s

        { state with
            ServicesConfig = service::state.ServicesConfig
            AppConfigs = middleware::state.AppConfigs }

    [<CustomOperation("use_cached_static_files_with_max_age")>]
    member __.UseStaticWithCacheMaxAge(state, path : string, maxAge) =
        let middleware (app : IApplicationBuilder) =
            match app.UseDefaultFiles(), state.MimeTypes with
            |app, [] -> app.UseStaticFiles(StaticFileOptions(OnPrepareResponse = (fun ctx -> ctx.Context.Response.Headers.[HeaderNames.CacheControl] <- sprintf "public, max-age=%i" maxAge |> StringValues)))
            |app, mimes ->
                let provider = FileExtensionContentTypeProvider()
                mimes |> List.iter (fun (extension, mime) -> provider.Mappings.[extension] <- mime)
                app.UseStaticFiles(StaticFileOptions(ContentTypeProvider=provider, OnPrepareResponse = (fun ctx -> ctx.Context.Response.Headers.[HeaderNames.CacheControl] <- sprintf "public, max-age=%i" maxAge |> StringValues)))
        let host (builder: IWebHostBuilder) =
            let p = Path.Combine(Directory.GetCurrentDirectory(), path)
            builder.UseWebRoot(p)
        { state with
            AppConfigs = middleware::state.AppConfigs
            WebHostConfigs = host::state.WebHostConfigs }
            
