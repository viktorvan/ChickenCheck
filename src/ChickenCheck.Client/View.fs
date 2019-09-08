module View


open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props
module Mui = Fable.MaterialUI.Core
open Fable.MaterialUI.MaterialDesignIcons
open Fable.MaterialUI.Icons
open Fable.MaterialUI.Props
open Fable.MaterialUI.Themes

open ChickenCheck.Client
open Messages
open ChickenCheck.Client.Pages
open Elmish.React.Common
open ChickenCheck.Domain.Helpers
open Elmish

let private styles (theme : ITheme) : IStyles list =
    [
        Styles.Toolbar [
            FlexWrap "wrap"
        ]
        Styles.Custom("toolbarTitle", [
            FlexGrow 1
        ])
        Styles.Custom("appBar", [
            BorderBottom(sprintf "1px solid %s" theme.palette.divider |> Mui.toObj) 
        ])
        Styles.Custom("link", [
            MarginLeft theme.spacing.unit
            MarginRight (theme.spacing.unit * 2)
        ])
        Styles.Custom("bottomNavigation", [
            Width "100%"
            CSSProp.Position PositionOptions.Fixed
            Bottom 0
        ])
        Styles.Content [
            MarginTop theme.spacing.unit
            MarginBottom theme.spacing.unit
        ]
    ]

let private view' (classes: Mui.IClasses) model dispatch =

    let pageHtml (page : Page) =
        match page with
        | SigninPage pageModel -> lazyView2 Session.Signin.view pageModel (SigninMsg >> dispatch)
        | ChickensPage pageModel -> lazyView2 Chicken.Index.view pageModel (IndexMsg >> ChickenMsg >> dispatch)

    let bottomNav =
        let getValue =
            match model.CurrentPage with
            | ChickensPage _ -> 0
            | _ -> -1
        Mui.bottomNavigation 
            [ ShowLabels true
              MaterialProp.Value getValue
              Class !!classes?bottomNavigation ] 
            [ Mui.bottomNavigationAction 
                [ OnClick (fun _ -> dispatch GoToChickens)
                  MaterialProp.Icon (accountGroupIcon []) 
                  MaterialProp.Label (Mui.typography [] [ str "hönor" ]) ] ]

    let isLoggedIn, loggedInUsername =
        match model.Session with
        | None -> false, ""
        | Some session -> true, session.Name.Value

    div [] 
        [ yield Mui.cssBaseline []
          if isLoggedIn then
              yield Mui.appBar 
                  [ AppBarProp.Position AppBarPosition.Static
                    Class !!classes?appBar ] 
                  [ Mui.toolbar 
                      [ Class !!classes?toolbar ] 
                      [ Mui.typography 
                          [ Variant TypographyVariant.H6
                            Class !!classes?toolbarTitle ] 
                          [ str "ChickenCheck" ]
                        str loggedInUsername ] ] 
          yield div [ Class !!classes?content ] [ pageHtml model.CurrentPage ]
          if isLoggedIn then yield bottomNav ] 


// Boilerplate below to support JSS with Elmish

type private IProps =
    abstract member model: Model with get, set
    abstract member dispatch: (Msg -> unit) with get, set
    inherit Mui.IClassesProps

type private Component(p) =
    inherit PureStatelessComponent<IProps>(p)
    let viewFun (p: IProps) = view' p.classes p.model p.dispatch
    let viewWithStyles = Mui.withStyles (StyleType.Func styles) [] viewFun
    override this.render() = ReactElementType.create !!viewWithStyles this.props []

let view (model: Model) (dispatch: Msg -> unit) : ReactElement =
    let props = jsOptions<IProps>(fun p ->
        p.model <- model
        p.dispatch <- dispatch)
    ofType<Component,_,_> props []