module ChickenCheck.Client.ChickenCard

open Fable.React.Props
open Fable.React
open ChickenCheck.Domain
open Fulma
open Fable.FontAwesome
open ChickenCheck.Client
open Utils

type ChickenCardProps =
    { Name: String200
      Breed: String200
      ImageUrl: ImageUrl option
      EggCountOnDate : EggCount
      IsLoading : bool
      AddEgg : unit -> unit
      RemoveEgg : unit -> unit }
      
let view = elmishView "ChickenCard" (fun (props:ChickenCardProps) ->
    
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
                                props.RemoveEgg()) ]
                ] 
                [ 
                    Fa.i [ Fa.Size Fa.Fa5x; Fa.Solid.Egg ] [] 
                ]

        let addedEggs =

            if props.IsLoading then
                [ ViewComponents.loading ]
            else
                [ for _ in 1..props.EggCountOnDate.Value do
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
                    [ str props.Name.Value ]
                Heading.h6 
                    [ Heading.IsSubtitle;  Heading.Modifiers [ Modifier.TextColor Color.IsWhite ] ]
                    [ str props.Breed.Value ] 
            ]

    let cardBackgroundStyle =
        let imageUrlStr =
            props.ImageUrl
            |> Option.map (ImageUrl.value)
            |> Option.defaultValue ""

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
                            OnClick (fun _ -> props.AddEgg())
                            cardBackgroundStyle
                        ] 
                ]
                [ 
                    Card.header [] [ header ] 
                    Card.content [] [ eggIcons ] 
                ]
        ])
