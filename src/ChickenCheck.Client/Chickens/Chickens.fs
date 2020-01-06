module ChickenCheck.Client.Chickens

open Fable.React
open ChickenCheck.Domain
open Elmish
open System
open Fulma
open FsToolkit.ErrorHandling
open ChickenCheck.Client
open Fulma.Extensions.Wikiki
open ChickenCheck.Client.Utils
open ChickenCheck.Client.ChickenCard
open ChickenCheck.Backend


let init =
    { Chickens = Map.empty
      AddEggStatus = Map.empty
      RemoveEggStatus = Map.empty
      Errors = []
      CurrentDate = Date.today }, [ CmdMsg.GetAllChickensWithEggs Date.today ]

let toMsg = ChickenMsg >> CmdMsg.OfMsg

let update (msg: ChickenMsg) (model: ChickensModel) : ChickensModel * CmdMsg list =
    match msg with

    | FetchedChickensWithEggs chickens ->
        let buildModel { Chicken = chicken; OnDate = onDateCount; Total = totalCount } =
            chicken.Id,
            { Id = chicken.Id
              Name = chicken.Name
              ImageUrl = chicken.ImageUrl
              Breed = chicken.Breed
              TotalEggCount = totalCount
              EggCountOnDate  = onDateCount }
            
        let newChickens =
            chickens |> List.map buildModel |> Map.ofList 
            
        { model with 
            Chickens = newChickens },
            [ CmdMsg.NoCmdMsg ]
            
    | AddError msg ->
        { model with Errors = msg :: model.Errors }, 
        [ CmdMsg.NoCmdMsg ]

    | FetchedEggCountOnDate (date, countByChicken) -> 
        if model.CurrentDate = date then
            if countByChicken |> Map.isEmpty then
                model, 
                [ "Count by date map was empty" |> AddError |> toMsg ]
            else
                let updateEggCount id (model:ChickenDetails) =
                    match countByChicken |> Map.tryFind id with
                    | Some newCount ->
                        { model with EggCountOnDate = newCount }
                    | None -> model
                    
                { model with Chickens = model.Chickens |> Map.map updateEggCount }, 
                [ CmdMsg.NoCmdMsg ]
        else
            model, 
            [ CmdMsg.NoCmdMsg ]

    | ClearErrors -> 
        { model with Errors = [] }, 
        [ CmdMsg.NoCmdMsg ]
    | ChangeDate date -> 
        { model with CurrentDate = date }, 
        [ CmdMsg.GetEggCountOnDate date ]
        
    | ChickenMsg.AddEgg ((id: ChickenId), date) ->
        let isRunning = model.AddEggStatus |> Map.add id Running
        { model with AddEggStatus = isRunning }, [ CmdMsg.AddEgg (id, date) ]

    | AddedEgg (id, date) ->
        let isCompleted = model.AddEggStatus |> Map.add id Completed
        let model = { model with AddEggStatus = isCompleted }
        
        match Map.tryFind id model.Chickens with
        | None -> 
            model, 
            [ CmdMsg.NoCmdMsg ]
    
        | Some chicken ->
            let newEggCount = chicken.EggCountOnDate.Increase()
            let newTotal = chicken.TotalEggCount.Increase()

            match newEggCount, newTotal with
            | Ok newEggCount, Ok newTotal ->
                let updatedChicken =
                    { chicken with 
                        EggCountOnDate = newEggCount
                        TotalEggCount = newTotal }
                { model with Chickens = model.Chickens |> Map.add chicken.Id updatedChicken }, 
                [ CmdMsg.NoCmdMsg ]
                
                
            | Error (ValidationError (param, msg)), _ | _, Error (ValidationError (param, msg)) ->
                let newMsg =
                    let errorMsg = sprintf "could not add egg: %s:%s" param msg
                    AddEggFailed (id, errorMsg) 
                
                model, [ newMsg |> toMsg ]

                
    | AddEggFailed (id, msg) ->
        let isCompleted = model.AddEggStatus |> Map.add id Completed
        { model with AddEggStatus = isCompleted }, 
        [ AddError msg |> toMsg ]
        
    | ChickenMsg.RemoveEgg ((id: ChickenId), date) -> 
        let isRunning = model.RemoveEggStatus |> Map.add id Running
        { model with RemoveEggStatus = isRunning }, [ CmdMsg.RemoveEgg (id, date) ]

    | RemovedEgg (id, date) ->
        let isCompleted = model.RemoveEggStatus |> Map.add id Completed 
        let model = { model with RemoveEggStatus = isCompleted }
        match Map.tryFind id model.Chickens with
        | None -> 
            model, 
            [ CmdMsg.NoCmdMsg ]
        | Some chicken ->
            let newEggCount = chicken.EggCountOnDate.Decrease()
            let newTotal = chicken.TotalEggCount.Decrease()

            match (newEggCount, newTotal) with
            | Ok newEggCount, Ok newTotal ->
                let updatedChicken =
                    { chicken with
                        EggCountOnDate = newEggCount
                        TotalEggCount = newTotal }
                { model with Chickens = model.Chickens |> Map.add id updatedChicken }, 
                [ CmdMsg.NoCmdMsg ]

            | Error (ValidationError (param, msg)), _ | _, Error (ValidationError (param, msg)) ->
                let newMsg =
                    let errorMsg = sprintf "could not add egg: %s:%s" param msg
                    RemoveEggFailed (id, errorMsg)
                
                model, 
                [ newMsg |> toMsg ]
                
    | RemoveEggFailed (id, msg) ->
        { model with RemoveEggStatus = model.RemoveEggStatus |> Map.add id ApiCallStatus.Completed }, 
        [ AddError msg |> toMsg ]

type ChickensProps =
    { Model: ChickensModel; Dispatch: Dispatch<Msg> }
    
let view = elmishView "Chickens" (fun (props:ChickensProps) ->
    let model = props.Model
    let dispatch = props.Dispatch

    let header =
        Text.p 
            [ 
                Modifiers 
                    [ 
                        Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                        Modifier.TextSize (Screen.All, TextSize.Is2)
                    ] 
            ] 
            [ str "Vem värpte idag?" ]

    let errorView =
        let errorFor item = 
            item
            |> ViewComponents.apiErrorMsg (fun _ -> ClearErrors |> ChickenMsg |> dispatch) 

        model.Errors |> List.map errorFor

    let hasErrors = model.Errors |> List.isEmpty |> not

    let listView = 
        let cardViewRows = 
            let idsInBatchOf n =
                let idsSortedByName =
                    props.Model.Chickens 
                    |> Map.values
                    |> List.sortBy (fun c -> c.Name)
                    |> List.map (fun c -> c.Id)
                    
                idsSortedByName 
                |> List.batchesOf n
                
            let cardView chickenId =
                { Model = props.Model
                  Id = chickenId
                  AddEgg = (ChickenMsg.AddEgg >> ChickenMsg >> props.Dispatch)
                  RemoveEgg = (ChickenMsg.RemoveEgg >> ChickenMsg >> props.Dispatch) }
                |> ChickenCard.view
                
            let cardViewRow ids = List.map cardView ids
            
            idsInBatchOf 3
            |> List.map cardViewRow

        cardViewRows
        |> List.map (Columns.columns []) 
        |> Container.container []
    
    let chickenEggView =
        Section.section 
            [ ]
            [ 
                header
                DatePicker.view { CurrentDate = model.CurrentDate; OnChangeDate = (ChangeDate >> ChickenMsg >> dispatch) }
                listView
            ]

    div []
        [
            yield PageLoader.pageLoader 
                [ 
                    PageLoader.Color IsInfo
                    PageLoader.IsActive (model.Chickens.IsEmpty)  
                ] 
                [ ] 
            if hasErrors then yield Section.section [ ] ( errorView )
            if not model.Chickens.IsEmpty then 
                yield Section.section [] 
                    [ 
                        chickenEggView 
                        Statistics.view model
                    ]
        ])