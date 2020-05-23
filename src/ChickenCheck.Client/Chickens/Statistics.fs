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

let view = elmishView "Statistics" (fun (model: ChickensPageModel) -> 
    let allCounts chickens =
        chickens
        |> Map.values
        |> List.map eggCountView
        |> Level.level []
        
    model.Chickens
    |> Deferred.map(fun chickens ->     
        Container.container []
            [ Text.p 
                [ Modifiers 
                    [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                      Modifier.TextSize (Screen.All, TextSize.Is2)] ] 
                [ str "Hur mycket har de värpt totalt?" ] 
              allCounts chickens ])
    |> Deferred.defaultValue nothing
    )
