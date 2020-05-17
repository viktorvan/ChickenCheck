module Server.Startup
open ChickenCheck.Backend
open System
open System.IO

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection

open Giraffe

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.AspNetCore.Http
open ChickenCheck.Domain

let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x
let publicPath = tryGetEnv "public_path" |> Option.defaultValue "../../output/server/public" |> Path.GetFullPath
let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let automationApi : IChickenApi =
    try
        CompositionRoot.chickenApi
    with :? TypeInitializationException as ex ->
        eprintfn "%s" Configuration.ConfigErrorMsg
        raise ex

let debugErrorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) =
    // do some logging
    printfn "Error at %s on method %s" routeInfo.path routeInfo.methodName
    printfn "%O" ex
    // decide whether or not you want to propagate the error to the client
    Ignore

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue automationApi
    #if DEBUG
    |> Remoting.withErrorHandler debugErrorHandler
    |> Remoting.withDiagnosticsLogger (printfn "%s")
    #endif
    |> Remoting.buildHttpHandler


let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    // inject ILogger
    services.AddGiraffe() |> ignore
    tryGetEnv "APPINSIGHTS_INSTRUMENTATIONKEY" |> Option.iter (services.AddApplicationInsightsTelemetry >> ignore)

WebHost
    .CreateDefaultBuilder()
    .UseWebRoot(publicPath)
    .UseContentRoot(publicPath)
    .Configure(Action<IApplicationBuilder> configureApp)
    .ConfigureServices(configureServices)
    .UseUrls("http://0.0.0.0:" + port.ToString() + "/")
    .Build()
    .Run()
