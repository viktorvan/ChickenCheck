module ChickenCheck.Client.ChickenCard

open ChickenCheck.Domain
open ChickenCheck.Client
open Utils
open Feliz
open Feliz.Bulma

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
            Bulma.icon [
                icon.isLarge
                color.hasTextWhite
                prop.onClick (fun ev ->
                    ev.cancelBubble <- true
                    ev.stopPropagation()
                    props.RemoveEgg())
                prop.children [
                    Html.i [
                        prop.classes [ "fa-5x fas fa-egg" ]
                    ]
                ]
            ]

        let eggsForCount eggCount =

            if props.IsLoading then
                [ SharedViews.loading ]
            else
                [ for _ in 1..eggCount do
                    Bulma.column [
                        column.is3
                        prop.children eggIcon
                    ]
                ]

        Bulma.columns [
            columns.isCentered
            columns.isVCentered
            columns.isMobile
            prop.style [ style.height (length.px 200) ]
            prop.children (eggsForCount props.EggCountOnDate.Value)
        ]

    let header =
        Bulma.text.div [
            prop.children [
                Bulma.title.h4 [
                    color.hasTextWhite
                    prop.text props.Name.Value
                ]
                Bulma.subtitle.p [
                    color.hasTextWhite
                    prop.text props.Breed.Value
                ]
            ]
        ]

    let cardBackgroundStyle =
        let imageUrlStr =
            props.ImageUrl
            |> Option.map (ImageUrl.value)
            |> Option.defaultValue ""
            
        prop.style [
            style.backgroundImage (sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrlStr)
            style.backgroundRepeat.noRepeat
            style.backgroundSize.cover
        ]
        
    Bulma.card [
        prop.onClick (fun _ -> props.AddEgg())
        cardBackgroundStyle
        prop.children [
            Bulma.cardHeader header
            Bulma.cardContent eggIcons
        ]
    ])