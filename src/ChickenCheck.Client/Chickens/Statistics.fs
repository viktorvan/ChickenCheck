module ChickenCheck.Client.Chickens.Statistics

open Feliz
open Feliz.Bulma
open ChickenCheck.Shared
open ChickenCheck.Client

type Chicken =
    { Name: string
      EggCount: EggCount }

let eggCountViews (chickens: Chicken list) =
    let eggCountView (chicken: Chicken) =
        Bulma.levelItem [
            text.hasTextCentered
            prop.children [
                Html.div [
                    Html.p [
                        prop.classes [ "heading" ]
                        prop.text chicken.Name
                    ]
                    Bulma.title.p chicken.EggCount.Value
                ]
            ]
        ]
    
    chickens
    |> List.sortBy (fun c -> c.Name)
    |> List.map eggCountView
    |> Bulma.level
        

let render (props: {| Chickens: Chicken list |}) =
    let header = 
        Bulma.subtitle.h3 [
            text.hasTextCentered
            prop.text "Hur mycket har de värpt totalt?"
        ]
    Bulma.container [
        prop.style [ style.marginTop (length.px 10) ]
        prop.children [
            header
            eggCountViews props.Chickens
        ]
    ]
    
let statistics = Utils.memo "ChickenStatistics" render
