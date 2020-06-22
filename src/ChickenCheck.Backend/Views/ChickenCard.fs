module ChickenCheck.Backend.Views.ChickenCard

open ChickenCheck.Backend
open ChickenCheck.Shared
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine
open ChickenCheck.Backend.Views.Shared

type Model =
    { Id: ChickenId
      Name: string
      Breed: string
      ImageUrl: ImageUrl option
      EggCount: EggCount
      CurrentDate: NotFutureDate }
    
let eggIconAttr (id: ChickenId) = prop.custom (DataAttributes.EggIcon, id.Value.ToString())
let chickenCardAttr (id: ChickenId) = prop.custom (DataAttributes.ChickenCard, id.Value.ToString())
    
[<AutoOpen>]
module private Helpers =
    let eggIcons model =
        let eggIcon = 
            Bulma.icon [
                eggIconAttr model.Id
                icon.isLarge
                color.hasTextWhite
                prop.children [
                    Html.i [
                        prop.classes [ "fa-5x fas fa-egg" ]
                    ]
                ]
            ]

        let eggsForCount eggCount =
            [ for _ in 1..eggCount do
                Bulma.column [
                    column.is3
                    prop.id ("egg-icon-" + model.Id.Value.ToString())
                    prop.children eggIcon
                ]
              Bulma.column [
                  column.is3
                  helpers.isHidden
                  prop.id ("egg-icon-loader-" + model.Id.Value.ToString())
                  prop.children Shared.loading
              ]
            ]

        Bulma.columns [
            columns.isCentered
            columns.isVCentered
            columns.isMobile
            prop.style [ style.height (length.px 200) ]
            prop.children (eggsForCount model.EggCount.Value)
        ]
        
    let header (model: Model) =
        Bulma.text.div [
            prop.children [
                Bulma.title.h4 [
                    color.hasTextWhite
                    prop.text model.Name
                ]
                Bulma.subtitle.p [
                    color.hasTextWhite
                    prop.text model.Breed
                ]
            ]
        ]
        
    let cardBackgroundStyle (model: Model) =
        let imageUrlStr =
            model.ImageUrl
            |> Option.map (ImageUrl.value)
            |> Option.defaultValue ""
            
        prop.style [
            style.backgroundImage (sprintf "linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s)" imageUrlStr)
            style.backgroundRepeat.noRepeat
            style.backgroundSize.cover
        ]
    
let layout (model: Model) =

    Bulma.card [
        chickenCardAttr model.Id
        cardBackgroundStyle model
        prop.children [
            Bulma.cardHeader (header model)
            Bulma.cardContent [ 
                eggIcons model
            ]
        ]
    ]
  
  
    
