namespace ChickenCheck.Backend.Views

open ChickenCheck.Backend
open Giraffe.ViewEngine

type ChickenCardModel =
    { Id: ChickenId
      Name: string
      Breed: string
      ImageUrl: ImageUrl option
      EggCount: EggCount
      CurrentDate: NotFutureDate }
    
module ChickenCard =
    let chickenIdAttr (id: ChickenId) = attr DataAttributes.ChickenId (id.Value.ToString())
        
    [<AutoOpen>]
    module private Helpers =
        let eggIcons model =
            let eggIcon = 
                span 
                    [ _class "icon is-large has-text-white" ]
                    [ i [ _class "fa-5x fas fa-egg" ] [ ] ]

            let eggsForCount eggCount =
                [ for _ in 1..eggCount do
                  div [ _class "column is-3 egg-icon"; chickenIdAttr model.Id ] [ eggIcon ]
                  div [ _class "column is-3 egg-icon is-hidden egg-icon-loader"; chickenIdAttr model.Id ] [ Shared.loading ]
                  div [ _class "column is-3 egg-icon is-hidden egg-error"; chickenIdAttr model.Id ] [ Shared.error ]
                ]

            div [ _class "columns is-centered is-vcentered is-mobile"; _style "height:100%;" ] 
                [ yield! eggsForCount model.EggCount.Value ]
            
        let header (model: ChickenCardModel) =
            header [ _class "header" ] 
                [
                    h4 [ _class "title has-text-white" ] [ str model.Name ]
                    p [ _class "subtitle has-text-white" ] [ str model.Breed ]
                ]
            
        let cardBackgroundStyle (model: ChickenCardModel) =
            let imageUrlStr =
                model.ImageUrl
                |> Option.map (ImageUrl.value)
                |> Option.defaultValue ""
                
            _style $"""background-image: linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0)), url(%s{imageUrlStr});
                       background-repeat: no-repeat;
                       background-size: cover;
                       height: 300px;
                       display: flex;
                       flex-direction: column;"""
                       
    
        
    let layout (model: ChickenCardModel) =
        div 
            [ _class "card chicken-card"
              chickenIdAttr model.Id
              cardBackgroundStyle model
              Htmx.hxPost $"/chickens/%s{model.Id.Value.ToString()}/eggs/%s{model.CurrentDate.ToUrlString()}"
              Htmx.hxTarget "this"
              Htmx.hxSwap "outerHTML"
              Htmx.hxTrigger "click" ] 
            [
                header model
                div [ _class "card-content"; _style "flex-grow: 1;"] 
                    [ eggIcons model ]
            ]
