module ChickenCheck.Backend.Views.Statistics

open ChickenCheck.Shared
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine

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
        

let layout (model: {| Chickens: Chicken list |}) =
    let header = 
        Bulma.subtitle.h3 [
            text.hasTextCentered
            prop.text "Hur mycket har de värpt totalt?"
        ]
    Bulma.container [
        prop.style [ style.marginTop (length.px 10) ]
        prop.children [
            header
            eggCountViews model.Chickens
        ]
    ]
    
