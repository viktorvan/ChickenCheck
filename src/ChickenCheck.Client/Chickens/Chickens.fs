module ChickenCheck.Client.Chickens

open Fable.React
open ChickenCheck.Domain
open Elmish
open System
open Fulma
open FsToolkit.ErrorHandling
open ChickenCheck.Client
open Fulma.Extensions.Wikiki

type Model =
    { Chickens : Chicken list
      TotalEggCount : EggCountMap option
      EggCountOnDate : EggCountMap option
      CurrentDate : Date 
      ChickenListModel : ChickenCardList.Model option
      Errors : string list }

type Msg = 
    | FetchChickens 
    | FetchedChickens of Chicken list
    | FetchTotalCount 
    | FetchedTotalCount of EggCountMap
    | FetchEggCountOnDate of Date
    | FetchedEggCountOnDate of Date * EggCountMap
    | ChangeDate of Date
    | ChickenListMsg of ChickenCardList.Msg
    | UpdateListModel
    | AddError  of string
    | ClearErrors

type Api =
    { GetChickens: unit -> Cmd<Msg> 
      GetTotalCount: unit -> Cmd<Msg>
      GetCountOnDate: Date -> Cmd<Msg> }

let init =
    let date = Date.today
    let cmds =
        [ FetchChickens |> Cmd.ofMsg
          FetchTotalCount |> Cmd.ofMsg 
          FetchEggCountOnDate Date.today |> Cmd.ofMsg ]
        |> Cmd.batch

    { Chickens = []
      ChickenListModel = None
      Errors = []
      TotalEggCount = None
      EggCountOnDate = None
      CurrentDate = date }, cmds


let update (api: Api) chickenCardApi msg (model: Model) =
    match msg with
    | FetchChickens -> 
        model,
        api.GetChickens()

    | FetchedChickens chickens -> 
        { model with 
            Chickens = chickens },
            UpdateListModel |> Cmd.ofMsg

    | AddError  msg ->
        { model with Errors = msg :: model.Errors }, Cmd.none

    | FetchEggCountOnDate date -> 
        let callApi =
            api.GetCountOnDate date

        let cmds =
            [ UpdateListModel |> Cmd.ofMsg
              callApi ]
            |> Cmd.batch

        { model with 
            EggCountOnDate = None }, cmds

    | FetchedEggCountOnDate (date, countByChicken) -> 
        if model.CurrentDate = date then
            if countByChicken |> Map.isEmpty then
                model, "Count by date map was empty" |> AddError  |> Cmd.ofMsg
            else
                { model with EggCountOnDate = Some countByChicken }, 
                    UpdateListModel |> Cmd.ofMsg
        else
            model, Cmd.none

    | FetchTotalCount -> 
        { model with 
            TotalEggCount = None }, 
        api.GetTotalCount()

    | FetchedTotalCount totalCount -> 
        if totalCount |> Map.isEmpty then
            model, "TotalCount map was empty" |> AddError  |> Cmd.ofMsg
        else
            { model with TotalEggCount = Some totalCount },
            UpdateListModel |> Cmd.ofMsg

    | UpdateListModel ->
        match (model.Chickens, model.EggCountOnDate) with
        | [], _ -> 
            { model with ChickenListModel = None }, Cmd.none

        | chickens, eggCount ->
            let listModel = ChickenCardList.init model.CurrentDate eggCount chickens
            { model with ChickenListModel = Some listModel }, Cmd.none

    | ChickenListMsg msg -> 
        match model.ChickenListModel with
        | None -> 
            model, Cmd.none
        | Some listModel ->
            let (listModel, result) = ChickenCardList.update chickenCardApi msg listModel
            let model = { model with ChickenListModel = Some listModel }
            let model, cmd =
                match result with
                | ChickenCardList.Internal cmd ->
                    model, cmd |> Cmd.map ChickenListMsg

                | ChickenCardList.External msg -> 
                    let handleCountChange chickenId handler =   
                        match model.TotalEggCount with
                        | Some totalEggCount ->
                            let newTotal = 
                                totalEggCount.[chickenId] 
                                |> handler
                            match newTotal with
                            | Ok count ->
                                { model with TotalEggCount = totalEggCount |> Map.add chickenId count |> Some }, 
                                Cmd.none
                            | Result.Error (ValidationError (param, msg)) ->
                                let errorMsg = sprintf "%s : %s" param msg
                                { model with Errors = errorMsg :: model.Errors }, 
                                Cmd.none
                        | None ->
                            model, Cmd.none

                    match msg with
                    | ChickenCard.ExternalMsg.AddedEgg chickenId -> 
                        handleCountChange chickenId EggCount.increase

                    | ChickenCard.ExternalMsg.RemovedEgg chickenId ->
                        handleCountChange chickenId EggCount.decrease

                    | ChickenCard.ExternalMsg.Error msg ->
                        { model with Errors = msg :: model.Errors }, Cmd.none

            model, cmd

    | ClearErrors -> 
        { model with Errors = [] }, Cmd.none
    | ChangeDate date -> 
        { model with CurrentDate = date }, FetchEggCountOnDate date |> Cmd.ofMsg


let view (model: Model) (dispatch: Msg -> unit) =

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
            |> ViewComponents.apiErrorMsg (fun _ -> dispatch ClearErrors) 

        model.Errors |> List.map errorFor

    let hasErrors = model.Errors |> List.isEmpty |> not

    let chickenEggView =
        match model.ChickenListModel with
        | None ->
            Section.section 
                [ ]
                [ 
                    header
                ]

        | Some listModel ->
            Section.section 
                [ ]
                [ 
                    header
                    DatePicker.view model.CurrentDate (ChangeDate >> dispatch)
                    ChickenCardList.view listModel (ChickenListMsg >> dispatch) 
                ]

    div []
        [
            yield PageLoader.pageLoader 
                [ 
                    PageLoader.Color IsInfo
                    PageLoader.IsActive (model.ChickenListModel.IsNone)  
                ] 
                [ ] 
            if hasErrors then yield Section.section [ ] ( errorView )
            if model.ChickenListModel.IsSome then 
                yield Section.section [] 
                    [ 
                        chickenEggView 
                        Statistics.view { Chickens = model.Chickens; TotalEggCount = model.TotalEggCount }
                    ]
        ]