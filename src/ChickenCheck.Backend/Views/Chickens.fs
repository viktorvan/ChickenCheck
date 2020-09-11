module ChickenCheck.Backend.Views.Chickens

open ChickenCheck.Shared
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine

type ChickenDetails =
    { Id: ChickenId
      Name : string
      ImageUrl : ImageUrl option 
      Breed : string
      TotalEggCount : EggCount
      EggCountOnDate : EggCount }

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
        Bulma.subtitle.h3 [
            text.hasTextCentered
            prop.text (sprintf "%s ägg %s" countText dateText)
        ]
    
    let chickenListView = 
        let cardViewRows (chickens: ChickenDetails list) = 
            let cardView (chicken: ChickenDetails) =
 
                let model = 
                    { ChickenCard.Model.Id = chicken.Id
                      ChickenCard.Model.Name = chicken.Name
                      ChickenCard.Model.Breed = chicken.Breed
                      ChickenCard.Model.CurrentDate = currentDate
                      ChickenCard.Model.EggCount = chicken.EggCountOnDate
                      ChickenCard.Model.ImageUrl = chicken.ImageUrl }
                let card = ChickenCard.layout model
                
                Bulma.column [
                    column.is4
                    prop.children [ card ] 
                ]
                
            let cardViewRow rowChickens = List.map cardView rowChickens
            
            chickens
            |> List.sortBy (fun c -> c.Name)
            |> List.batchesOf 3
            |> List.map cardViewRow

        model
        |> cardViewRows
        |> List.map Bulma.columns 
        |> Bulma.container

    let statistics =
        let model =
            {| Chickens = model |> List.map (fun c -> { Statistics.Chicken.Name = c.Name 
                                                        Statistics.Chicken.EggCount = c.TotalEggCount }) |}
        Statistics.layout model
            
    Html.div [
        Bulma.section [
            header
            DatePicker.layout currentDate
            chickenListView
        ]
        Bulma.section [
            statistics
        ]
    ]
    
