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

type Msg = 
    | FetchChickens
    | FetchedChickens of Chicken list
    | FetchFailed of string
    | ClearErrorMsg


let update (chickenCheckApi: IChickenCheckApi) (requestBuilder: SecureRequestBuilder) msg (model: ChickensPageModel) =
    let fetchSuccess result = 
        match result with
        | Ok customers ->
            customers |> FetchedChickens
        | Result.Error (err:DomainError) -> err.ErrorMsg |> FetchFailed
            

    let fetchError _ = "Serverfel" |> FetchFailed

    match msg with
    | FetchChickens -> 
        let request = requestBuilder.Build ()
        { model with FetchStatus = Running }, Cmd.OfAsync.either chickenCheckApi.GetChickens request fetchSuccess fetchError
    | FetchedChickens customers -> 
        { model with FetchStatus = ApiCallStatus.Completed; Chickens = customers }, Cmd.none
    | FetchFailed msg ->
        { model with FetchStatus = Failed msg }, Cmd.none
    | ClearErrorMsg ->
        { model with FetchStatus = NotStarted }, Cmd.none

let styles (theme : ITheme) : IStyles list =
    [
        Styles.Root [
            Display DisplayOptions.Flex
            FlexWrap "wrap"
            JustifyContent "space-around"
            BackgroundColor theme.palette.background.paper
        ]
        Styles.Custom("gridList", [ 
            Width 800
            Height 450 
        ] )
        Styles.Icon [
            Color "rgba(255, 255, 255, 0.54)"
        ]
        Styles.Error [
            BackgroundColor theme.palette.error.dark
        ]
    ]

let private view' (classes: Mui.IClasses) (model: ChickensPageModel) (dispatch: Msg -> unit) =
    let loading =
        Mui.circularProgress [] 

    let chickenTiles =  
        match model.Chickens with
        | [] -> Mui.typography [ Variant TypographyVariant.H6 ] [ str "Du har inga hönor" ] |> List.singleton
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
                        [ MaterialProp.Color ComponentColor.Primary ] 
                        [ minusIcon [] ]
                let addEggButton = 
                    Mui.iconButton 
                        [ MaterialProp.Color ComponentColor.Primary ] 
                        [ plusIcon [] ]

                let buttons = span [] [ removeEggButton; addEggButton ]
                Mui.gridListTile 
                    [] 
                    [ img 
                        [ if hasImage then yield Src imageUrl
                          yield Alt chicken.Name.Value ]
                      Mui.gridListTileBar 
                        [ GridListTileBarProp.Title title 
                          GridListTileBarProp.Subtitle subTitle 
                          GridListTileBarProp.ActionIcon buttons ]
                        [] ]
                
            chickens |> List.map chickenTile 

    let chickenList =
        div
            [ Class !!classes?root ]
            [ Mui.gridList 
               [ Class !!classes?gridList ] 
               chickenTiles ]

    let tryAgainButton =
        button [] [ str "Försök igen" ]

    if model.FetchStatus = ApiCallStatus.NotStarted
        then FetchChickens |> dispatch

    let content = 
        [ yield 
            match model.FetchStatus with
            | NotStarted | Running -> loading
            | ApiCallStatus.Completed -> chickenList
            | Failed _ -> tryAgainButton ] 
        |> ViewComponents.centered 

    let clearAction = fun _ -> ClearErrorMsg |> dispatch
    div []
        [ content
          ViewComponents.apiErrorMsg clearAction !!classes?error model.FetchStatus ]


// Workaround for using JSS with Elmish
// https://github.com/mvsmal/fable-material-ui/issues/4#issuecomment-423477900
type private IProps =
    abstract member Model: ChickensPageModel with get, set
    abstract member Dispatch: (Msg -> unit) with get, set
    inherit Mui.IClassesProps

type private Component(p) =
    inherit PureStatelessComponent<IProps>(p)
    let viewFun (p: IProps) = view' p.classes p.Model p.Dispatch
    let viewWithStyles = Mui.withStyles (StyleType.Func styles) [] viewFun
    override this.render() = ReactElementType.create !!viewWithStyles this.props []

let view (model: ChickensPageModel) (dispatch: Msg -> unit) : ReactElement =
    let props = jsOptions<IProps>(fun p ->
        p.Model <- model
        p.Dispatch <- dispatch)
    ofType<Component,_,_> props [] 
