module ChickenCheck.Client.Chickens.Update

open ChickenCheck.Client
open ChickenCheck.Shared
open Elmish
open FsToolkit.ErrorHandling
open ChickenCheck.Client.Chickens


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

let update (api: IChickenApiCmds) (msg: ChickenMsg) (model: ChickensPageModel) : ChickensPageModel * Cmd<ChickenMsg>=
    match msg with
    | GetAllChickens (Start date) ->
        model, api.GetAllChickens(date)
        
    | GetAllChickens (Finished (Ok chickens)) ->
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
            
    | GetAllChickens (Finished (Error err)) -> notImplemented()
            
    | AddError msg ->
        { model with Errors = msg :: model.Errors }, Cmd.none
        
    | GetEggCount (Start (date, chickens)) ->
        model, api.GetEggCount(date, chickens)

    | GetEggCount (Finished (Ok (date, countByChicken))) -> 
        if model.CurrentDate = date then
            if countByChicken |> Map.isEmpty then
                model, 
                "Count by date map was empty" |> AddError |> Cmd.ofMsg
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
            
    | GetEggCount (Finished (Error err)) -> notImplemented()

    | ClearErrors -> 
        { model with Errors = [] }, Cmd.none
        
    | ChangeDate date ->
        match model.Chickens with
        | Deferred.InProgress ->
            model, Cmd.none
        | Deferred.Resolved chickens ->
            let chickenIds = chickens |> Map.keys
            { model with CurrentDate = date }, api.GetEggCount(date, chickenIds)
        | Deferred.HasNotStartedYet ->
            { model with CurrentDate = date }, api.GetAllChickens(date) 
        
    | ChickenMsg.AddEgg (Start (id, date)) ->
        model
        |> setStartLoading id, api.AddEgg (id, date)

    | ChickenMsg.AddEgg (Finished (Ok (id, date))) ->
        model
        |> setStopLoading id
        |> increaseEggCount id, Cmd.none
        
    | ChickenMsg.AddEgg (Finished (Error (id, err))) ->
        model
        |> setStopLoading id, Cmd.none

    | ChickenMsg.RemoveEgg (Start (id, date)) -> 
        model
        |> setStartLoading id, api.RemoveEgg (id, date)

    | ChickenMsg.RemoveEgg (Finished (Ok (id, date))) ->
        model
        |> setStopLoading id
        |> decreaseEggCount id, Cmd.none
        
    | ChickenMsg.RemoveEgg (Finished (Error (id, date))) -> 
        model
        |> setStopLoading id, Cmd.none
    
    | NoOp -> model, Cmd.none
