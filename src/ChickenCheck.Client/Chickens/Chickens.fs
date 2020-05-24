module ChickenCheck.Client.Chickens

open ChickenCheck.Client
open ChickenCheck.Domain
open Elmish
open FsToolkit.ErrorHandling
open ChickenCheck.Client.Utils
open ChickenCheck.Client.ChickenCard
open Feliz
open Feliz.Bulma
open Feliz.Bulma.PageLoader


let init token (api: IChickenApiCmds) =
    { Chickens = HasNotStartedYet
      Errors = []
      CurrentDate = Date.today }, api.GetAllChickensWithEggs(token, Date.today)

let toMsg = ChickenMsg >> Cmd.ofMsg

let setIsLoading model state id =
    let c =
        model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.change id (Option.map (fun c -> { c with IsLoading = state }))
            )
    { model with Chickens = c }
    
let setStartLoading id model  = setIsLoading model true id
let setStopLoading id model = setIsLoading model false id
    
let changeEggCount model id f =
    let newChickens =
        model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.change id (Option.map (fun c ->
                { c with 
                    EggCountOnDate = f c.EggCountOnDate
                    TotalEggCount = f c.TotalEggCount  } ))
        )
    { model with Chickens = newChickens }
        
let increaseEggCount id model = changeEggCount model id EggCount.increase
    
let decreaseEggCount id model = changeEggCount model id EggCount.decrease

let update token (api: IChickenApiCmds) (msg: ChickenMsg) (model: ChickensPageModel) : ChickensPageModel * Cmd<Msg>=
    match msg with
    | GetAllChickensWithEggs (Start date) ->
        model, api.GetAllChickensWithEggs(token, date)
        
    | GetAllChickensWithEggs (Finished chickens) ->
        let buildModel { Chicken = chicken; OnDate = onDateCount; Total = totalCount } =
            chicken.Id,
            { Id = chicken.Id
              Name = chicken.Name
              ImageUrl = chicken.ImageUrl
              Breed = chicken.Breed
              TotalEggCount = totalCount
              EggCountOnDate  = onDateCount
              IsLoading = false }
            
        let newChickens =
            chickens |> List.map buildModel |> Map.ofList 
            
        { model with 
            Chickens = Deferred.Resolved newChickens }, Cmd.none
            
    | AddError msg ->
        { model with Errors = msg :: model.Errors }, Cmd.none
        
    | GetEggCountOnDate (Start (date)) ->
        model, api.GetEggCountOnDate(token, date)

    | GetEggCountOnDate (Finished (date, countByChicken)) -> 
        if model.CurrentDate = date then
            if countByChicken |> Map.isEmpty then
                model, 
                "Count by date map was empty" |> AddError |> ChickenMsg |> Cmd.ofMsg
            else
                let updateEggCount id (model:ChickenDetails) =
                    match countByChicken |> Map.tryFind id with
                    | Some newCount ->
                        { model with EggCountOnDate = newCount }
                    | None -> model
                    
                model.Chickens
                |> Deferred.map (fun chickens ->
                    { model with Chickens = chickens |> Map.map updateEggCount |> Resolved }, Cmd.none
                    )
                |> Deferred.defaultValue (model, Cmd.none)
        else
            model, Cmd.none

    | ClearErrors -> 
        { model with Errors = [] }, Cmd.none
        
    | ChangeDate date -> 
        { model with CurrentDate = date }, api.GetEggCountOnDate(token, date)
        
    | ChickenMsg.AddEgg (Start (id, date)) ->
        model
        |> setStartLoading id, api.AddEgg (token, id, date)

    | ChickenMsg.AddEgg (Finished (id, date)) ->
        model
        |> setStopLoading id
        |> increaseEggCount id, Cmd.none

    | ChickenMsg.RemoveEgg (Start (id, date)) -> 
        model
        |> setStartLoading id, api.RemoveEgg (token, id, date)

    | ChickenMsg.RemoveEgg (Finished (id, date)) ->
        model
        |> setStopLoading id
        |> decreaseEggCount id, Cmd.none
                
type ChickensProps =
    { Model: ChickensPageModel; Dispatch: Dispatch<Msg> }
    
let view = elmishView "Chickens" (fun (props:ChickensProps) ->
    let model = props.Model
    let dispatch = props.Dispatch

    let header =
        Html.p [
            text.hasTextCentered
            size.isSize2
            prop.text "Vem värpte idag?"
        ]

    let errorView =
        let errorFor item = 
            item
            |> SharedViews.apiErrorMsg (fun _ -> ClearErrors |> ChickenMsg |> dispatch) 

        model.Errors |> List.map errorFor

    let hasErrors = model.Errors |> List.isEmpty |> not
        
    let chickenListView = 
        let cardViewRows (chickens: ChickenDetails list) = 
            let cardView (chicken: ChickenDetails) =
                let card =
                    ChickenCard.view
                        { Name = chicken.Name
                          Breed = chicken.Breed
                          ImageUrl = chicken.ImageUrl
                          EggCountOnDate = chicken.EggCountOnDate
                          IsLoading = chicken.IsLoading
                          AddEgg = fun () -> Start (chicken.Id, model.CurrentDate) |> ChickenMsg.AddEgg |> ChickenMsg |> props.Dispatch
                          RemoveEgg = fun () -> Start (chicken.Id, model.CurrentDate) |> ChickenMsg.RemoveEgg |> ChickenMsg |> props.Dispatch }
                
                [ 
                    Bulma.column [
                        column.is4
                        prop.classes [ "is-hidden-touch" ]
                        prop.children [ card ] 
                    ]
                    Bulma.column [
                        column.is12
                        prop.classes [ "is-hidden-desktop" ]
                        prop.children [ card ] 
                    ]
                ]
                
            let cardViewRow details = List.collect cardView details
            
            chickens
            |> List.sortBy (fun c -> c.Name)
            |> List.batchesOf 3
            |> List.map cardViewRow

        model.Chickens
        |> Deferred.map (fun chickens ->
            chickens
            |> Map.values
            |> cardViewRows
            |> List.map (Bulma.columns) 
            |> Bulma.container)
        |> Deferred.defaultValue Html.none

    let view' =
        Bulma.section [
            header
            DatePicker.view { CurrentDate = model.CurrentDate; OnChangeDate = (ChangeDate >> ChickenMsg >> dispatch) }
            chickenListView
            Statistics.view model
        ]
            
    Html.div [
        PageLoader.pageLoader [
            pageLoader.isInfo
            if not (model.Chickens |> Deferred.resolved) then pageLoader.isActive 
        ]
        if hasErrors then Bulma.section errorView
        view'
    ])
