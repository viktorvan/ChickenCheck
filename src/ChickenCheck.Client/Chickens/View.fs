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
open ChickenCheck.Client.ChickenCard


let view = Utils.elmishView "Chickens" (fun (props: {| Model: ChickensPageModel; Dispatch: Dispatch<ChickenMsg>; User: User |}) ->
    let header =
        Html.p [
            text.hasTextCentered
            size.isSize2
            prop.text "Vem värpte idag?"
        ]

    let errorView =
        let errorFor item = 
            item
            |> SharedViews.apiErrorMsg (fun _ -> ClearErrors |> props.Dispatch) 

        props.Model.Errors |> List.map errorFor

    let hasErrors = props.Model.Errors |> List.isEmpty |> not
    
    let runIfLoggedIn user f =
        match user with
        | (ApiUser _) -> f
        | _ -> ignore
    let addEgg = fun (id, date) -> Start (id,date) |> AddEgg |> props.Dispatch
    let removeEgg = fun (id, date) -> Start (id,date) |> RemoveEgg |> props.Dispatch 
        
    let chickenListView = 
        let cardViewRows (chickens: ChickenDetails list) = 
            let cardView (chicken: ChickenDetails) =
                let cardProps =
                    { Chicken = chicken
                      CurrentDate = props.Model.CurrentDate
                      AddEgg = runIfLoggedIn props.User addEgg
                      RemoveEgg = runIfLoggedIn props.User removeEgg }
                let card = ChickenCard.View.view (sprintf "ChickenCard-%O" chicken.Id.Value) cardProps
                
                Bulma.column [
                    column.is4
                    prop.children [ card ] 
                ]
                
            let cardViewRow rowChickens = List.map cardView rowChickens
            
            chickens
            |> List.sortBy (fun c -> c.Name)
            |> List.batchesOf 3
            |> List.map cardViewRow

        props.Model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.values
            |> cardViewRows
            |> List.map Bulma.columns 
            |> Bulma.container)
        |> Deferred.defaultValue Html.none

    let statistics =
        props.Model.Chickens
        |> Deferred.map (fun chickens ->
            let props = {| Chickens = chickens |> Map.values |}
            Statistics.View.view props)
        |> Deferred.defaultValue Html.none 
        
    let view' =
        Bulma.section [
            header
            DatePicker.View.view {| CurrentDate = props.Model.CurrentDate; Dispatch = props.Dispatch |}
            chickenListView
            statistics
        ]
            
    Html.div [
        PageLoader.pageLoader [
            pageLoader.isInfo
            if not (props.Model.Chickens |> Deferred.resolved) then pageLoader.isActive 
        ]
        if hasErrors then Bulma.section errorView
        view'
    ])
