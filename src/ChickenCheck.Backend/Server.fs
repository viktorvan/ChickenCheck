module Server.Startup
open System.Security.Claims
open ChickenCheck.Backend
open System
open ChickenCheck.Backend.Views
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder

open Giraffe
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.IdentityModel.Tokens
open Saturn

open Microsoft.AspNetCore.Http
open ChickenCheck.Shared
open FSharp.Control.Tasks.V2.ContextInsensitive

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

let requireLoggedIn = pipeline { requires_authentication (Giraffe.Auth.challenge JwtBearerDefaults.AuthenticationScheme) }

let browser = pipeline {
    plug acceptHtml
    plug putSecureBrowserHeaders
    set_header "x-pipeline-type" "Browser"
}

open Chickens
let chickens : HttpHandler =
    handleContext 
        (fun ctx ->
            task {
                printfn "***path: %s%s\n" ctx.Request.Path.Value ctx.Request.QueryString.Value
                let maybeDate = ctx.TryGetQueryStringValue "date"
                let date = maybeDate |> Option.bind NotFutureDate.parse |> Option.defaultValue NotFutureDate.today
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
                          EggCountOnDate = snd c.Count
                          IsLoading = false })
                   |> Map.ofList
               return! ctx.WriteHtmlStringAsync (Chickens.layout model date |> App.layout Anonymous)
            })

let defaultView = router {
    get "/" (redirectTo false "/chickens")
    get "/index.html" (redirectTo false "/")
    get "/default.html" (redirectTo false "/")
}

let browserRouter = router {
    pipe_through browser
    pipe_through Turbolinks.turbolinks
    forward "" defaultView
    get "/chickens" chickens
}

OptionHandler.register()

let app = application {
    url ("https://*:" + CompositionRoot.config.ServerPort.ToString() + "/")
    force_ssl
    use_token_authentication
    use_router browserRouter
    use_static CompositionRoot.config.PublicPath
    memory_cache
    use_gzip
    logging ignore
}

run app
