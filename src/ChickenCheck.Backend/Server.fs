module Server.Startup

open System
open ChickenCheck.Backend
open ChickenCheck.Backend.Turbolinks
open ChickenCheck.Backend.Views
open ChickenCheck.Backend.Views.Chickens
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open Saturn
open ChickenCheck.Shared
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open ChickenCheck.Backend.SaturnExtensions


let defaultRoute = "/chickens"
let getUser (ctx: HttpContext) =
    match ctx.User |> Option.ofObj with
    | Some principal when principal.Identity.IsAuthenticated ->
        ApiUser { Name = principal.Claims |> Seq.tryFind (fun c -> c.Type = "name") |> Option.map (fun c -> c.Value) |> Option.defaultValue "unknown" }
    | _ -> 
        Anonymous

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
                    return! ctx.WriteHtmlStringAsync (layout model date |> App.layout user)
                }

let setScheme : HttpHandler =
    fun next (ctx: HttpContext) ->
        task {
            if ctx.Request.Headers.["x-forwarded-proto"] = StringValues "https" then ctx.Request.Scheme <- "https"
            return! next ctx
        }
let endpointPipe = pipeline {
    plug putSecureBrowserHeaders
    plug setScheme
    plug head
}

let browserRouter = router {
    pipe_through turbolinks
    get "/" (redirectTo false (defaultRoute)) 
    get "/chickens" listChickens
    get "/login" Authentication.challenge
    get "/logout" (Authentication.requiresLoggedIn >> Authentication.logout)
}

let secureBrowserRouter = router {
    pipe_through turbolinks
    pipe_through (Authentication.authorizeUser CompositionRoot.config.Authentication.AccessRole)
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
    
    Authentication.authorizeUser CompositionRoot.config.Authentication.AccessRole >=> api'
    
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
