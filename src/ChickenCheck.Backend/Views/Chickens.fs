namespace ChickenCheck.Backend.Views

open ChickenCheck.Backend
open ChickenCheck.Backend.Views
open ChickenCheck.Backend.Extensions
open Giraffe.ViewEngine

type ChickenDetails =
    { Id: ChickenId
      Name : string
      ImageUrl : ImageUrl option 
      Breed : string
      TotalEggCount : EggCount
      EggCountOnDate : EggCount }

module Chickens =
    let layout (model:ChickenDetails list) currentDate =
        let header =
            let sum = model |> List.sumBy (fun c -> c.EggCountOnDate.Value)
            let dateText =
                match currentDate with
                | x when x = NotFutureDate.today() -> "idag"
                | x -> (NotFutureDate.toDateTime x).ToString("d/M")
            let countText =
                match sum with
                | 0 -> "Inga"
                | 1 -> "Ett"
                | 2 -> "Två"
                | 3 -> "Tre"
                | 4 -> "Fyra"
                | 5 -> "Fem"
                | 6 -> "Sex"
                | 7 -> "Sju"
                | 8 -> "Åtta"
                | 9 -> "Nio"
                | 10 -> "Tio"
                | 11 -> "Elva"
                | 12 -> "Tolv"
                | x -> x.ToString()
            h3 [ _class "subtitle has-text-centered" ]
               [ str $"%s{countText} ägg %s{dateText}" ]
        
        let chickenListView = 
            let cardViewRows (chickens: ChickenDetails list) = 
                let cardView (chicken: ChickenDetails) =
     
                    let model = 
                        { Id = chicken.Id
                          Name = chicken.Name
                          Breed = chicken.Breed
                          CurrentDate = currentDate
                          EggCount = chicken.EggCountOnDate
                          ImageUrl = chicken.ImageUrl }
                    
                    div [ _class "column is-4" ]
                        [ ChickenCard.layout model ]
                    
                let cardViewRow rowChickens = List.map cardView rowChickens
                
                chickens
                |> List.sortBy (fun c -> c.Name)
                |> List.batchesOf 3
                |> List.map cardViewRow

            model
            |> cardViewRows
            |> List.map (div [ _class "columns" ]) 
            |> div [ _class "container" ]

        let statistics =
            let model =
                {| Chickens = model |> List.map (fun c -> { Statistics.Chicken.Name = c.Name 
                                                            Statistics.Chicken.EggCount = c.TotalEggCount }) |}
            Statistics.layout model
                
        div []
            [
                section [ _class "section" ] [
                    header
                    DatePicker.layout currentDate
                    chickenListView
                ]
                section [ _class "section" ] [
                    statistics
                ]
            ]
        
