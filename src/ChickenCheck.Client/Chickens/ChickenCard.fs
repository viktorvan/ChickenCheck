module ChickenCheck.Client.ChickenCard

open Fable.React.Props
open Fable.React
open ChickenCheck.Domain
open Fulma
open Fable.FontAwesome
open ChickenCheck.Client
open Utils

type ChickenCardProps =
    { Model : ChickensModel
      Id : ChickenId
      AddEgg : ChickenId * Date -> unit
      RemoveEgg : ChickenId * Date -> unit }
      
let view = elmishView "ChickenCard" (fun (props:ChickenCardProps) ->
    let model = props.Model
    
    match model.Chickens |> Map.tryFind props.Id with
    | None ->
        div [] []
    | Some chicken ->
        let eggIcons =
            let eggIcon = 
                Icon.icon 
                    [ 
                        Icon.Size IsLarge
                        Icon.Modifiers [ Modifier.TextColor Color.IsWhite ] 
                        Icon.Props 
                            [ OnClick 
                                (fun ev ->
                                    ev.cancelBubble <- true
                                    ev.stopPropagation()
                                    props.RemoveEgg (props.Id, props.Model.CurrentDate)) ]
                    ] 
                    [ 
                        Fa.i [ Fa.Size Fa.Fa5x; Fa.Solid.Egg ] [] 
                    ]

            let addedEggs =
                let isLoading = 
                    match model.AddEggStatus |> Map.tryFind props.Id, model.RemoveEggStatus |> Map.tryFind props.Id with
                    | Some Running, _ | _ , Some Running -> true
                    | _ -> false

                if isLoading then
                    [ ViewComponents.loading ]
                else
                    [ for _ in 1..chicken.EggCountOnDate.Value do
                        yield 
                            Column.column 
                                [ 
                                    Column.Width (Screen.All, Column.Is3) 
                                ] 
                                [ 
                                    eggIcon 
                                ] 
                    ]

            Columns.columns 
                [ 
                    Columns.IsCentered
                    Columns.IsVCentered
                    Columns.IsMobile
                    Columns.Props [ Style [ Height 200 ] ]
                ] 
                addedEggs

        let header =
            div 
                [ ] 
                [ 
                    Heading.h4 
                        [ Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ] 
                        [ str chicken.Name.Value ]
                    Heading.h6 
                        [ Heading.IsSubtitle;  Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ]
                        [ str chicken.Breed.Value ] 
                ]

        let cardBackgroundStyle =

            let imageUrlStr = 
                match chicken.ImageUrl with
                | Some (imageUrl: ImageUrl) -> imageUrl.Value
                | None -> ""

            Style 
                [ 
                    sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrlStr 
                    |> box 
                    |> BackgroundImage 
                    BackgroundRepeat "no-repeat"
                    BackgroundSize "cover" 
                ] 

        Column.column
            [ 
                Column.Width (Screen.Desktop, Column.Is4)
                Column.Width (Screen.Mobile, Column.Is12)
            ]
            [
                Card.card 
                    [ 
                        Props 
                            [ 
                                OnClick (fun _ -> props.AddEgg (props.Id, model.CurrentDate))
                                cardBackgroundStyle
                            ] 
                    ]
                    [ 
                        Card.header [] [ header ] 
                        Card.content [] [ eggIcons ] 
                    ]
            ]
        )
