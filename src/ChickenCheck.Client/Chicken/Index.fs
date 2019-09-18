module ChickenCheck.Client.Chicken.Index

open Fable.MaterialUI
module Mui = Fable.MaterialUI.Core
open Fable.React.Props
open Fable.MaterialUI.MaterialDesignIcons
open Fable.React
open Fable.Core.JsInterop
open ChickenCheck.Client
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open Elmish
open System

type EggCount = Map<ChickenId, NaturalNum>

type Chickens =
    | Request
    | Result of Chicken list
    | Error of string
    | ClearError

type TotalCount =
    | Request
    | Result of EggCount
    | Error of string
    | ClearError

type EggCountOnDate =
    | Request of Date
    | Result of Date * EggCount
    | Error of string
    | ClearError

type AddEgg =
    | Request of ChickenId * Date
    | Result of ChickenId * Date
    | Error of string
    | ClearError

type RemoveEgg =
    | Request of ChickenId * Date
    | Result of ChickenId * Date
    | Error of string
    | ClearError

type Msg = 
    | Chickens of Chickens
    | TotalCount of TotalCount
    | EggCountOnDate of EggCountOnDate
    | AddEgg of AddEgg
    | RemoveEgg of RemoveEgg
    | OnChangeDate of Date


let update (chickenCheckApi: IChickenCheckApi) (requestBuilder: SecureRequestBuilder) msg (model: ChickenIndexModel) =
    let callApi apiFunc arg successMsg errorMsg =
        let ofSuccess = function
            | Ok res -> res |> successMsg
            | Result.Error (err:DomainError) -> err.ErrorMsg |> errorMsg
        let ofError _ = "Serverfel" |> errorMsg
        Cmd.OfAsync.either apiFunc (requestBuilder.Build arg) ofSuccess ofError

    let handleChickens = function
        | Chickens.Request -> 
            { model with FetchChickensStatus = Running }, 
            (callApi
                chickenCheckApi.GetChickens 
                () 
                (Chickens.Result >> Chickens) 
                (Chickens.Error >> Chickens))

        | Chickens.Result chickens -> 
            { model with FetchChickensStatus = ApiCallStatus.Completed
                         Chickens = chickens }, 
                         [ Cmd.ofMsg (TotalCount.Request |> TotalCount)
                           Cmd.ofMsg (EggCountOnDate.Request model.SelectedDate |> EggCountOnDate) ] 
                           |> Cmd.batch

        | Chickens.Error msg ->
            { model with FetchChickensStatus = Failed msg }, Cmd.none

        | Chickens.ClearError ->
            { model with FetchChickensStatus = NotStarted }, Cmd.none

    let handleEggCountOnDate = function
        | EggCountOnDate.Request date -> 
            { model with FetchEggCountOnDateStatus = Running }, 
                callApi
                    chickenCheckApi.GetEggCountOnDate 
                    date 
                    ((fun res -> EggCountOnDate.Result (date, res)) >> EggCountOnDate) 
                    (EggCountOnDate.Error >> EggCountOnDate)

        | EggCountOnDate.Result (date, countByChicken) -> 
            if model.SelectedDate = date then
                { model with FetchEggCountOnDateStatus = Completed
                             EggCountOnDate = Some countByChicken }, Cmd.none
            else
                model, Cmd.none

        | EggCountOnDate.Error msg -> 
            { model with FetchEggCountOnDateStatus = Failed msg }, Cmd.none

        | EggCountOnDate.ClearError ->
            { model with FetchEggCountOnDateStatus = NotStarted }, Cmd.none

    let handleTotalCount = function
        | TotalCount.Request -> 
            { model with FetchTotalEggCountStatus = Running }, 
            callApi
                chickenCheckApi.GetTotalEggCount
                ()
                (TotalCount.Result >> TotalCount)
                (TotalCount.Error >> TotalCount)

        | TotalCount.Result countByChicken -> 
            { model with FetchTotalEggCountStatus = Completed
                         TotalEggCount = Some countByChicken }, Cmd.none

        | TotalCount.Error msg -> 
            { model with FetchTotalEggCountStatus = Failed msg }, Cmd.none

        | TotalCount.ClearError ->
            { model with FetchTotalEggCountStatus = NotStarted }, Cmd.none

    let handleAddEgg = function
        | AddEgg.Request (chickenId, date) -> 
            { model with AddEggStatus = Running }, 
            callApi
                chickenCheckApi.AddEgg
                { AddEgg.ChickenId = chickenId; Date = date }
                ((fun _ -> AddEgg.Result (chickenId, date)) >> AddEgg) 
                (AddEgg.Error >> AddEgg)

        | AddEgg.Result (chickenId, date) -> 
            match model.TotalEggCount, model.EggCountOnDate with
            | None, _ | _, None -> model, AddEgg.Error "Could not add egg" |> AddEgg |> Cmd.ofMsg
            | Some totalCount, Some onDateCount ->
                let newTotal = totalCount.[chickenId].Value + 1 |> NaturalNum.create
                let newOnDate = 
                    if date = model.SelectedDate then
                        onDateCount.[chickenId].Value + 1 |> NaturalNum.create
                    else onDateCount.[chickenId] |> Ok
                match (newTotal, newOnDate) with
                | Ok newTotal, Ok newOnDate ->
                    { model with AddEggStatus = Completed
                                 TotalEggCount = model.TotalEggCount |> Option.map (fun total -> total |> Map.add chickenId newTotal)
                                 EggCountOnDate = model.EggCountOnDate |> Option.map (fun onDate -> onDate |> Map.add chickenId newOnDate) }, Cmd.none
                | Result.Error _, _ | _, Result.Error _ -> model,AddEgg.Error "Could not add egg" |> AddEgg |> Cmd.ofMsg 

        | AddEgg.Error msg -> 
            { model with AddEggStatus = Failed msg }, Cmd.none

        | AddEgg.ClearError ->
            { model with AddEggStatus = NotStarted }, Cmd.none

    let handleRemoveEgg = function
        | RemoveEgg.Request (chickenId, date) -> 
            { model with RemoveEggStatus = Running }, 
            callApi
                chickenCheckApi.RemoveEgg
                { RemoveEgg.ChickenId = chickenId; Date = date }
                ((fun _ -> RemoveEgg.Result (chickenId, date)) >> RemoveEgg) 
                (RemoveEgg.Error >> RemoveEgg)

        | RemoveEgg.Result (chickenId, date) -> 
            match model.TotalEggCount, model.EggCountOnDate with
            | None, _ | _, None -> model, RemoveEgg.Error "Could not remove egg" |> RemoveEgg |> Cmd.ofMsg
            | Some totalCount, Some onDateCount ->
                let newTotal = 
                    let current = totalCount.[chickenId].Value
                    if current < 1 then 0 |> NaturalNum.create
                    else current - 1 |> NaturalNum.create
                let newOnDate = 
                    if date = model.SelectedDate && onDateCount.[chickenId].Value > 0 then
                        onDateCount.[chickenId].Value - 1 |> NaturalNum.create
                    else onDateCount.[chickenId] |> Ok
                match (newTotal, newOnDate) with
                | Ok newTotal, Ok newOnDate ->
                    { model with RemoveEggStatus = Completed
                                 TotalEggCount = model.TotalEggCount |> Option.map (fun total -> total |> Map.add chickenId newTotal)
                                 EggCountOnDate = model.EggCountOnDate |> Option.map (fun onDate -> onDate |> Map.add chickenId newOnDate) }, Cmd.none
                | Result.Error _, _ | _, Result.Error _ -> model, RemoveEgg.Error "Could not add egg" |> RemoveEgg |> Cmd.ofMsg 

        | RemoveEgg.Error msg -> 
            { model with RemoveEggStatus = Failed msg }, Cmd.none

        | RemoveEgg.ClearError ->
            { model with RemoveEggStatus = NotStarted }, Cmd.none

    match msg with
    | Chickens msg -> handleChickens msg
    | EggCountOnDate msg -> handleEggCountOnDate msg
    | TotalCount msg -> handleTotalCount msg
    | AddEgg msg -> handleAddEgg msg
    | RemoveEgg msg -> handleRemoveEgg msg
    | OnChangeDate date ->
        { model with SelectedDate = date }, EggCountOnDate.Request date |> EggCountOnDate |> Cmd.ofMsg

let styles (theme : ITheme) : IStyles list =
    [
        Styles.Root [
            Width "100%"
        ]
        Styles.Icon [
            Color "rgba(255, 255, 255, 0.54)"
        ]
        Styles.Error [
            BackgroundColor theme.palette.error.dark
        ]
    ]

module internal DateNavigator = 
    let view (classes: Mui.IClasses) (selectedDate: Date) onChangeDate = 
        let selectedDate = selectedDate |> Date.toDateTime
        div [ Class !!classes?root ]
            [ Mui.appBar [ AppBarProp.Position AppBarPosition.Static ] 
                [ Mui.tabs 
                    [ Value false ]
                    [ Mui.tab 
                        [ OnClick (fun _ -> onChangeDate (selectedDate.AddDays(-1.).ToString()))
                          Label "Föregående" ] 
                      Mui.tab 
                          [ Label (selectedDate.ToShortDateString()) ]
                      Mui.tab 
                          [ OnClick (fun _ -> onChangeDate (selectedDate.AddDays(1.).ToString()))
                            Label "Nästa"; Disabled (selectedDate.AddDays(1.) > DateTime.Today) ] ] ] 
              (ViewComponents.datePicker "date" onChangeDate selectedDate) |> List.singleton |> ViewComponents.centered ]

module internal ChickenList =
    let chickenTiles chickens (totalCount: EggCount option, countOnDate: EggCount option) onAddEgg onRemoveEgg =  
        match chickens with
        | [] ->
            Mui.typography [ Variant TypographyVariant.H6 ] [ str "Har du inga hönor?" ] |> List.singleton
        | chickens ->
            let chickenTile (chicken: Chicken) =
                let hasImage, imageUrl = 
                    match chicken.ImageUrl with
                    | Some (ImageUrl imageUrl) -> true, imageUrl
                    | None -> false, ""

                let title = Mui.typography [ Variant TypographyVariant.H6 ] [ str chicken.Name.Value ] 
                let subTitle = 
                    let getCountStr (countMap:EggCount option) = 
                        countMap
                        |> Option.map (fun c -> c.[chicken.Id].Value |> string) 
                        |> Option.defaultValue "-"
                    let total = getCountStr totalCount
                    let onDate = getCountStr countOnDate

                    Mui.typography [] [ sprintf "%s (totalt: %s)" onDate total |> str ]
                let removeEggButton = 
                    Mui.iconButton 
                        [ OnClick (fun _ -> onRemoveEgg chicken.Id)
                          MaterialProp.Color ComponentColor.Secondary ] 
                        [ minusIcon [] ]
                let addEggButton = 
                    Mui.iconButton 
                        [ OnClick (fun _ -> onAddEgg chicken.Id )
                          MaterialProp.Color ComponentColor.Secondary ] 
                        [ plusIcon [] ]

                let buttons = span [] [ removeEggButton; addEggButton ]
                Mui.gridListTile 
                    [ Style [ Width "300px" ] ] 
                    [ img 
                        [ if hasImage then yield Src imageUrl
                          yield Alt chicken.Name.Value ]
                      Mui.gridListTileBar 
                        [ GridListTileBarProp.Title title 
                          GridListTileBarProp.Subtitle subTitle 
                          GridListTileBarProp.ActionIcon buttons ]
                        [] ]
                

            chickens 
            |> List.map (chickenTile )

    let chickenList (classes: Mui.IClasses) (chickens: Chicken list) eggCount onAddEgg onRemoveEgg =
        div
            [ Class !!classes?root ]
            [ Mui.gridList 
               [ Class !!classes?gridList ] 
               (chickenTiles chickens eggCount onAddEgg onRemoveEgg) ]

    let tryAgainButton =
        button [] [ str "Försök igen" ]

    let view (classes: Mui.IClasses) chickens eggCount fetchStatus onAddEgg onRemoveEgg =
        [ yield 
            match fetchStatus with
            | (Completed, Completed, Completed) -> chickenList classes chickens eggCount onAddEgg onRemoveEgg
            | (Failed _,_,_) | (_, Failed _, _) | (_, _, Failed _) -> tryAgainButton 
            | _ -> ViewComponents.loading ]

let private view' (classes: Mui.IClasses) (model: ChickenIndexModel) (dispatch: Msg -> unit) =

    if model.FetchChickensStatus = ApiCallStatus.NotStarted
        then Chickens.Request |> Chickens |> dispatch

    let onChangeDate = 
        DateTime.Parse
        >> Date.create
        >> OnChangeDate
        >> dispatch

    let onAddEgg chickenId = AddEgg.Request (chickenId, model.SelectedDate) |> AddEgg |> dispatch
    let onRemoveEgg chickenId = RemoveEgg.Request (chickenId, model.SelectedDate) |> RemoveEgg |> dispatch
    let content = 
        [ Mui.typography [ Variant TypographyVariant.H6 ] [ str "Vem värpte?" ]
          DateNavigator.view classes model.SelectedDate onChangeDate 
          div 
              [] 
              (ChickenList.view 
                  classes 
                  model.Chickens 
                  (model.TotalEggCount, model.EggCountOnDate) 
                  (model.FetchChickensStatus, model.FetchTotalEggCountStatus, model.FetchEggCountOnDateStatus) 
                  onAddEgg 
                  onRemoveEgg) ]
        |> ViewComponents.centered 

    let clearAction msg = fun _ -> msg |> dispatch

    div []
        [ content
          ViewComponents.apiErrorMsg 
              (clearAction (Chickens.ClearError |> Chickens)) 
              !!classes?error model.FetchChickensStatus 
          ViewComponents.apiErrorMsg 
              (clearAction (EggCountOnDate.ClearError |> EggCountOnDate)) 
              !!classes?error model.FetchEggCountOnDateStatus 
          ViewComponents.apiErrorMsg 
              (clearAction (TotalCount.ClearError |> TotalCount)) 
              !!classes?error model.FetchTotalEggCountStatus 
          ViewComponents.apiErrorMsg 
              (clearAction (AddEgg.ClearError |> AddEgg)) 
              !!classes?error model.AddEggStatus 
          ViewComponents.apiErrorMsg 
              (clearAction (RemoveEgg.ClearError |> RemoveEgg)) 
              !!classes?error model.RemoveEggStatus ]


// Workaround for using JSS with Elmish
// https://github.com/mvsmal/fable-material-ui/issues/4#issuecomment-423477900
type private IProps =
    abstract member Model: ChickenIndexModel with get, set
    abstract member Dispatch: (Msg -> unit) with get, set
    inherit Mui.IClassesProps

type private Component(p) =
    inherit PureStatelessComponent<IProps>(p)
    let viewFun (p: IProps) = view' p.classes p.Model p.Dispatch
    let viewWithStyles = Mui.withStyles (StyleType.Func styles) [] viewFun
    override this.render() = ReactElementType.create !!viewWithStyles this.props []

let view (model: ChickenIndexModel) (dispatch: Msg -> unit) : ReactElement =
    let props = jsOptions<IProps>(fun p ->
        p.Model <- model
        p.Dispatch <- dispatch)
    ofType<Component,_,_> props [] 
