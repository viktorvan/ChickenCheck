module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open System
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Giraffe
open Giraffe.ViewEngine

let config = Configuration.config.Value

// Helpers
let csrfTokens (ctx: HttpContext) =
        match ctx.GetService<IAntiforgery>() with
        | null -> failwith "missing Antiforgery feature, setup with Saturn pipeline with 'use_antiforgery'"
        | antiforgery -> antiforgery.GetAndStoreTokens(ctx)
            
let csrfTokenInput (tokens:AntiforgeryTokenSet) (ctx: HttpContext) =
        input [
            _id "RequestVerificationToken"
            _name tokens.FormFieldName
            _value tokens.RequestToken
            _type "hidden"
        ]

let defaultRoute() = sprintf "/eggs/%s" (NotFutureDate.today().ToString())

let authorizeUser : HttpHandler = (Authentication.authorizeUser config.Authentication.AccessRole)

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
    
    
module Services =
    let connectionString = config.ConnectionString
    let db = Database.Database connectionString


// workflows
let healthCheck = Workflows.healthCheck Services.db.TestDatabaseAccess
let getAllChickensWithEggCounts = Workflows.getAllChickensWithEggCounts Services.db.GetAllChickens Services.db.GetChickenEggCount Services.db.GetChickenTotalEggCount
let getChicken = Workflows.getChickenWithEggCount Services.db.GetChicken Services.db.GetChickenEggCount
let getTotalEggCountOnDate = Workflows.getTotalEggCountOnDate Services.db.GetTotalEggCount
let addEgg = Workflows.addEgg Services.db.AddEgg 
let removeEgg = Workflows.removeEgg Services.db.RemoveEgg


// views
let writeHtml ctx content =
    let csrfToken = (csrfTokens ctx).RequestToken
    content 
    |> Views.App.layout csrfToken config.BasePath config.Domain (getUser ctx)
    |> ctx.WriteHtmlStringAsync
let writeHtmlFragment (ctx: HttpContext) content =
    content
    |> RenderView.AsString.htmlNode
    |> ctx.WriteHtmlStringAsync
let setHxTrigger (ctx: HttpContext) evt =
    ctx.SetHttpHeader("HX-Trigger", evt)

// logging
let logInfo (ctx: HttpContext) msg =
    let logger = ctx.GetLogger "chickencheck-logger"
    logger.LogInformation msg