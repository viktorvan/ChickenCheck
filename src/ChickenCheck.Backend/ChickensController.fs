module ChickenCheck.Backend.ChickensController

open ChickenCheck.Backend.Views
open ChickenCheck.Backend.Views.Chickens
open ChickenCheck.Shared
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open Saturn
open Giraffe
open ChickenCheck.Backend.Turbolinks

let controller =
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
            plug [All] (CompositionRoot.authorizeUser >=> protectFromForgery >=> turbolinks) 
            update addEgg
            delete removeEgg
        }
    
    let parseQueryDate defaultValue (ctx: HttpContext) =
        ctx.TryGetQueryStringValue "date"
        |> Option.map NotFutureDate.tryParse
        |> Option.defaultValue (defaultValue |> Ok)
    
    let listChickens (ctx: HttpContext) =
        match parseQueryDate (NotFutureDate.today()) ctx with
        | Error _ -> Controller.redirect ctx CompositionRoot.defaultRoute 
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

                return! ctx.WriteHtmlStringAsync
                            (layout model date
                             |> App.layout (CompositionRoot.csrfTokenInput ctx) CompositionRoot.config.Domain (CompositionRoot.getUser ctx))
            }

    controller {
        subController "/eggs" eggsController
        index listChickens
    }

