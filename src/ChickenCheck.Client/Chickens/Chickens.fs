module ChickenCheck.Client.Chickens

open ChickenCheck.Client
open ChickenCheck.Shared
open Elmish
open Elmish.React
open FsToolkit.ErrorHandling
open ChickenCheck.Client.Utils
open ChickenCheck.Client.ChickenCard
open Feliz
open Feliz.Bulma
open Feliz.Bulma.PageLoader


let init date =
    { Chickens = InProgress
      Errors = []
      CurrentDate = date }

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
    | GetAllChickens (Start date) ->
        model, api.GetAllChickens(token, date)
        
    | GetAllChickens (Finished chickens) ->
        let buildModel { Chicken = chicken; Count = (_, onDateCount); TotalCount = totalCount } =
            chicken.Id,
            { Id = chicken.Id
              Name = chicken.Name
              ImageUrl = chicken.ImageUrl
              Breed = chicken.Breed
              TotalEggCount = totalCount
              EggCountOnDate  = onDateCount
              IsLoading = false }
            
        let newChickens = chickens |> List.map buildModel |> Map.ofList
            
        { model with 
            Chickens = Deferred.Resolved newChickens }, Cmd.none
            
    | AddError msg ->
        { model with Errors = msg :: model.Errors }, Cmd.none
        
    | GetEggCount (Start (date, chickens)) ->
        model, api.GetEggCount(token, date, chickens)

    | GetEggCount (Finished (date, countByChicken)) -> 
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
        match model.Chickens with
        | Deferred.InProgress ->
            model, Cmd.none
        | Deferred.Resolved chickens ->
            let chickenIds = chickens |> Map.keys
            { model with CurrentDate = date }, api.GetEggCount(token, date, chickenIds)
        | Deferred.HasNotStartedYet ->
            { model with CurrentDate = date }, api.GetAllChickens(token, date) 
        
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
    
let view = (fun model dispatch ->
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
                let card = lazyView2 ChickenCard.view (chicken, model.CurrentDate) dispatch
                
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
            lazyView2 DatePicker.view model.CurrentDate dispatch
            chickenListView
            lazyView Statistics.view model
        ]
            
    Html.div [
        PageLoader.pageLoader [
            pageLoader.isInfo
            if not (model.Chickens |> Deferred.resolved) then pageLoader.isActive 
        ]
        if hasErrors then Bulma.section errorView
        view'
    ])
