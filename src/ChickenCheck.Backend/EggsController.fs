module ChickenCheck.Backend.EggsController

open ChickenCheck.Backend.Views
open FSharp.Control.Tasks.Affine
open Microsoft.AspNetCore.Http
open Saturn

    
let controller =
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

                return! 
                    Chickens.layout model date 
                    |> CompositionRoot.writeHtml ctx
            }
            
    let indexChickens ctx =
        Controller.redirect ctx (CompositionRoot.defaultRoute())

    controller {
        index indexChickens
        show listChickens
    }

