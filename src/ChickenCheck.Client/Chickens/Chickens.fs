module ChickenCheck.Client.Chickens

open Fable.React.Props
open Fable.React
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open Elmish
open System
open Fulma
open Fable.FontAwesome
open Fulma.Elmish
open Router
open FsToolkit.ErrorHandling
open ChickenCheck.Client
open ChickenCheck.Client.ApiHelpers
open Fulma.Extensions.Wikiki

type Model =
    { Chickens : Chicken list
      ChickenListModel : ChickenCardList.Model option
      Errors : string list
      TotalEggCount : Map<ChickenId, EggCount> option
      EggCountOnDate : Map<ChickenId, EggCount> option
      FetchChickensStatus : ApiCallStatus 
      FetchTotalEggCountStatus : ApiCallStatus 
      FetchEggCountOnDateStatus : ApiCallStatus 
      CurrentDate : Date }


type EggCountMap = Map<ChickenId, EggCount>

[<RequireQualifiedAccess>]
type Chickens =
    | Request
    | Result of Chicken list
    | Error of string

[<RequireQualifiedAccess>]
type TotalCount =
    | Request
    | Result of EggCountMap
    | Error of string

[<RequireQualifiedAccess>]
type EggCountOnDate =
    | Request of Date
    | Result of Date * EggCountMap
    | Error of string


type Msg = 
    | Chickens of Chickens
    | TotalCount of TotalCount
    | EggCountOnDate of EggCountOnDate
    | ChangeDate of Date
    | ChickenListMsg of ChickenCardList.Msg
    | UpdateListModel
    | ClearErrors

let init =
    let date = Date.today
    let cmds =
        [ Chickens.Request |> Chickens |> Cmd.ofMsg
          TotalCount.Request |> TotalCount |> Cmd.ofMsg 
          EggCountOnDate.Request date |> EggCountOnDate |> Cmd.ofMsg ]
        |> Cmd.batch

    { Chickens = []
      ChickenListModel = None
      Errors = []
      TotalEggCount = None
      EggCountOnDate = None
      FetchChickensStatus = NotStarted 
      FetchTotalEggCountStatus = NotStarted 
      FetchEggCountOnDateStatus = NotStarted 
      CurrentDate = date }, cmds


let datePickerView date dispatch =
    let onDateSet date =
        ChangeDate date |> dispatch
    let onDateChange delta =    
        onDateSet (Date.addDays delta date)
    let previousDate _ = onDateChange -1.
    let nextDate _ = onDateChange 1.

    let parseDate (ev: Browser.Types.Event) =
        ev.Value
        |> DateTime.Parse
        |> Date.create

    let dateButton onClick icon =
        Button.a 
            [ 
                Button.IsLink
                Button.OnClick onClick
                Button.Size IsLarge 
            ] 
            [ 
                Icon.icon [] 
                    [ Fa.i 
                        [ 
                            Fa.Size Fa.Fa3x
                            icon
                        ] 
                        [] 
                    ] 
            ]

    Level.level [ Level.Level.IsMobile ]
        [ 
            Level.item []
                [ 
                    dateButton previousDate Fa.Solid.CaretLeft
                ]
            Level.item []
                [ 
                    Field.div 
                        [ 
                            Field.Props [ Style [ Width "100%" ] ] 
                        ]
                        [ 
                            Input.date
                                [ 
                                    Input.OnChange (parseDate >> onDateSet) 
                                    date.ToDateTime().ToString("yyyy-MM-dd") |> Input.Value
                                ] 
                        ] 
                ]
            Level.item []
                [ 
                    dateButton nextDate Fa.Solid.CaretRight
                ] 
        ]

let update (chickenApi: IChickenApi) apiToken msg (model: Model) =
    let handleChickens = function
        | Chickens.Request -> 
            { model with FetchChickensStatus = Running }, 
            (callSecureApi
                apiToken
                chickenApi.GetChickens 
                () 
                (Chickens.Result >> Chickens) 
                (Chickens.Error >> Chickens))

        | Chickens.Result chickens -> 
            { model with 
                FetchChickensStatus = ApiCallStatus.Completed
                Chickens = chickens },
                UpdateListModel |> Cmd.ofMsg

        | Chickens.Error msg ->
            { model with FetchChickensStatus = Failed msg }, Cmd.none

    let handleEggCountOnDate = function
        | EggCountOnDate.Request date -> 
            let callApi =
                callSecureApi
                    apiToken
                    chickenApi.GetEggCountOnDate 
                    date 
                    ((fun res -> EggCountOnDate.Result (date, res)) >> EggCountOnDate) 
                    (EggCountOnDate.Error >> EggCountOnDate)

            let cmds =
                [ UpdateListModel |> Cmd.ofMsg
                  callApi ]
                |> Cmd.batch

            { model with 
                FetchEggCountOnDateStatus = Running 
                EggCountOnDate = None }, cmds

        | EggCountOnDate.Result (date, countByChicken) -> 
            let model = { model with FetchEggCountOnDateStatus = Completed }
            if model.CurrentDate = date then
                if countByChicken |> Map.isEmpty then
                    model, "Count by date map was empty" |> EggCountOnDate.Error |> EggCountOnDate |> Cmd.ofMsg
                else
                    { model with EggCountOnDate = Some countByChicken }, 
                        UpdateListModel |> Cmd.ofMsg
            else
                model, Cmd.none

        | EggCountOnDate.Error msg -> 
            { model with FetchEggCountOnDateStatus = Failed msg },
                Cmd.none

    let handleTotalCount = function
        | TotalCount.Request -> 
            { model with 
                FetchTotalEggCountStatus = Running 
                TotalEggCount = None }, 
            callSecureApi
                apiToken
                chickenApi.GetTotalEggCount
                ()
                (TotalCount.Result >> TotalCount)
                (TotalCount.Error >> TotalCount)

        | TotalCount.Result totalCount -> 
            let model = { model with FetchTotalEggCountStatus = Completed }
            if totalCount |> Map.isEmpty then
                model, "TotalCount map was empty" |> TotalCount.Error |> TotalCount |> Cmd.ofMsg
            else
                { model with TotalEggCount = Some totalCount },
                UpdateListModel |> Cmd.ofMsg

        | TotalCount.Error msg -> 
            { model with FetchTotalEggCountStatus = Failed msg },
                Cmd.none

    match msg with
    | Chickens msg -> handleChickens msg
    | EggCountOnDate msg -> handleEggCountOnDate msg
    | TotalCount msg -> handleTotalCount msg
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
            let (listModel, result) = ChickenCardList.update chickenApi apiToken msg listModel
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
        { model with CurrentDate = date }, EggCountOnDate.Request date |> EggCountOnDate |> Cmd.ofMsg

module Statistics =
    let chickenEggCount (countMap: EggCountMap option) (chicken: Chicken) =

        match countMap with
        | Some countMap ->
            let getCountStr chickenId (countMap: EggCountMap) =
                countMap
                |> Map.tryFind chickenId
                |> Option.map EggCount.toString
                |> Option.defaultValue "-"

            let totalCount = getCountStr chicken.Id countMap
            Level.item [ Level.Item.HasTextCentered ]
                [ div []
                    [ Level.heading [] [ chicken.Name.Value |> str ] 
                      Level.title [] [ str totalCount ] ] ]
            |> Some
        | None -> None

    let allCounts model =
        model.Chickens |> List.choose (chickenEggCount model.TotalEggCount)
        |> Level.level []

    let view model =
        Container.container []
            [ Text.p 
                [ Modifiers 
                    [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered)
                      Modifier.TextSize (Screen.All, TextSize.Is2)] ] 
                [ str "Hur mycket har de värpt totalt?" ] 
              allCounts model ]

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
                    datePickerView model.CurrentDate dispatch
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
                        Statistics.view model 
                    ]
        ]