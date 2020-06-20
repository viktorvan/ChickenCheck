module ChickenCheck.Client.Chickens.ChickenCard

open ChickenCheck.Shared
open ChickenCheck.Client
open Feliz
open Feliz.Bulma
      
type Model =
    { Id: ChickenId
      Name: string
      Breed: string
      ImageUrl: ImageUrl option
      IsLoading: bool
      EggCount: EggCount
      CurrentDate: NotFutureDate }
    
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
            prop.children (eggsForCount chicken.EggCount.Value)
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
    
    let render (props: {| Model: Model
                          AddEgg: unit -> unit
                          RemoveEgg: unit -> unit |}) =

        Bulma.card [
            prop.onClick (fun ev ->
                ev.preventDefault()
                ev.stopPropagation()
                props.AddEgg())
            cardBackgroundStyle props.Model
            prop.children [
                Bulma.cardHeader (header props.Model)
                Bulma.cardContent (eggIcons props.Model props.RemoveEgg)
            ]
        ]
      
      
        
let chickenCard = Utils.memoWithKey "ChickenCard" render (fun props -> props.Model.Id.Value.ToString())
