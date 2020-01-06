module ChickenCheck.Backend.Functions

open Giraffe
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.Azure.WebJobs
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http
open ChickenCheck.Backend.CompositionRoot
open System
open Microsoft.Extensions.Logging


let debugErrorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) = 
    // do some logging
    printfn "Error at %s on method %s" routeInfo.path routeInfo.methodName
    printfn "%O" ex
    // decide whether or not you want to propagate the error to the client
    Ignore

let webApp : HttpHandler =
    let api =
        Remoting.createApi()
        |> Remoting.fromValue chickenApi
        |> Remoting.withRouteBuilder Api.routeBuilder
        #if DEBUG
        |> Remoting.withErrorHandler debugErrorHandler
        |> Remoting.withDiagnosticsLogger (printfn "%s")
        #endif
        |> Remoting.buildHttpHandler

    choose [ api ]

module Http =
    [<FunctionName("api")>]
    let runApi ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, Route="{*path}")>] req: HttpRequest, context: ExecutionContext) =
        let hostingEnvironment = req.HttpContext.GetHostingEnvironment()
        hostingEnvironment.ContentRootPath <- context.FunctionAppDirectory
        let func = Some >> System.Threading.Tasks.Task.FromResult
        async {
            let! _ = webApp func req.HttpContext |> Async.AwaitTask
            req.HttpContext.Response.Body.Flush() //workaround https://github.com/giraffe-fsharp/Giraffe.AzureFunctions/issues/1
        } |> Async.StartAsTask

    [<FunctionName("status")>]
    let runStatus ([<HttpTrigger(Extensions.Http.AuthorizationLevel.Anonymous, "get")>] req : HttpRequest) =
        Microsoft.AspNetCore.Mvc.ContentResult(Content = getStatus(), ContentType = "text/html")

    [<FunctionName("keepAlive")>]
    let runKeepAlive ([<TimerTrigger("0 */4 * * * *")>] myTimer, log : ILogger) =
        DateTime.Now.ToString()
        |> sprintf "Executed at %s"
        |> log.LogInformation

