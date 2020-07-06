module ChickenCheck.Backend.Views.ChickenCard

open ChickenCheck.Shared
open Feliz.ViewEngine
open Feliz.Bulma.ViewEngine

type Model =
    { Id: ChickenId
      Name: string
      Breed: string
      ImageUrl: ImageUrl option
      EggCount: EggCount
      CurrentDate: NotFutureDate }
    
let chickenIdAttr (id: ChickenId) = prop.custom (DataAttributes.ChickenId, id.Value.ToString())
    
[<AutoOpen>]
module private Helpers =
    let eggIcons model =
        let eggIcon = 
            Bulma.icon [
                icon.isLarge
                color.hasTextWhite
                prop.children [
                    Html.i [
                        chickenIdAttr model.Id
                        prop.classes [ "fa-5x fas fa-egg egg-icon" ]
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
            prop.style [ style.height (length.percent 100) ]
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
            style.height (length.px 300)
            style.display.flex
            style.flexDirection.column
        ]
    
let layout (model: Model) =

    Bulma.card [
        cardBackgroundStyle model
        prop.children [
            Bulma.cardHeader (header model)
            Bulma.cardContent [ 
                chickenIdAttr model.Id
                prop.classes [ "chicken-card" ]
                prop.style [ style.flexGrow 1 ]
                prop.children [
                    if model.EggCount.Value > 0 then eggIcons model
                ]
            ]
        ]
    ]
  
  
    
