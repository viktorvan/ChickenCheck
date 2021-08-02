module ChickenCheck.Backend.Statistics

open ChickenCheck.Backend.Views
open FSharp.Control.Tasks.Affine
open Giraffe

    
let statistics : HttpHandler =
    fun next ctx ->
        task {
            match ctx.GetQueryStringValue "date" with
            | Error _ ->
                return! RequestErrors.BAD_REQUEST "query string date is required" next ctx
            | Ok dateStr ->
                let date = NotFutureDate.parse dateStr
                let! allChickens = CompositionRoot.getAllChickensWithEggCounts date
                let model =
                    {| Chickens = allChickens |> List.map (fun c -> { Name = c.Chicken.Name 
                                                                      EggCount = c.TotalCount })
                       CurrentDate = date |}
                    
                return!
                    Statistics.layout model
                    |> CompositionRoot.writeHtml ctx
        }
