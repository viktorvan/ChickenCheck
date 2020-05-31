module ChickenCheck.Client.Statistics 

open Utils
open Feliz
open Feliz.Bulma


let private eggCountViews chickens =
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
    |> Map.values
    |> List.sortBy (fun c -> c.Name)
    |> List.map eggCountView
    |> Bulma.level
        
let view' chickens =
    let header = 
        Bulma.text.p [
            Bulma.size.isSize2
            text.hasTextCentered
            prop.text "Hur mycket har de värpt totalt?"
        ]
    Bulma.container [
        header
        eggCountViews chickens
    ]
    
let view = elmishView "Statistics" (fun (model: ChickensPageModel) -> 
    model.Chickens
    |> Deferred.map(view')
    |> Deferred.defaultValue Html.none
    )
