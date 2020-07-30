module Server.Startup

open System
open System.IO
open System.Security.Claims
open ChickenCheck.Backend
open ChickenCheck.Backend.Turbolinks
open ChickenCheck.Backend.Views
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Giraffe
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Primitives
open Microsoft.IdentityModel.Tokens
open Microsoft.Net.Http.Headers
open Saturn
open ChickenCheck.Shared
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open Fable.Remoting.Server
open Fable.Remoting.Giraffe

type Saturn.Application.ApplicationBuilder with
//    [<CustomOperation("use_token_authentication")>]
//    member __.UseTokenAuthentication(state: ApplicationState) =
//        let middleware (app: IApplicationBuilder) =
//            app.UseAuthentication()
// 
//        let service (services : IServiceCollection) =
//            services.AddAuthentication(fun options ->
//              options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
//              options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme
//            ).AddJwtBearer(fun options ->
//                options.Authority <- sprintf "https://%s/" CompositionRoot.config.Authentication.Domain
//                options.Audience <- CompositionRoot.config.Authentication.Audience
//                options.TokenValidationParameters <- TokenValidationParameters(
//                    NameClaimType = ClaimTypes.NameIdentifier
//                )
//            ) |> ignore
//            services
//     
//        { state with ServicesConfig = service::state.ServicesConfig ; AppConfigs = middleware::state.AppConfigs ; CookiesAlreadyAdded = true }
        
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

let requireLoggedIn = pipeline { requires_authentication (Giraffe.Auth.challenge JwtBearerDefaults.AuthenticationScheme) }

let defaultRoute = "/chickens"

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
                            c.Chicken.Id, 
                            { Id = c.Chicken.Id
                              Name = c.Chicken.Name
                              ImageUrl = c.Chicken.ImageUrl
                              Breed = c.Chicken.Breed
                              TotalEggCount = c.TotalCount
                              EggCountOnDate = snd c.Count })
                        |> Map.ofList
                    return! ctx.WriteHtmlStringAsync (Chickens.layout model date |> App.layout Anonymous)
                }

let endpointPipe = pipeline {
    plug putSecureBrowserHeaders
    plug head
}

let browser = router {
    pipe_through turbolinks
    get "/" (redirectTo false (defaultRoute)) 
    get "/chickens" listChickens
}

let apiErrorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) =
    // do some logging
    let logger = routeInfo.httpContext.GetService<ILogger<IChickensApi>>()
    let msg = sprintf "Error at %s on method %s" routeInfo.path routeInfo.methodName
    logger.LogError(ex, msg)
    // decide whether or not you want to propagate the error to the client
    Ignore

let api : HttpHandler =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue CompositionRoot.api
    #if DEBUG
    |> Remoting.withDiagnosticsLogger (printfn "%s")
    #endif
    |> Remoting.withErrorHandler apiErrorHandler
    |> Remoting.buildHttpHandler
    
    
let health : HttpHandler =
    let checkHealth : HttpHandler =
        fun next ctx ->
            task {
                do! CompositionRoot.healthCheck()
                return! setStatusCode 200 next ctx
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
        api
        browser
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
            

let app = application {
    error_handler errorHandler
    pipe_through endpointPipe
    url ("http://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
//    use_token_authentication
    use_router webApp
    use_cached_static_files_with_max_age CompositionRoot.config.PublicPath 31536000
    use_gzip
    logging ignore
}

run app
