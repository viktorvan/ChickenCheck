module ChickenCheck.Client.Chickens

open ChickenCheck.Client
open ChickenCheck.Shared
open FsToolkit.ErrorHandling
open Feliz
open Feliz.Bulma
open Feliz.Bulma.PageLoader
open ChickenCheck.Client.Chickens
open Elmish
open Contexts
open Feliz.UseElmish

let init date = fun () -> Model.init date, Start date |> GetAllChickens |> Cmd.ofMsg

let render initDate api () =
    let user = React.useContext (userContext)
    let model, dispatch = React.useElmish(init initDate, Update.update api, [| |])
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
    
    let runIfLoggedIn user f =
        match user with
        | (ApiUser _) -> f
        | _ -> ignore
        
    let chickenListView = 
        let cardViewRows (chickens: ChickenDetails list) = 
            let cardView (chicken: ChickenDetails) =
                let addEgg = runIfLoggedIn user (fun () -> Start (chicken.Id, model.CurrentDate) |> AddEgg |> dispatch)
                let removeEgg = runIfLoggedIn user (fun () -> Start (chicken.Id, model.CurrentDate) |> RemoveEgg |> dispatch)
 
                let cardProps =
                    {| Chicken = chicken
                       CurrentDate = model.CurrentDate
                       AddEgg = addEgg
                       RemoveEgg = removeEgg |}
                let card = ChickenCard.chickenCard cardProps
                
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

    let statistics =
        model.Chickens
        |> Deferred.map (fun chickens ->
            let props = {| Chickens = chickens |> Map.values |}
            Statistics.statistics props)
        |> Deferred.defaultValue Html.none 
        
    let view =
        Bulma.section [
            header
            DatePicker.datePicker {| CurrentDate = model.CurrentDate; ChangeDate = ChangeDate >> dispatch |}
            chickenListView
            statistics
        ]
            
    Html.div [
        PageLoader.pageLoader [
            pageLoader.isInfo
            if not (model.Chickens |> Deferred.resolved) then pageLoader.isActive 
        ]
        if hasErrors then Bulma.section errorView
        view
    ]
let chickens initDate api = React.memo("Chickens", render initDate api)
