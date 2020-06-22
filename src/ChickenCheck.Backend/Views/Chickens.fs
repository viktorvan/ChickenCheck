module ChickenCheck.Backend.Views.Chickens

open ChickenCheck.Shared
open Feliz.ViewEngine
open ChickenCheck.Backend
open Feliz.Bulma.ViewEngine

type ChickenDetails =
    { Id: ChickenId
      Name : string
      ImageUrl : ImageUrl option 
      Breed : string
      TotalEggCount : EggCount
      EggCountOnDate : EggCount }

let layout (model:Map<ChickenId, ChickenDetails>) currentDate =
    let header =
        Bulma.subtitle.h2 [
            text.hasTextCentered
            prop.text "Vem värpte idag?"
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
        |> Map.values
        |> cardViewRows
        |> List.map Bulma.columns 
        |> Bulma.container

//    let statistics =
//        model
//        |> Option.map (fun chickens ->
//            let props =
//                let chickens = chickens |> Map.values
//                {| Chickens = chickens |> List.map (fun c -> { Name = c.Name 
//                                                               EggCount = c.TotalEggCount }) |}
//            Statistics.statistics props)
//        |> Option.defaultValue Html.none 
        
            
    Html.div [
//        PageLoader.pageLoader [
//            pageLoader.isInfo
//            if not (model.Chickens |> Deferred.resolved) then pageLoader.isActive 
//        ]
        Bulma.section [
            header
            DatePicker.layout currentDate
            chickenListView
//            statistics
        ]
    ]
    
