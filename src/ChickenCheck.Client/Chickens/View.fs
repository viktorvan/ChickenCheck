module ChickenCheck.Client.Chickens.View

open ChickenCheck.Client
open ChickenCheck.Shared
open Elmish.React
open FsToolkit.ErrorHandling
open Feliz
open Feliz.Bulma
open Feliz.Bulma.PageLoader
open ChickenCheck.Client.Chickens
open Elmish


let view user model (dispatch: Dispatch<ChickenMsg>) =
    let header =
        Html.p [
            text.hasTextCentered
            size.isSize2
            prop.text "Vem värpte idag?"
        ]

    let errorView =
        let errorFor item = 
            item
            |> SharedViews.apiErrorMsg (fun _ -> ClearErrors |> dispatch) 

        model.Errors |> List.map errorFor

    let hasErrors = model.Errors |> List.isEmpty |> not
        
    let chickenListView = 
        let cardViewRows (chickens: ChickenDetails list) = 
            let cardView (chicken: ChickenDetails) =
                let card = lazyView3 ChickenCard.View.view user (chicken, model.CurrentDate) dispatch
                
                Bulma.column [
                    column.is4
                    prop.children [ card ] 
                ]
                
            let cardViewRow rowChickens = List.map cardView rowChickens
            
            chickens
            |> List.sortBy (fun c -> c.Name)
            |> List.batchesOf 3
            |> List.map cardViewRow

        model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.values
            |> cardViewRows
            |> List.map Bulma.columns 
            |> Bulma.container)
        |> Deferred.defaultValue Html.none

    let view' =
        Bulma.section [
            header
            lazyView2 DatePicker.View.view model.CurrentDate dispatch
            chickenListView
            lazyView Statistics.View.view model
        ]
            
    Html.div [
        PageLoader.pageLoader [
            pageLoader.isInfo
            if not (model.Chickens |> Deferred.resolved) then pageLoader.isActive 
        ]
        if hasErrors then Bulma.section errorView
        view'
    ]
