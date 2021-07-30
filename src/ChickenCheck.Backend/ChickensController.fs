module ChickenCheck.Backend.ChickensController

open ChickenCheck.Backend.Views
open FSharp.Control.Tasks.Affine
open Microsoft.AspNetCore.Http
open Saturn
open Giraffe

let controller =
    let eggsController (chickenId: string) =
        let addEgg ctx (date: string) =
            task {
                let chickenId = ChickenId.parse chickenId
                let date = NotFutureDate.parse date
                let! _ = CompositionRoot.addEgg chickenId date
                return! Response.accepted ctx ()
            }
        let addEggX ctx (date: string) =
            task {
                let chickenId = ChickenId.parse chickenId
                let date = NotFutureDate.parse date
                let! addEgg = CompositionRoot.addEgg chickenId date
                let! chicken = CompositionRoot.getChicken chickenId date
                let chicken = chicken |> Option.defaultWith (fun _ -> invalidArg "ChickenId" "Invalid chicken-id")
                let model = 
                    { ChickenCardModel.Id = chickenId
                      Name = chicken.Name
                      Breed = chicken.Breed
                      ImageUrl = chicken.ImageUrl
                      EggCount = chicken.EggCount
                      CurrentDate = date }
                return
                    ChickenCard.layout model
                    |> CompositionRoot.writeHtmlFragment ctx
            }
            
            
        let removeEgg (ctx: HttpContext) (date: string) = 
            task {
                let chickenId = ChickenId.parse chickenId
                let date = NotFutureDate.parse date
                let! _ = CompositionRoot.removeEgg chickenId date
                return! Response.accepted ctx ()
            }
        
        controller {
            plug [All] (CompositionRoot.authorizeUser >=> protectFromForgery) 
            update addEggX
            delete removeEgg
        }
    
    let parseQueryDate defaultValue (ctx: HttpContext) =
        ctx.TryGetQueryStringValue "date"
        |> Option.map NotFutureDate.tryParse
        |> Option.defaultValue (defaultValue |> Ok)
    
    let listChickens (ctx: HttpContext) =
        match parseQueryDate (NotFutureDate.today()) ctx with
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

    controller {
        subController "/eggs" eggsController
        index listChickens
    }

