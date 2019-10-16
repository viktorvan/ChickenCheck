module ChickenCheck.Client.Statistics 

open ChickenCheck.Domain
open Fulma
open Fable.React

type Model =
    { Chickens : Chicken list
      TotalEggCount : EggCountMap option }

let private chickenEggCount (countMap: EggCountMap option) (chicken: Chicken) =
    match countMap with
    | Some countMap ->
        let getCountStr chickenId (countMap: EggCountMap) =
            countMap
            |> Map.tryFind chickenId
            |> Option.map EggCount.toString
            |> Option.defaultValue "-"

        let totalCount = getCountStr chicken.Id countMap
        Level.item [ Level.Item.HasTextCentered ]
            [ div []
                [ Level.heading [] [ chicken.Name.Value |> str ] 
                  Level.title [] [ str totalCount ] ] ]
        |> Some
    | None -> None

let private allCounts (model: Model) =
    model.Chickens 
    |> List.choose (chickenEggCount model.TotalEggCount)
    |> Level.level []

let view (model: Model) =
    Container.container []
        [ Text.p 
            [ Modifiers 
                [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                  Modifier.TextSize (Screen.All, TextSize.Is2)] ] 
            [ str "Hur mycket har de värpt totalt?" ] 
          allCounts model ]

