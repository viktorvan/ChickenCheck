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
open ChickenCheck.Domain.Commands
open ChickenCheck.Client.ChickenCard

type Api =
    { GetChickens: unit -> Cmd<Msg> 
      GetTotalCount: unit -> Cmd<Msg>
      GetCountOnDate: Date -> Cmd<Msg> 
      AddEgg : Commands.AddEgg -> Cmd<Msg>
      RemoveEgg : Commands.RemoveEgg -> Cmd<Msg> }

let private ofMsg (msg: ChickenMsg) = msg |> ChickenMsg |> Cmd.ofMsg

let init =
    { Chickens = Map.empty
      AddEggStatus = Map.empty
      RemoveEggStatus = Map.empty
      Errors = []
      CurrentDate = Date.today }, FetchChickens |> ofMsg

let update (api: Api) (msg: ChickenMsg) (model: ChickensModel) =
    match msg with
    | FetchChickens -> 
        model,
        api.GetChickens()

    | FetchedChickens chickens ->
        let buildModel (c: Chicken) =
            { Id = c.Id
              Name = c.Name
              ImageUrl = c.ImageUrl
              Breed = c.Breed
              TotalEggCount = EggCount.zero
              EggCountOnDate  = EggCount.zero }
            
        let cmds =
            [ FetchTotalCount 
              FetchEggCountOnDate Date.today ]
            |> List.map ofMsg
            |> Cmd.batch
              
        { model with 
            Chickens = chickens |> List.map (fun c -> c.Id, buildModel c) |> Map.ofList },
            cmds

    | AddError msg ->
        { model with Errors = msg :: model.Errors }, Cmd.none

    | FetchEggCountOnDate date -> 
        let callApi = api.GetCountOnDate date
        model, callApi
        
    | FetchedEggCountOnDate (date, countByChicken) -> 
        if model.CurrentDate = date then
            if countByChicken |> Map.isEmpty then
                model, "Count by date map was empty" |> AddError |> ofMsg
            else
                let updateEggCount id (model:ChickenDetails) =
                    match countByChicken |> Map.tryFind id with
                    | Some newCount ->
                        { model with EggCountOnDate = newCount }
                    | None -> model
                    
                { model with Chickens = model.Chickens |> Map.map updateEggCount }, Cmd.none
        else
            model, Cmd.none

    | FetchTotalCount -> 
        model, api.GetTotalCount()

    | FetchedTotalCount totalCount -> 
        if totalCount |> Map.isEmpty then
            model, "TotalCount map was empty" |> AddError  |> ofMsg
        else
            let updateEggCount id (model:ChickenDetails) =
                match totalCount |> Map.tryFind id with
                | Some newCount ->
                    { model with TotalEggCount = newCount }
                | None -> model
                
            { model with Chickens = model.Chickens |> Map.map updateEggCount }, Cmd.none

    | ClearErrors -> 
        { model with Errors = [] }, Cmd.none
    | ChangeDate date -> 
        { model with CurrentDate = date }, FetchEggCountOnDate date |> ofMsg
        
    | AddEgg ((id: ChickenId), date) ->
        let isRunning = model.AddEggStatus |> Map.add id Running
        { model with AddEggStatus = isRunning }, 
        api.AddEgg { AddEgg.ChickenId = id; Date = date }

    | AddedEgg (id, date) ->
        let isCompleted = model.AddEggStatus |> Map.add id Completed
        let model = { model with AddEggStatus = isCompleted }
        
        match Map.tryFind id model.Chickens with
        | None -> model, Cmd.none
        | Some chicken ->
            let newEggCount = chicken.EggCountOnDate.Increase()
            let newTotal = chicken.TotalEggCount.Increase()

            match newEggCount, newTotal with
            | Ok newEggCount, Ok newTotal ->
                let updatedChicken =
                    { chicken with 
                        EggCountOnDate = newEggCount
                        TotalEggCount = newTotal }
                { model with Chickens = model.Chickens |> Map.add chicken.Id updatedChicken }, Cmd.none
                
            | Error (ValidationError (param, msg)), _ | _, Error (ValidationError (param, msg)) ->
                let cmd =
                    let errorMsg = sprintf "could not add egg: %s:%s" param msg
                    AddEggFailed (id, errorMsg) |> ofMsg
                
                model, cmd
                
    | AddEggFailed (id, msg) ->
        let isCompleted = model.AddEggStatus |> Map.add id Completed
        { model with AddEggStatus = isCompleted }, 
        AddError msg |> ofMsg
        
    | RemoveEgg ((id: ChickenId), date) -> 
        { model with RemoveEggStatus = model.RemoveEggStatus |> Map.add id Running }, 
        api.RemoveEgg { RemoveEgg.ChickenId = id; Date = date }

    | RemovedEgg (id, date) ->
        let isCompleted = model.RemoveEggStatus |> Map.add id Completed 
        let model = { model with RemoveEggStatus = isCompleted }
        match Map.tryFind id model.Chickens with
        | None -> model, Cmd.none
        | Some chicken ->
            let newEggCount = chicken.EggCountOnDate.Decrease()
            let newTotal = chicken.TotalEggCount.Decrease()

            match (newEggCount, newTotal) with
            | Ok newEggCount, Ok newTotal ->
                let updatedChicken =
                    { chicken with
                        EggCountOnDate = newEggCount
                        TotalEggCount = newTotal }
                { model with Chickens = model.Chickens |> Map.add id updatedChicken }, Cmd.none
            | Error (ValidationError (param, msg)), _ | _, Error (ValidationError (param, msg)) ->
                let cmd =
                    let errorMsg = sprintf "could not add egg: %s:%s" param msg
                    RemoveEggFailed (id, errorMsg) |> ofMsg
                
                model, cmd
                
    | RemoveEggFailed (id, msg) ->
        { model with RemoveEggStatus = model.RemoveEggStatus |> Map.add id ApiCallStatus.Completed }, 
        AddError msg |> ofMsg

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
        let batched =
            props.Model.Chickens 
            |> Map.toList
            |> List.map snd
            |> List.sortBy (fun c -> c.Name)
            |> List.map (fun c -> c.Id)
            |> List.batchesOf 3 

        let batchedCardViews =
            batched
            |> List.map
                (fun batch ->
                    batch
                    |> List.map
                        (fun chickenId ->
                            let props =
                                { Model = props.Model
                                  Id = chickenId
                                  AddEgg = (AddEgg >> ChickenMsg >> props.Dispatch)
                                  RemoveEgg = (RemoveEgg >> ChickenMsg >> props.Dispatch) }
                            ChickenCard.view props))

        batchedCardViews
        |> List.map (Columns.columns []) 
        |> Container.container []
    
    let chickenEggView =
        let chickenListProps = { Model = model; Dispatch = dispatch }
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