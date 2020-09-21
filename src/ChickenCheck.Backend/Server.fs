module Server.Startup

open System
open System.Security.Cryptography.X509Certificates
open ChickenCheck.Backend
open ChickenCheck.Backend.Turbolinks
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open Microsoft.AspNetCore.DataProtection
open Saturn
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.Logging
open ChickenCheck.Backend.SaturnExtensions


let setScheme: HttpHandler =
    fun next (ctx: HttpContext) ->
        task {
            if ctx.Request.Headers.["x-forwarded-proto"] = StringValues "https"
            then ctx.Request.Scheme <- "https"
            return! next ctx
        }

let endpointPipe =
    pipeline {
        plug putSecureBrowserHeaders
        plug setScheme
        plug head
    }
    
let routeToWWW : HttpHandler =
    fun next ctx ->
        if not (ctx.Request.Host.Value.StartsWith("www", StringComparison.InvariantCultureIgnoreCase) || ctx.Request.Host.Value.StartsWith("localhost", StringComparison.InvariantCultureIgnoreCase)) then 
            (redirectTo true ("https://www." + ctx.Request.Host.Value + CompositionRoot.defaultRoute()) next ctx)
        else next ctx
    
let browserRouter =
    router {
        pipe_through routeToWWW
        pipe_through turbolinks
        get "/" (redirectTo false (CompositionRoot.defaultRoute()))
        get "/login" Authentication.challenge
        get "/logout" Authentication.logout
        forward "/eggs" EggsController.browserController
    }
    
let apiRouter =
    router {
        forward "/api/eggs" EggsController.apiController
    }

let health: HttpHandler =
    let checkHealth: HttpHandler =
        fun next ctx ->
            let healthView (time: DateTime) =
                sprintf
                    "<div>Healthy at <span id=healthCheckTime>%s</span></div>"
                    (time.ToString("yyyy-MM-dd hh:mm:ss"))

            task {
                let! time = CompositionRoot.healthCheck ()
                return! (htmlString (healthView time)) next ctx
            }

    router { get "/health" checkHealth }

let notFoundHandler: HttpHandler =
    fun next ctx ->
        task {
            return! (setStatusCode 404
                     >=> htmlString "<div>Not Found!</div>")
                        next
                        ctx
        }

let webApp =
    choose [ health
             browserRouter
             apiRouter
             notFoundHandler ]

let configureServices (services: IServiceCollection) =
    let cert = new X509Certificate2(CompositionRoot.config.DataProtection.Certificate, CompositionRoot.config.DataProtection.CertificatePassword)
    services
        .AddDataProtection()
        .PersistKeysToFileSystem(CompositionRoot.config.DataProtection.Path)
        .ProtectKeysWithCertificate(cert) |> ignore
    services

OptionHandler.register ()

let errorHandler: ErrorHandler =
    fun exn logger _next ctx ->
        match exn with
        | :? ArgumentException as a -> Response.badRequest ctx a.Message
        | _ ->
            let msg =
                sprintf "Exception for %s%s" ctx.Request.Path.Value ctx.Request.QueryString.Value

            logger.LogError(exn, msg)
            Response.internalError ctx ()
            
application {
    error_handler errorHandler
    pipe_through endpointPipe
    url (CompositionRoot.config.RequestScheme + "://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    service_config configureServices
    use_basic_auth
//    use_antiforgery
    use_router webApp
    use_cached_static_files_with_max_age CompositionRoot.config.PublicPath 31536000
    use_gzip
    logging ignore
}
|> run
