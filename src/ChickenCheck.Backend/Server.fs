module Server.Startup
open System.Security.Claims
open ChickenCheck.Backend
open System
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder

open Giraffe
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.IdentityModel.Tokens
open Saturn

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.AspNetCore.Http
open ChickenCheck.Shared

type Saturn.Application.ApplicationBuilder with
  [<CustomOperation("use_token_authentication")>]
  member __.UseTokenAuthentication(state: ApplicationState) =
    let middleware (app: IApplicationBuilder) =
      app.UseAuthentication()
 
    let service (services : IServiceCollection) =
      services.AddAuthentication(fun options ->
        options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
        options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme
      ).AddJwtBearer(fun options ->
        options.Authority <- sprintf "https://%s/" CompositionRoot.config.Authentication.Domain
        options.Audience <- CompositionRoot.config.Authentication.Audience
        options.TokenValidationParameters <- TokenValidationParameters(
          NameClaimType = ClaimTypes.NameIdentifier
        )
      ) |> ignore
      services
 
    { state with ServicesConfig = service::state.ServicesConfig ; AppConfigs = middleware::state.AppConfigs ; CookiesAlreadyAdded = true }

let errorHandler (ex: Exception) (routeInfo: RouteInfo<HttpContext>) =
    // do some logging
    let logger = routeInfo.httpContext.GetService<ILogger<IChickenApi>>()
    let msg = sprintf "Error at %s on method %s" routeInfo.path routeInfo.methodName
    logger.LogError(ex, msg)
    // decide whether or not you want to propagate the error to the client
    Ignore
    
let createWithLogger createFunc (ctx: HttpContext) =
    try
        let logger = ctx.GetService<ILogger<IChickenApi>>()
        createFunc logger
    with :? TypeInitializationException as ex ->
        eprintfn "%s" Configuration.ConfigErrorMsg
        raise ex

let buildApi() =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    #if DEBUG
    |> Remoting.withDiagnosticsLogger (printfn "%s")
    #endif
    #if !DEBUG
    |> Remoting.withErrorHandler prodErrorHandler
    #endif

let chickenApi : HttpHandler =
    buildApi()
    |> Remoting.fromContext (createWithLogger CompositionRoot.chickenApi)
    |> Remoting.buildHttpHandler
    
let chickenEditApi : HttpHandler =
    buildApi()
    |> Remoting.fromContext (createWithLogger CompositionRoot.chickenEditApi)
    |> Remoting.buildHttpHandler
    
let requireLoggedIn = pipeline { requires_authentication (Giraffe.Auth.challenge JwtBearerDefaults.AuthenticationScheme) }
    
let completeApi : HttpHandler = choose [
    chickenApi
    requireLoggedIn >=> chickenEditApi
]

let configureApp (app : IApplicationBuilder) =
    app.UseDefaultFiles()
       .UseStaticFiles()
       .UseGiraffe chickenApi

OptionHandler.register()

let app = application {
    url ("http://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    use_token_authentication
    use_router completeApi
    use_static CompositionRoot.config.PublicPath
    memory_cache
    use_gzip
    logging ignore
}

run app
