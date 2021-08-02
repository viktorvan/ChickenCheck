namespace ChickenCheck.Backend.Views

open ChickenCheck.Backend
open Giraffe.ViewEngine

type StatisticsModel =
    { Name: string
      EggCount: EggCount }

module Statistics =
    let private eggCountView (heading: string) (eggCount: int) =
        div [ _class "level-item" ] 
            [
                div [] 
                    [
                        p [ _class "heading" ] [ str heading ]
                        p [ _class "title" ] [ str $"%i{eggCount}" ]
                    ]
            ]

    let eggCountViews (chickens: StatisticsModel list) =
        
        chickens
        |> List.sortBy (fun c -> c.Name)
        |> List.map (fun c -> eggCountView c.Name c.EggCount.Value)
        |> div [ _class "level has-text-centered" ] 
            

    let layout (model: {| Chickens: StatisticsModel list
                          CurrentDate: NotFutureDate |}) =
        let header = h3 [ _class "subtitle has-text-centered" ] [ str "Hur mycket har de värpt totalt?" ]
        div 
            [
                Htmx.hxGet $"/statistics?date=%s{model.CurrentDate.ToUrlString()}"
                Htmx.hxTrigger "updatedEggs from:body"
                Htmx.hxSelect ".container.statistics"
                _class "container statistics"
                _style "margin-top: 10px;"
            ]
            [
                header
                div [ _class "level has-text-centered" ] [ eggCountView "Totalt" (model.Chickens |> List.sumBy (fun c -> c.EggCount.Value)) ]
                eggCountViews model.Chickens
            ]
        
