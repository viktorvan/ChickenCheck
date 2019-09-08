module ChickenCheck.Client.Session.Signin
open Elmish
open ChickenCheck.Client
open ChickenCheck.Client.Pages
open ChickenCheck.Domain
open ChickenCheck.Domain.Session
open Fable.React
open Fable.Core.JsInterop
open ChickenCheck.Client.ApiHelpers

type ExternalMsg =
    | NoOp
    | SignedIn of Session

type Msg =
    | ChangeEmail of string
    | ChangePassword of string
    | LoginCompleted of Session
    | LoginFailed of string
    | ClearLoginError
    | Submit

let update (chickenCheckApi: IChickenCheckApi) msg (model: SigninModel) =
    match msg with
    | ChangeEmail msg ->
        let newEmail =  
            match Email.create msg with
            | Ok e -> StringInput.Valid e
            | Error _ -> StringInput.Invalid msg
        { model with Email = newEmail }, Cmd.none, NoOp

    | ChangePassword msg ->
        let newPassword =
            match Password.create msg with
            | Ok pw -> StringInput.Valid pw
            | Error _ -> StringInput.Invalid msg
        { model with Password = newPassword }, Cmd.none, NoOp

    | LoginCompleted session -> { model with LoginStatus = ApiCallStatus.Completed }, (ChickensPage.init |> newUrl), SignedIn session

    | LoginFailed err -> { model with LoginStatus = Failed err }, Cmd.none, NoOp

    | ClearLoginError -> { model with LoginStatus = NotStarted }, Cmd.none, NoOp

    | Submit ->
        let ofSuccess result =
            match result with
            | Ok session -> LoginCompleted session
            | Error err ->
                let msg =
                    match err with
                    | Login l ->
                        match l with
                        | UserDoesNotExist -> "Användaren saknas"
                        | PasswordIncorrect -> "Fel lösenord"
                    | _ -> GeneralErrorMsg
                msg |> LoginFailed


        match model.Email, model.Password with
        | (StringInput.Valid email, StringInput.Valid password) ->
            { model with LoginStatus = Running }, Cmd.OfAsync.either chickenCheckApi.CreateSession { Email = email; Password = password } ofSuccess (handleApiError LoginFailed), NoOp
        | _ -> failwith "Tried to submit invalid form"

open Fable.MaterialUI
module Mui = Fable.MaterialUI.Core
open Fable.React.Props
open Fable.MaterialUI.MaterialDesignIcons


let styles (theme : ITheme) : IStyles list =
    [
        Styles.Root [ 
            Height "100vh" 
        ]
        Styles.Custom("image", [
            BackgroundImage "url(https://chickencheck.z6.web.core.windows.net/Images/Signin1.jpg)"
            BackgroundRepeat "no-repeat" 
            BackgroundSize "cover"
            BackgroundPosition "center"
        ])
        Styles.Paper [
            Margin "64px 32px 64px 32px"
            Display DisplayOptions.Flex
            FlexDirection "column"
            AlignItems AlignItemsOptions.Center
        ]
        Styles.Avatar [
            Margin theme.spacing.unit
            BackgroundColor theme.palette.secondary.main
        ]
        Styles.Form [
            Width "100%"
            MarginTop theme.spacing.unit
        ]
        Styles.Error [
            BackgroundColor theme.palette.error.dark
        ]
        Styles.Wrapper [
            Margin theme.spacing.unit
            Position PositionOptions.Relative
        ]
        Styles.Progress [
            Color "green"
            CSSProp.Position PositionOptions.Absolute
            Top "50%"
            Left "50%"
            MarginTop "-12"
            MarginLeft "-12"
        ]
    ]

let private view' (classes: Mui.IClasses) (model : SigninModel) (dispatch : Msg -> unit) =

    let emailInput =
        let isValid, emailStr = model.Email |> StringInput.tryValid
        Mui.textField 
            [ OnChange (fun ev -> ev.Value |> ChangeEmail |> dispatch)
              TextFieldProp.Variant TextFieldVariant.Outlined 
              MaterialProp.Margin FormControlMargin.Normal
              Required true
              FullWidth true
              Id "email" 
              Label "Epost"
              Name "email"
              AutoComplete "email"
              AutoFocus true
              MaterialProp.Error (not isValid)
              Value emailStr 
              ] []

    let passwordInput =
        let isValid, pwStr = model.Password |> StringInput.tryValid
        Mui.textField
            [ OnChange (fun ev -> ev.Value |> ChangePassword |> dispatch)
              TextFieldProp.Variant TextFieldVariant.Outlined
              MaterialProp.Margin FormControlMargin.Normal
              Required true
              FullWidth true
              Name "password"
              Label "Lösenord"
              Type "password"
              Id "password"
              AutoComplete "current-password" 
              MaterialProp.Error (not isValid)
              Value pwStr 
              ] []
    
    let loading =
        Mui.circularProgress [ Class !!classes?progress 
                               CircularProgressProp.Size !^24 ] 

    let running = match model.LoginStatus with Running -> true | _ -> false
    let submitButton text =
        Mui.button              
            [ OnClick (fun ev -> ev.preventDefault(); Submit |> dispatch )
              Type "submit"
              FullWidth true
              ButtonProp.Variant ButtonVariant.Contained 
              MaterialProp.Color ComponentColor.Primary
              Disabled (model.IsInvalid || running)
              Class !!classes?submit ] 
            [ str text ]

    div [] 
        [ Mui.grid [ Container true
                     Class !!classes?root ] 
            [ Mui.cssBaseline [] 
              Mui.grid 
                  [ Item true
                    Sm !^GridSizeNum.``4``
                    Md !^GridSizeNum.``7``
                    Class !!classes?image ] 
                  [] 
              Mui.grid 
                  [ Item true
                    Xs !^GridSizeNum.``12``
                    Sm !^GridSizeNum.``8``
                    Md !^GridSizeNum.``5`` 
                    MaterialProp.Elevation 6 ] 
                  [ div 
                      [ Class !!classes?paper ] 
                      [ Mui.avatar 
                          [ Class !!classes?avatar ] 
                          [ lockOutlineIcon [] ]
                        Mui.typography [ Variant TypographyVariant.H5 ] 
                            [ str "Logga in" ]
                        form 
                            [ Class !!classes?form
                              NoValidate true ] 
                            [ emailInput 
                              passwordInput 
                              div [ Class !!classes?wrapper ]
                                [ yield submitButton "Logga in"
                                  if running then yield loading ] ] ] ] ] 
          ViewComponents.apiErrorMsg 
              (fun _ -> ClearLoginError |> dispatch) !!classes?error model.LoginStatus ]


// Workaround for using JSS with Elmish
// https://github.com/mvsmal/fable-material-ui/issues/4#issuecomment-423477900
type private IProps =
    abstract member Model: SigninModel with get, set
    abstract member Dispatch: (Msg -> unit) with get, set
    inherit Mui.IClassesProps

type private Component(p) =
    inherit PureStatelessComponent<IProps>(p)
    let viewFun (p: IProps) = view' p.classes p.Model p.Dispatch
    let viewWithStyles = Mui.withStyles (StyleType.Func styles) [] viewFun
    override this.render() = ReactElementType.create !!viewWithStyles this.props []

let view (model: SigninModel) (dispatch: Msg -> unit) : ReactElement =
    let props = jsOptions<IProps>(fun p ->
        p.Model <- model
        p.Dispatch <- dispatch)
    ofType<Component,_,_> props [] 