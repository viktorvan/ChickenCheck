module ChickenCheck.Client.Statistics 

open Fulma
open Fable.React
open Utils

let private eggCountView (chicken: ChickenDetails) =
    let totalCount = chicken.TotalEggCount.Value |> string
    Level.item [ Level.Item.HasTextCentered ]
        [ div []
            [ Level.heading [] [ chicken.Name.Value |> str ] 
              Level.title [] [ str totalCount ] ] ]

let view = elmishView "Statistics" (fun (model: ChickensModel) -> 
    let allCounts =
        model.Chickens
        |> Map.values
        |> List.map eggCountView
        |> Level.level []
        
    Container.container []
        [ Text.p 
            [ Modifiers 
                [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                  Modifier.TextSize (Screen.All, TextSize.Is2)] ] 
            [ str "Hur mycket har de värpt totalt?" ] 
          allCounts ]
    )
