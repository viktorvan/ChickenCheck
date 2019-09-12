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
open Elmish
open ChickenCheck.Client.Pages
open System

type Msg = 
    | FetchChickens
    | FetchedChickens of Chicken list
    | FetchChickensFailed of string
    | OnChangeDate of DateTime
    | FetchEggsOnDate of DateTime
    | FetchedEggsOnDate of DateTime * (ChickenId * NaturalNum)
    | ClearErrorMsg


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
        { model with FetchStatus = Running }, 
        Cmd.OfAsync.either chickenCheckApi.GetChickens request fetchSuccess fetchError
    | FetchedChickens customers -> 
        { model with FetchStatus = ApiCallStatus.Completed; Chickens = customers }, FetchEggsOnDate model.SelectedDate |> Cmd.ofMsg
    | FetchChickensFailed msg ->
        { model with FetchStatus = Failed msg }, Cmd.none
    | OnChangeDate date ->
        { model with SelectedDate = date }, Cmd.none
    | FetchEggsOnDate(_) -> model, Cmd.none
    | FetchedEggsOnDate _ -> failwith "Not Implemented"
    | ClearErrorMsg ->
        { model with FetchStatus = NotStarted }, Cmd.none

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
    let view (classes: Mui.IClasses) (selectedDate: DateTime) onChangeDate = 
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
    let chickenTiles chickens =  
        match chickens with
        | [] -> Mui.typography [ Variant TypographyVariant.H6 ] [ str "Har du inga hönor?" ] |> List.singleton
        | chickens ->
            let chickenTile (chicken: Chicken) =
                let hasImage, imageUrl = 
                    match chicken.ImageUrl with
                    | Some (ImageUrl imageUrl) -> true, imageUrl
                    | None -> false, ""

                let title = Mui.typography [ Variant TypographyVariant.H6 ] [ str chicken.Name.Value ] 
                let subTitle = Mui.typography [] [ str (chicken.Breed.Value) ]
                let removeEggButton = 
                    Mui.iconButton 
                        [ MaterialProp.Color ComponentColor.Secondary ] 
                        [ minusIcon [] ]
                let addEggButton = 
                    Mui.iconButton 
                        [ MaterialProp.Color ComponentColor.Secondary ] 
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
                
            chickens |> List.map chickenTile 

    let chickenList (classes: Mui.IClasses) (chickens) =
        div
            [ Class !!classes?root ]
            [ Mui.gridList 
               [ Class !!classes?gridList ] 
               (chickenTiles chickens) ]

    let tryAgainButton =
        button [] [ str "Försök igen" ]

    let view (classes: Mui.IClasses) (chickens, fetchStatus) =
        [ yield 
            match fetchStatus with
            | NotStarted | Running -> ViewComponents.loading
            | ApiCallStatus.Completed -> chickenList classes chickens
            | Failed _ -> tryAgainButton ] 

let private view' (classes: Mui.IClasses) (model: ChickenIndexModel) (dispatch: Msg -> unit) =

    if model.FetchStatus = ApiCallStatus.NotStarted
        then FetchChickens |> dispatch

    let onChangeDate = 
        DateTime.Parse
        >> OnChangeDate
        >> dispatch

    let content = 
        [ Mui.typography [ Variant TypographyVariant.H6 ] [ str "Vem värpte?" ]
          DateNavigator.view classes model.SelectedDate onChangeDate 
          div [] (ChickenList.view classes (model.Chickens, model.FetchStatus) ) ]
        |> ViewComponents.centered 

    let clearAction = fun _ -> ClearErrorMsg |> dispatch

    div []
        [ content
          ViewComponents.apiErrorMsg clearAction !!classes?error model.FetchStatus ]


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
