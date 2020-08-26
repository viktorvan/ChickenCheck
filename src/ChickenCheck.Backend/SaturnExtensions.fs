module ChickenCheck.Backend.SaturnExtensions

open System.IO
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
open ChickenCheck.Backend
open ChickenCheck.Shared

type Application.ApplicationBuilder with
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
            
    [<CustomOperation("use_auth0_open_id")>]
    member __.UseAuth0OpenId(state: ApplicationState) =
        // https://auth0.com/docs/quickstart/webapp/aspnet-core-3?download=true#install-and-configure-openid-connect-middleware
        
        let toAbsolutePath (req: HttpRequest) (path: string) =
            match path.StartsWith("/") with
            | true ->
                req.Scheme + "://" + req.Host.Value + req.PathBase.Value + path
            | false -> 
                path
        
        let middleware (app : IApplicationBuilder) =
            app.UseAuthentication()
                .UseHsts()
                .UseCookiePolicy()
                .UseAuthorization()

        let service (services: IServiceCollection) =
            services.Configure<CookiePolicyOptions>(fun (o:CookiePolicyOptions) ->
                    o.Secure <- CookieSecurePolicy.SameAsRequest
                    o.HttpOnly <- HttpOnlyPolicy.Always
                    o.MinimumSameSitePolicy <- Microsoft.AspNetCore.Http.SameSiteMode.None)
                .AddAuthorization()
                .AddAuthentication(fun (o:AuthenticationOptions) ->
                    o.DefaultAuthenticateScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                    o.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                    o.DefaultChallengeScheme <- CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddOpenIdConnect("Auth0", fun (o:OpenIdConnectOptions) ->
                    o.Authority <- sprintf "https://%s" CompositionRoot.config.Authentication.Domain
                    o.ClientId <- CompositionRoot.config.Authentication.ClientId
                    o.ClientSecret <- CompositionRoot.config.Authentication.ClientSecret
                    o.ResponseType <- OpenIdConnectResponseType.Code
                    o.Scope.Add "openid"
                    o.CallbackPath <- PathString "/callback" // Also ensure that you have added the URL as an Allowed Callback URL in your Auth0 dashboard
                    o.ClaimsIssuer <- "Auth0"
                    o.TokenValidationParameters <- TokenValidationParameters(NameClaimType = "name", RoleClaimType = sprintf "https://schemas.viktorvan.com/roles")
                    o.Events <- OpenIdConnectEvents(
                        OnRedirectToIdentityProviderForSignOut = (fun ctx ->
                            let logoutUri = sprintf "https://%s/v2/logout?client_id=%s" CompositionRoot.config.Authentication.Domain CompositionRoot.config.Authentication.ClientId
                            
                            let redirectQueryParameter =
                                ctx.Properties.RedirectUri
                                |> String.notNullOrEmpty
                                |> Option.map (toAbsolutePath ctx.Request)
                                |> Option.map (sprintf "&returnTo=%s")
                                |> Option.defaultValue ""
                        
                            ctx.Response.Redirect (logoutUri + redirectQueryParameter)
                            ctx.HandleResponse()
                            System.Threading.Tasks.Task.CompletedTask),
                        OnUserInformationReceived = fun ctx ->
                            printfn "received user info"
                            System.Threading.Tasks.Task.CompletedTask))
                |> ignore
            services

        { state with
            ServicesConfig = service::state.ServicesConfig
            AppConfigs = middleware::state.AppConfigs
            CookiesAlreadyAdded = true }
