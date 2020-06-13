namespace ChickenCheck.Client.ChickenCard

open ChickenCheck.Shared
open ChickenCheck.Client
open ChickenCheck.Client.Chickens
open Feliz
open Feliz.Bulma

type ChickenCardProps =
    { Chicken: ChickenDetails
      CurrentDate: NotFutureDate
      AddEgg: ChickenId * NotFutureDate -> unit
      RemoveEgg: ChickenId * NotFutureDate -> unit }
      
module View =
    let view name = Utils.elmishView name (fun props ->
        let eggIcons =
            let eggIcon = 
                Bulma.icon [
                    icon.isLarge
                    color.hasTextWhite
                    prop.onClick (fun _ -> props.RemoveEgg (props.Chicken.Id, props.CurrentDate))
                    prop.children [
                        Html.i [
                            prop.classes [ "fa-5x fas fa-egg" ]
                        ]
                    ]
                ]

            let eggsForCount eggCount =

                if props.Chicken.IsLoading then
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
                prop.children (eggsForCount props.Chicken.EggCountOnDate.Value)
            ]

        let header =
            Bulma.text.div [
                prop.children [
                    Bulma.title.h4 [
                        color.hasTextWhite
                        prop.text props.Chicken.Name
                    ]
                    Bulma.subtitle.p [
                        color.hasTextWhite
                        prop.text props.Chicken.Breed
                    ]
                ]
            ]

        let cardBackgroundStyle =
            let imageUrlStr =
                props.Chicken.ImageUrl
                |> Option.map (ImageUrl.value)
                |> Option.defaultValue ""
                
            prop.style [
                style.backgroundImage (sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrlStr)
                style.backgroundRepeat.noRepeat
                style.backgroundSize.cover
            ]
            
        Bulma.card [
            prop.onClick (fun _ -> props.AddEgg (props.Chicken.Id, props.CurrentDate))
            cardBackgroundStyle
            prop.children [
                Bulma.cardHeader header
                Bulma.cardContent eggIcons
            ]
        ])