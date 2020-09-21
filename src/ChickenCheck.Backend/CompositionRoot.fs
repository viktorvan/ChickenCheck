module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Shared
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open System
open Microsoft.AspNetCore.Antiforgery
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Giraffe
open Feliz.ViewEngine

let config = Configuration.config.Value

// Helpers
//let csrfTokenInput (ctx: HttpContext) =
//        match ctx.GetService<IAntiforgery>() with
//        | null -> failwith "missing Antiforgery feature, setup with Saturn pipeline with 'use_antiforgery'"
//        | antiforgery ->
//            let tokens = antiforgery.GetAndStoreTokens(ctx)
//            Html.input [
//                prop.id "RequestVerificationToken"
//                prop.name tokens.FormFieldName
//                prop.value tokens.RequestToken
//                prop.type'.hidden
//            ]

let defaultRoute() = sprintf "/eggs/%s" (NotFutureDate.today().ToString())

//let authorizeUser : HttpHandler = (Authentication.authorizeUser config.Authentication.AccessRole)

//let getUser (ctx: HttpContext) =
//    match ctx.User |> Option.ofObj with
//    | Some principal when principal.Identity.IsAuthenticated ->
//        ApiUser
//            { Name =
//                  principal.Claims
//                  |> Seq.tryFind (fun c -> c.Type = "name")
//                  |> Option.map (fun c -> c.Value)
//                  |> Option.defaultValue "unknown" }
//    | _ -> Anonymous
    
    
// services
let chickenStore = Database.ChickenStore config.ConnectionString

// workflows
let healthCheck() = Workflows.healthCheck chickenStore ()
let getAllChickens date = Workflows.getAllChickens chickenStore date
let addEgg (chicken, date) = Workflows.addEgg chickenStore chicken date
let removeEgg (chicken, date) = Workflows.removeEgg chickenStore chicken date
let removeAllEggs date = Workflows.removeAllEggs chickenStore date

