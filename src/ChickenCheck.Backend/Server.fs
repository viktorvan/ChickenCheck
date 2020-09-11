module Server.Startup

open System
open System.Security.Cryptography.X509Certificates
open ChickenCheck.Backend
open ChickenCheck.Backend.Turbolinks
open ChickenCheck.Backend.Views
open ChickenCheck.Backend.Views.Chickens
open Feliz.ViewEngine
open Giraffe
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open Microsoft.AspNetCore.DataProtection
open Saturn
open ChickenCheck.Shared
open FSharp.Control.Tasks.V2.ContextInsensitive
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open ChickenCheck.Backend.SaturnExtensions


let defaultRoute = "/chickens"

let getUser (ctx: HttpContext) =
    match ctx.User |> Option.ofObj with
    | Some principal when principal.Identity.IsAuthenticated ->
        ApiUser
            { Name =
                  principal.Claims
                  |> Seq.tryFind (fun c -> c.Type = "name")
                  |> Option.map (fun c -> c.Value)
                  |> Option.defaultValue "unknown" }
    | _ -> Anonymous
    
let csrfTokenInput (ctx: HttpContext) =
        match ctx.GetService<IAntiforgery>() with
        | null -> failwith "missing Antiforgery feature, setup with Saturn pipeline with 'use_antiforgery'"
        | antiforgery ->
            let tokens = antiforgery.GetAndStoreTokens(ctx)
            Html.input [
                prop.id "RequestVerificationToken"
                prop.name tokens.FormFieldName
                prop.value tokens.RequestToken
                prop.type'.hidden
            ]
                               
let listChickens: HttpHandler =
    fun next (ctx: HttpContext) ->
        ctx.TryGetQueryStringValue "date"
        |> Option.map NotFutureDate.tryParse
        |> Option.defaultValue (NotFutureDate.today () |> Ok)
        |> function
        | Error _ -> redirectTo false (defaultRoute) next ctx
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

                return! ctx.WriteHtmlStringAsync
                            (layout model date
                             |> App.layout (csrfTokenInput ctx) CompositionRoot.config.Domain user)
            }

let listChickens2 (ctx: HttpContext) =
        ctx.TryGetQueryStringValue "date"
        |> Option.map NotFutureDate.tryParse
        |> Option.defaultValue (NotFutureDate.today () |> Ok)
        |> function
        | Error _ -> Controller.redirect ctx (defaultRoute) 
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

                return! ctx.WriteHtmlStringAsync
                            (layout model date
                             |> App.layout (csrfTokenInput ctx) CompositionRoot.config.Domain user)
            }

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
    
let eggsController (chickenId: string) =
    let addEgg ctx (date: string) =
        task {
            let chickenId = ChickenId.parse chickenId
            let date = NotFutureDate.parse date
            let! _ = CompositionRoot.addEgg (chickenId, date)
            return! Response.accepted ctx ()
        }
        
    let removeEgg (ctx: HttpContext) (date: string) = 
        task {
            let chickenId = ChickenId.parse chickenId
            let date = NotFutureDate.parse date
            let! _ = CompositionRoot.removeEgg (chickenId, date)
            return! Response.accepted ctx ()
        }
    
    controller {
        plug [All] (Authentication.authorizeUser CompositionRoot.config.Authentication.AccessRole >=> protectFromForgery >=> turbolinks) 
        update addEgg
        delete removeEgg
    }
    
let chickensController =
    controller {
        subController "/eggs" eggsController
        index listChickens2
    }

let browserRouter =
    router {
        pipe_through turbolinks
        get "/" (redirectTo false (defaultRoute))
        get "/test" (fun next ctx -> Response.accepted ctx ())
//        get "/chickens" listChickens
        get "/login" Authentication.challenge
        get "/logout" Authentication.logout
        forward "/chickens" chickensController
    }
    

let secureBrowserRouter =
    router {
        pipe_through turbolinks
        pipe_through (Authentication.authorizeUser CompositionRoot.config.Authentication.AccessRole)
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
//             secureBrowserRouter
//             secureApi
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
    url ("http://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    service_config configureServices
    use_auth0_open_id
    use_antiforgery
    use_router webApp
    use_cached_static_files_with_max_age CompositionRoot.config.PublicPath 31536000
    use_gzip
    logging ignore
}
|> run
