module ChickenCheck.Client.Session.Signin
open Elmish
open ChickenCheck.Client
open ChickenCheck.Client.Pages
open ChickenCheck.Domain
open ChickenCheck.Domain.Session
open Fable.React
open Fable.Core.JsInterop
open ChickenCheck.Client.ApiHelpers
open Fable.FontAwesome

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

open Fable.React.Props
open Fulma


let view (model : SigninModel) (dispatch : Msg -> unit) =

    let emailInput =
        let isValid, emailStr = model.Email |> StringInput.tryValid
        Field.div []
            [ Label.label [] [ str "Email" ]
              Control.div [ Control.HasIconLeft; Control.HasIconRight ] 
                  [ yield Input.email [ Input.Value emailStr
                                        Input.Placeholder "Email" 
                                        Input.OnChange (fun ev -> ev.Value |> ChangeEmail |> dispatch) 
                                        Input.Props [ Required true ] ] 
                    yield Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ] 
                        [ Fa.i [ Fa.Solid.Envelope ] [] ] 
                    if not isValid then yield Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                                                        [ Fa.i [ Fa.Solid.ExclamationTriangle] [] ]
                    if not isValid then yield Help.help [ Help.Color IsDanger ] [ str "Ogiltig epostadress"] ]]

    let passwordInput =
        let isValid, pwStr = model.Password |> StringInput.tryValid
        Field.div []
            [ Label.label [] [ str "Lösenord" ]
              Control.div [ Control.HasIconLeft; Control.HasIconRight ] 
                  [ yield Input.password [ Input.Value pwStr
                                           Input.Placeholder "Lösenord" 
                                           Input.OnChange (fun ev -> ev.Value |> ChangePassword |> dispatch) 
                                           Input.Props [ Required true ] ] 
                    yield Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ] 
                        [ Fa.i [ Fa.Solid.Key ] [] ] 
                    if not isValid then yield Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                                                        [ Fa.i [ Fa.Solid.ExclamationTriangle] [] ]
                    if not isValid then yield Help.help [ Help.Color IsDanger ] [ str "Ange ett lösenord"] ]]
    
    let running = match model.LoginStatus with Running -> true | _ -> false

    let submitButton text =
        Button.button              
            [ Button.OnClick (fun ev -> ev.preventDefault(); Submit |> dispatch )
              Button.Disabled (model.IsInvalid || running) ] 
            [ str text ]

    Hero.hero
        [ Hero.IsFullHeight
          Hero.CustomClass "login-img" ] 
        [ Hero.body []
            [ Container.container
                []
                [ Column.column [ Column.Width (Screen.All, Column.Is6); Column.Offset (Screen.All, Column.Is6)]
                    [ form [ ] 
                        [ Box.box' [] 
                            [ Field.div [] 
                                [ emailInput 
                                  passwordInput
                                  submitButton "Logga in" ] ] ] ] ] 
                    ]
                ]
        
