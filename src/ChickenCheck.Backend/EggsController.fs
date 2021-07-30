module ChickenCheck.Backend.EggsController

open System
open ChickenCheck.Backend.Views
open ChickenCheck.Backend.Views.Chickens
open FSharp.Control.Tasks.Affine
open Microsoft.AspNetCore.Http
open Saturn
open Giraffe

type EggRequest =
    { ChickenIds: System.Guid[] }
    
let controller =
    let addEggs (ctx: HttpContext) (date: string) =
        let date = NotFutureDate.parse date
        task {
            let! request = ctx.BindModelAsync<EggRequest>()
            if isNull request.ChickenIds || Array.length request.ChickenIds < 1 then 
                return! Response.badRequest ctx ()
            else
                try
                    let! saveEggs = 
                        request.ChickenIds
                        |> Array.map ChickenId.create
                        |> Array.map (fun id -> CompositionRoot.addEgg (id, date))
                        |> Async.Parallel
                    return! Response.accepted ctx ()
                with _ -> return! Response.badRequest ctx ()
        }
        
    let deleteEggs (ctx: HttpContext) (date: string) = 
        task {
            let date = NotFutureDate.parse date
            let! _ = CompositionRoot.removeAllEggs date
            return! Response.accepted ctx ()
        }
        
    let listChickens (ctx: HttpContext) date =
        match NotFutureDate.tryParse date with
        | Error _ -> Controller.redirect ctx (CompositionRoot.defaultRoute())
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
                             |> App.layout (CompositionRoot.csrfTokenInput ctx) CompositionRoot.config.BasePath CompositionRoot.config.Domain (CompositionRoot.getUser ctx))
            }
            
    let indexChickens ctx =
        Controller.redirect ctx (CompositionRoot.defaultRoute())

    controller {
        index indexChickens
        show listChickens
//        update addEggs
//        delete deleteEggs
    }

