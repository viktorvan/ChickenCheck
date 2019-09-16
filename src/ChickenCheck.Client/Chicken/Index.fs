module ChickenCheck.Client.Chicken.Index

open Fable.MaterialUI
module Mui = Fable.MaterialUI.Core
open Fable.React.Props
open Fable.MaterialUI.Icons
open Fable.MaterialUI.MaterialDesignIcons
open Fable.React
open Fable.Core.JsInterop
open ChickenCheck.Client
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open Elmish
open ChickenCheck.Client.Pages
open System

type EggCount = Map<ChickenId, NaturalNum>

type Msg = 
    | FetchChickens
    | FetchedChickens of Chicken list
    | FetchChickensFailed of string
    | OnChangeDate of Date
    | FetchEggCountOnDate of Date
    | FetchedEggCountOnDate of Date * EggCount
    | FetchEggCountOnDateFailed of string
    | FetchTotalEggCount
    | FetchedTotalEggCount of EggCount
    | FetchTotalEggCountFailed of string
    | AddEgg of ChickenId * Date
    | AddedEgg of ChickenId * Date
    | AddEggFailed of string
    | ClearChickensErrorMsg
    | ClearEggCountOnDateErrorMsg
    | ClearTotalEggCountErrorMsg
    | ClearAddEggErrorMsg


let update (chickenCheckApi: IChickenCheckApi) (requestBuilder: SecureRequestBuilder) msg (model: ChickenIndexModel) =

    match msg with
    | FetchChickens -> 
        let fetchSuccess result = 
            match result with
            | Ok customers ->
                customers |> FetchedChickens
            | Result.Error (err:DomainError) -> err.ErrorMsg |> FetchChickensFailed
        let fetchError _ = "Serverfel" |> FetchChickensFailed
        let request = requestBuilder.Build ()
        { model with FetchChickensStatus = Running }, 
        Cmd.OfAsync.either chickenCheckApi.GetChickens request fetchSuccess fetchError

    | FetchedChickens chickens -> 
        { model with FetchChickensStatus = ApiCallStatus.Completed
                     Chickens = chickens }, [ Cmd.ofMsg FetchTotalEggCount; Cmd.ofMsg (FetchEggCountOnDate model.SelectedDate) ] |> Cmd.batch

    | FetchChickensFailed msg ->
        { model with FetchChickensStatus = Failed msg }, Cmd.none

    | OnChangeDate date ->
        { model with SelectedDate = date }, FetchEggCountOnDate date |> Cmd.ofMsg

    | FetchEggCountOnDate date -> 
        let fetchSuccess result = 
            match result with
            | Ok (eggsByChicken) ->
                (date, eggsByChicken) |> FetchedEggCountOnDate
            | Result.Error (err:DomainError) -> err.ErrorMsg |> FetchEggCountOnDateFailed
        let fetchError _ = "Serverfel" |> FetchEggCountOnDateFailed
        let request = requestBuilder.Build date
        { model with FetchEggCountOnDateStatus = Running }, 
        Cmd.OfAsync.either chickenCheckApi.GetEggCountOnDate request fetchSuccess fetchError

    | FetchedEggCountOnDate (date, countByChicken) -> 
        if model.SelectedDate = date then
            { model with FetchEggCountOnDateStatus = Completed
                         EggCountOnDate = Some countByChicken }, Cmd.none
        else
            model, Cmd.none

    | FetchEggCountOnDateFailed msg -> 
        { model with FetchEggCountOnDateStatus = Failed msg }, Cmd.none

    | FetchTotalEggCount -> 
        let fetchSuccess result = 
            match result with
            | Ok (eggsByChicken) ->
                (eggsByChicken) |> FetchedTotalEggCount
            | Result.Error (err:DomainError) -> err.ErrorMsg |> FetchTotalEggCountFailed
        let fetchError _ = "Serverfel" |> FetchChickensFailed
        let request = requestBuilder.Build ()
        { model with FetchEggCountOnDateStatus = Running }, 
        Cmd.OfAsync.either chickenCheckApi.GetTotalEggCount request fetchSuccess fetchError

    | FetchedTotalEggCount countByChicken -> 
        { model with FetchTotalEggCountStatus = Completed
                     TotalEggCount = Some countByChicken }, Cmd.none

    | FetchTotalEggCountFailed msg -> 
        { model with FetchTotalEggCountStatus = Failed msg }, Cmd.none

    | AddEgg (chickenId, date) -> 
        let addSuccess result = 
            match result with
            | Ok () -> AddedEgg (chickenId, date)
            | Result.Error (err:DomainError) -> err.ErrorMsg |> AddEggFailed
        let fetchError _ = "Serverfel" |> AddEggFailed
        let cmd = { AddEgg.ChickenId = chickenId; Date = date }
        let request = requestBuilder.Build cmd
        { model with AddEggStatus = Running }, 
        Cmd.OfAsync.either chickenCheckApi.AddEgg request addSuccess fetchError

    | AddedEgg (chickenId, date) -> 
        match model.TotalEggCount, model.EggCountOnDate with
        | None, _ | _, None -> model, AddEggFailed "Could not add egg" |> Cmd.ofMsg
        | Some totalCount, Some onDateCount ->
            let newTotal = totalCount.[chickenId].Value + 1 |> NaturalNum.create
            let newOnDate = 
                if date = model.SelectedDate then
                    onDateCount.[chickenId].Value + 1 |> NaturalNum.create
                else onDateCount.[chickenId] |> Ok
            match (newTotal, newOnDate) with
            | Ok newTotal, Ok newOnDate ->
                { model with TotalEggCount = model.TotalEggCount |> Option.map (fun total -> total |> Map.add chickenId newTotal)
                             EggCountOnDate = model.EggCountOnDate |> Option.map (fun onDate -> onDate |> Map.add chickenId newOnDate) }, Cmd.none
            | Result.Error _, _ | _, Result.Error _ -> model,AddEggFailed "Could not add egg" |> Cmd.ofMsg 

    | AddEggFailed msg -> 
        { model with AddEggStatus = Failed msg }, Cmd.none

    | ClearChickensErrorMsg ->
        { model with FetchChickensStatus = NotStarted }, Cmd.none

    | ClearEggCountOnDateErrorMsg ->
        { model with FetchEggCountOnDateStatus = NotStarted }, Cmd.none

    | ClearTotalEggCountErrorMsg ->
        { model with FetchTotalEggCountStatus = NotStarted }, Cmd.none

    | ClearAddEggErrorMsg ->
        { model with AddEggStatus = NotStarted }, Cmd.none

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
    let chickenTiles chickens (totalCount: EggCount option, countOnDate: EggCount option) onAddEgg =  
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
                        [ MaterialProp.Color ComponentColor.Secondary ] 
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

    let chickenList (classes: Mui.IClasses) (chickens: Chicken list) eggCount onAddEgg =
        div
            [ Class !!classes?root ]
            [ Mui.gridList 
               [ Class !!classes?gridList ] 
               (chickenTiles chickens eggCount onAddEgg) ]

    let tryAgainButton =
        button [] [ str "Försök igen" ]

    let view (classes: Mui.IClasses) chickens eggCount fetchStatus onAddEgg =
        [ yield 
            match fetchStatus with
            | (Completed, Completed, Completed) -> chickenList classes chickens eggCount onAddEgg
            | (Failed _,_,_) | (_, Failed _, _) | (_, _, Failed _) -> tryAgainButton 
            | _ -> ViewComponents.loading ]

let private view' (classes: Mui.IClasses) (model: ChickenIndexModel) (dispatch: Msg -> unit) =

    if model.FetchChickensStatus = ApiCallStatus.NotStarted
        then FetchChickens |> dispatch

    let onChangeDate = 
        DateTime.Parse
        >> Date.create
        >> OnChangeDate
        >> dispatch

    let onAddEgg chickenId = AddEgg (chickenId, model.SelectedDate) |> dispatch
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
                  onAddEgg ) ]
        |> ViewComponents.centered 

    let clearAction msg = fun _ -> msg |> dispatch

    div []
        [ content
          ViewComponents.apiErrorMsg (clearAction ClearChickensErrorMsg) !!classes?error model.FetchChickensStatus 
          ViewComponents.apiErrorMsg (clearAction ClearEggCountOnDateErrorMsg) !!classes?error model.FetchEggCountOnDateStatus 
          ViewComponents.apiErrorMsg (clearAction ClearAddEggErrorMsg) !!classes?error model.AddEggStatus ]


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
