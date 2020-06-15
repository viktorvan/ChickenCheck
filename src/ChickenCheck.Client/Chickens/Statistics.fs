module ChickenCheck.Client.Statistics

open Feliz
open Feliz.Bulma
open ChickenCheck.Shared
open ChickenCheck.Client.Chickens

let eggCountViews (chickens: ChickenDetails list) =
    let eggCountView (chicken: ChickenDetails) =
        Bulma.levelItem [
            text.hasTextCentered
            prop.children [
                Html.div [
                    Html.p [
                        prop.classes [ "heading" ]
                        prop.text chicken.Name
                    ]
                    Bulma.title.p chicken.TotalEggCount.Value
                ]
            ]
        ]
    
    chickens
    |> List.sortBy (fun c -> c.Name)
    |> List.map eggCountView
    |> Bulma.level
        

let render (props: {| Chickens: ChickenDetails list |}) =
    let header = 
        Bulma.text.p [
            Bulma.size.isSize2
            text.hasTextCentered
            prop.text "Hur mycket har de värpt totalt?"
        ]
    Bulma.container [
        header
        eggCountViews props.Chickens
    ]
    
let statistics = React.memo("ChickenStatistics", render)
