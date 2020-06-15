module ChickenCheck.Client.ChickenCard

open ChickenCheck.Shared
open ChickenCheck.Client
open ChickenCheck.Client.Chickens
open Feliz
open Feliz.Bulma
      
    
[<AutoOpen>]
module private Helpers =
    let eggIcons chicken removeEgg =
        let eggIcon = 
            Bulma.icon [
                icon.isLarge
                color.hasTextWhite
                prop.onClick (fun ev -> 
                    ev.preventDefault()
                    ev.stopPropagation()
                    removeEgg())
                prop.children [
                    Html.i [
                        prop.classes [ "fa-5x fas fa-egg" ]
                    ]
                ]
            ]

        let eggsForCount eggCount =

            if chicken.IsLoading then
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
            prop.children (eggsForCount chicken.EggCountOnDate.Value)
        ]
        
    let header chicken =
        Bulma.text.div [
            prop.children [
                Bulma.title.h4 [
                    color.hasTextWhite
                    prop.text chicken.Name
                ]
                Bulma.subtitle.p [
                    color.hasTextWhite
                    prop.text chicken.Breed
                ]
            ]
        ]
        
    let cardBackgroundStyle chicken =
        let imageUrlStr =
            chicken.ImageUrl
            |> Option.map (ImageUrl.value)
            |> Option.defaultValue ""
            
        prop.style [
            style.backgroundImage (sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrlStr)
            style.backgroundRepeat.noRepeat
            style.backgroundSize.cover
        ]
    
    let render (props: {| Chicken: ChickenDetails
                          CurrentDate: NotFutureDate
                          AddEgg: unit -> unit
                          RemoveEgg: unit -> unit |}) =

        Bulma.card [
            prop.onClick (fun ev ->
                ev.preventDefault()
                ev.stopPropagation()
                props.AddEgg())
            cardBackgroundStyle props.Chicken
            prop.children [
                Bulma.cardHeader (header props.Chicken)
                Bulma.cardContent (eggIcons props.Chicken props.RemoveEgg)
            ]
        ]
      
        
let chickenCard = React.memo("ChickenCard", render, withKey = fun props -> props.Chicken.Id.Value.ToString())
