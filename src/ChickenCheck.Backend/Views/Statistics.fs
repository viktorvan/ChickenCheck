module ChickenCheck.Backend.Views.Statistics

open ChickenCheck.Shared
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine

type Chicken =
    { Name: string
      EggCount: EggCount }

let private eggCountView (heading: string) (eggCount: int) =
    Bulma.levelItem [
        text.hasTextCentered
        prop.children [
            Html.div [
                Html.p [
                    prop.classes [ "heading" ]
                    prop.text heading 
                ]
                Bulma.title.p eggCount
            ]
        ]
    ]

let eggCountViews (chickens: Chicken list) =
    
    chickens
    |> List.sortBy (fun c -> c.Name)
    |> List.map (fun c -> eggCountView c.Name c.EggCount.Value)
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
            eggCountView "Totalt" (model.Chickens |> List.sumBy (fun c -> c.EggCount.Value))
            eggCountViews model.Chickens
        ]
    ]
    
