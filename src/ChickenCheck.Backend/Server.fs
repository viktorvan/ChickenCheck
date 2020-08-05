module Server.Startup

open System
open System.IO
open ChickenCheck.Backend
open ChickenCheck.Backend.Configuration
open ChickenCheck.Backend.Turbolinks
open ChickenCheck.Backend.Views
//open Microsoft.AspNetCore.Authentication.Cookies
//open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Builder
open Giraffe
open Microsoft.AspNetCore.CookiePolicy
open Microsoft.IdentityModel.Protocols.OpenIdConnect
open Microsoft.Net.Http.Headers
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Primitives
open Saturn
open ChickenCheck.Shared
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.AspNetCore.Authentication
open Microsoft.Extensions.DependencyInjection

let setPathScheme (path: string) =
    // application is running as http, but is ssl-terminated in production, we need to change scheme to https.
    printfn "setting pathscheme for %s" path
    if path.Contains("localhost", StringComparison.InvariantCultureIgnoreCase) then 
        path
    else
        path.Replace("http", "https")
    
let toAbsolutePath (req: HttpRequest) (path: string) =
    match path.StartsWith("/") with
    | true ->
        req.Scheme + "://" + req.Host.Value + req.PathBase.Value + path
    | false -> 
        path
    |> setPathScheme
        
type Saturn.Application.ApplicationBuilder with
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
            Console.WriteLine("Using webRoot: " + p)
            builder.UseWebRoot(p)
        { state with
            AppConfigs = middleware::state.AppConfigs
            WebHostConfigs = host::state.WebHostConfigs }
    
    [<CustomOperation("use_auth0_open_id")>]
    member __.UseAuth0OpenId(state: ApplicationState) =
        // https://auth0.com/docs/quickstart/webapp/aspnet-core-3?download=true#install-and-configure-openid-connect-middleware
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
                    o.Events <- OpenIdConnectEvents(OnRedirectToIdentityProviderForSignOut = fun ctx ->
                        let logoutUri = sprintf "https://%s/v2/logout?client_id=%s" CompositionRoot.config.Authentication.Domain CompositionRoot.config.Authentication.ClientId
                        
                        let redirectQueryParameter =
                            ctx.Properties.RedirectUri
                            |> String.notNullOrEmpty
                            |> Option.map (toAbsolutePath ctx.Request)
                            |> Option.map (sprintf "&returnTo=%s")
                            |> Option.defaultValue ""
                    
                        ctx.Response.Redirect (logoutUri + redirectQueryParameter)
                        ctx.HandleResponse()
                        System.Threading.Tasks.Task.CompletedTask))
                |> ignore
            services

        { state with
            ServicesConfig = service::state.ServicesConfig
            AppConfigs = middleware::state.AppConfigs
            CookiesAlreadyAdded = true }

let defaultRoute = "/chickens"
let getUser (ctx: HttpContext) =
    match ctx.User |> Option.ofObj with
    | Some principal when principal.Identity.IsAuthenticated ->
        ApiUser { Name = principal.Claims |> Seq.tryFind (fun c -> c.Type = "name") |> Option.map (fun c -> c.Value) |> Option.defaultValue "unknown" }
    | _ -> 
        Anonymous

open Chickens
let listChickens : HttpHandler =
    fun next (ctx: HttpContext) ->
        ctx.TryGetQueryStringValue "date"
        |> Option.map NotFutureDate.tryParse 
        |> Option.defaultValue (NotFutureDate.today() |> Ok)
        |> function
            | Error _ ->
                redirectTo false (defaultRoute) next ctx
            | Ok date ->
                task {
                    let! chickensWithEggCounts = CompositionRoot.getAllChickens date
                    let model =
                        chickensWithEggCounts
                        |> List.map (fun c ->
                            { Id = c.Chicken.Id
                              Name = c.Chicken.Name
                              ImageUrl = c.Chicken.ImageUrl
                              Breed = c.Chicken.Breed
                              TotalEggCount = c.TotalCount
                              EggCountOnDate = snd c.Count })
                    let user = getUser ctx
                    return! ctx.WriteHtmlStringAsync (Chickens.layout model date |> App.layout user)
                }

let endpointPipe = pipeline {
    plug putSecureBrowserHeaders
    plug head
}

module Authentication =
        
    let challenge : HttpHandler =
        fun next ctx ->
            let returnUrl = 
                ctx.TryGetQueryStringValue "returnUrl"
                |> Option.defaultValue "/"
                |> toAbsolutePath ctx.Request
            task {
                printfn "Using return url: %s" returnUrl
                do! ctx.ChallengeAsync("Auth0", AuthenticationProperties(RedirectUri = returnUrl))
                return! next ctx
            }
            
    let requireLoggedIn = requiresAuthentication challenge
    let logout : HttpHandler =
        Giraffe.Auth.signOut "Auth0"
        >=> Giraffe.Auth.signOut CookieAuthenticationDefaults.AuthenticationScheme
        
let browserRouter = router {
    pipe_through turbolinks
    get "/" (redirectTo false (defaultRoute)) 
    get "/chickens" listChickens
    get "/login" Authentication.challenge
}

let secureBrowserRouter = router {
    pipe_through turbolinks
    pipe_through Authentication.requireLoggedIn
    get "/logout" Authentication.logout
}

let apiErrorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) =
    // do some logging
    let logger = routeInfo.httpContext.GetService<ILogger<IChickensApi>>()
    let msg = sprintf "Error at %s on method %s" routeInfo.path routeInfo.methodName
    logger.LogError(ex, msg)
    // decide whether or not you want to propagate the error to the client
    Ignore

let secureApi : HttpHandler =
    let api' =
        Remoting.createApi()
        |> Remoting.withRouteBuilder Route.builder
        |> Remoting.fromValue CompositionRoot.api
        #if DEBUG
        |> Remoting.withDiagnosticsLogger (printfn "%s")
        #endif
        |> Remoting.withErrorHandler apiErrorHandler
        |> Remoting.buildHttpHandler
    
    Authentication.requireLoggedIn >=> api'
    
let health : HttpHandler =
    let checkHealth : HttpHandler =
        fun next ctx ->
            let healthView (time: DateTime) = sprintf "<div>Healthy at <span id=healthCheckTime>%s</span></div>" (time.ToString("yyyy-MM-dd hh:mm:ss"))
            task {
                let! time = CompositionRoot.healthCheck()
                return! (htmlString (healthView time)) next ctx
            }
            
    router {
        get "/health" checkHealth
    }
    
let notFoundHandler : HttpHandler =
    fun next ctx ->
        task { 
            return! (setStatusCode 404 >=> htmlString "<div>Not Found!</div>" ) next ctx
        }

let webApp =
    choose [
        health
        browserRouter
        secureBrowserRouter
        secureApi
        notFoundHandler
    ]
    
OptionHandler.register()

let errorHandler : ErrorHandler =
    fun exn logger _next ctx ->
        match exn with
        | :? ArgumentException as a ->
            Response.badRequest ctx a.Message
        | _ ->
            let msg = sprintf "Exception for %s%s" ctx.Request.Path.Value ctx.Request.QueryString.Value
            logger.LogError(exn, msg)
            Response.internalError ctx ()

application {
    error_handler errorHandler
    pipe_through endpointPipe
    url ("http://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    use_auth0_open_id
    use_router webApp
    use_cached_static_files_with_max_age CompositionRoot.config.PublicPath 31536000
    use_gzip
    logging ignore
} |> run
