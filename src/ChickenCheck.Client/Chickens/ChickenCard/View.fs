module ChickenCheck.Client.ChickenCard.View

open ChickenCheck.Shared
open ChickenCheck.Client
open ChickenCheck.Client.Chickens
open Feliz
open Feliz.Bulma

let view user (chicken, currentDate) dispatch =
    let runIfLoggedIn user f =
        user
        |> Deferred.map (function
            | Anonymous -> ignore
            | ApiUser _ -> f) 
        |> Deferred.defaultValue ignore
    
    let addEgg  =
        let handleEvent (ev:Browser.Types.MouseEvent) =
            ev.cancelBubble <- true
            ev.stopPropagation()
            Start (chicken.Id, currentDate) |> ChickenMsg.AddEgg |> dispatch
        runIfLoggedIn user handleEvent
    
    let removeEgg  =
        let handleEvent (ev:Browser.Types.MouseEvent) =
            ev.cancelBubble <- true
            ev.stopPropagation()
            Start (chicken.Id, currentDate) |> ChickenMsg.RemoveEgg |> dispatch
        runIfLoggedIn user handleEvent
    
    let eggIcons =
        let eggIcon = 
            Bulma.icon [
                icon.isLarge
                color.hasTextWhite
                prop.onClick removeEgg
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

    let header =
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

    let cardBackgroundStyle =
        let imageUrlStr =
            chicken.ImageUrl
            |> Option.map (ImageUrl.value)
            |> Option.defaultValue ""
            
        prop.style [
            style.backgroundImage (sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrlStr)
            style.backgroundRepeat.noRepeat
            style.backgroundSize.cover
        ]
        
    Bulma.card [
        prop.onClick addEgg
        cardBackgroundStyle
        prop.children [
            Bulma.cardHeader header
            Bulma.cardContent eggIcons
        ]
    ]