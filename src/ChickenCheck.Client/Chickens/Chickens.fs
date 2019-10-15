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
      TotalEggCount : Map<ChickenId, EggCount>
      EggCountOnDate : Map<ChickenId, EggCount>
      FetchChickensStatus : ApiCallStatus 
      FetchTotalEggCountStatus : ApiCallStatus 
      FetchEggCountOnDateStatus : ApiCallStatus 
      CurrentDate : Date }

let init route =
    match route with
    | ChickenRoute.Chickens ->
        { Chickens = []
          ChickenListModel = None
          Errors = []
          TotalEggCount = Map.empty
          EggCountOnDate = Map.empty
          FetchChickensStatus = NotStarted 
          FetchTotalEggCountStatus = NotStarted 
          FetchEggCountOnDateStatus = NotStarted 
          CurrentDate = Date.today }, Cmd.none


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
    | TryBuildListModel
    | ClearErrors

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

    Level.level [ Level.Level.IsMobile ]
        [ 
            Level.item []
                [ 
                    Button.a 
                        [ Button.IsLink; Button.OnClick previousDate ] 
                        [ Icon.icon [] [ Fa.i [ Fa.Size Fa.Fa2x; Fa.Solid.CaretLeft ] [] ] ] 
                ]
            Level.item []
                [ 
                    Field.div 
                        [ 
                            Field.Props [ Data ("display-mode", "inline") ] 
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
              [ Button.a [ Button.IsLink; Button.OnClick nextDate ] [ Icon.icon [] [ Fa.i [ Fa.Size Fa.Fa2x; Fa.Solid.CaretRight ] [] ] ] ] ]

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
                TryBuildListModel |> Cmd.ofMsg

        | Chickens.Error msg ->
            { model with FetchChickensStatus = Failed msg }, Cmd.none

    let handleEggCountOnDate = function
        | EggCountOnDate.Request date -> 
            { model with FetchEggCountOnDateStatus = Running }, 
                callSecureApi
                    apiToken
                    chickenApi.GetEggCountOnDate 
                    date 
                    ((fun res -> EggCountOnDate.Result (date, res)) >> EggCountOnDate) 
                    (EggCountOnDate.Error >> EggCountOnDate)

        | EggCountOnDate.Result (date, countByChicken) -> 
            if model.CurrentDate = date then
                { model with FetchEggCountOnDateStatus = Completed
                             EggCountOnDate = countByChicken }, 
                TryBuildListModel |> Cmd.ofMsg
            else
                model, Cmd.none

        | EggCountOnDate.Error msg -> 
            { model with FetchEggCountOnDateStatus = Failed msg },
                Cmd.none

    let handleTotalCount = function
        | TotalCount.Request -> 
            { model with FetchTotalEggCountStatus = Running }, 
            callSecureApi
                apiToken
                chickenApi.GetTotalEggCount
                ()
                (TotalCount.Result >> TotalCount)
                (TotalCount.Error >> TotalCount)

        | TotalCount.Result countByChicken -> 
            { model with FetchTotalEggCountStatus = Completed
                         TotalEggCount = countByChicken },
            TryBuildListModel |> Cmd.ofMsg

        | TotalCount.Error msg -> 
            { model with FetchTotalEggCountStatus = Failed msg },
                Cmd.none

    match msg with
    | Chickens msg -> handleChickens msg
    | EggCountOnDate msg -> handleEggCountOnDate msg
    | TotalCount msg -> handleTotalCount msg
    | TryBuildListModel ->
        match (model.Chickens, model.EggCountOnDate) with
        | [], _  -> 
            { model with ChickenListModel = None }, Cmd.none

        | _, map when Map.isEmpty map -> { model with ChickenListModel = None }, Cmd.none

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
                        let newTotal = 
                            model.TotalEggCount.[chickenId] 
                            |> handler
                        match newTotal with
                        | Ok count ->
                            { model with TotalEggCount = model.TotalEggCount |> Map.add chickenId count }, Cmd.none
                        | Result.Error (ValidationError (param, msg)) ->
                            let errorMsg = sprintf "%s : %s" param msg
                            { model with Errors = errorMsg :: model.Errors }, Cmd.none

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
    let chickenEggCount (countMap: EggCountMap) (chicken: Chicken) =

        let getCountStr chickenId (countMap: EggCountMap) =
            countMap
            |> Map.tryFind chickenId
            |> Option.map EggCount.toString
            |> Option.defaultValue "-"

        let totalCount = getCountStr chicken.Id countMap
        Level.item []
            [ div []
                [ Level.heading [] [ chicken.Name.Value |> str ] 
                  Level.title [] [ str totalCount ] ] ]

    let allCounts model =
        model.Chickens |> List.map (chickenEggCount model.TotalEggCount)
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

    match (model.FetchChickensStatus, model.FetchTotalEggCountStatus, model.FetchEggCountOnDateStatus) with
    | NotStarted, _, _ | _, NotStarted, _ | _, _, NotStarted -> 
        [ Chickens.Request |> Chickens
          TotalCount.Request |> TotalCount
          EggCountOnDate.Request model.CurrentDate |> EggCountOnDate ]
        |> List.iter dispatch
    | _ -> ()

    let clearAction msg = fun _ -> msg |> dispatch

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