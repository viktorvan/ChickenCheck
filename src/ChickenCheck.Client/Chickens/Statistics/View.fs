namespace ChickenCheck.Client.Statistics

open Feliz
open Feliz.Bulma
open ChickenCheck.Shared
open ChickenCheck.Client
open ChickenCheck.Client.Chickens

module View =
    let private eggCountViews (chickens: ChickenDetails list) =
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
        
//    let view = FunctionComponent.Of((fun (props: {| Chickens: ChickenDetails list |}) ->
//            view' props.Chickens), "ChickenStatistics", equalsButFunctions)
    let view = Utils.elmishView "ChickenStatistics" (fun (props: {| Chickens: ChickenDetails list |}) -> view' props.Chickens)
