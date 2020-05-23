module ChickenCheck.Client.Session

open Elmish
open Fable.React
open ChickenCheck.Domain
open Fable.FontAwesome
open FsToolkit.ErrorHandling
open ChickenCheck.Client

type SessionPageModel with
    member __.IsValid =
        match __.Email, __.Password with
        | StringInput.Valid _, StringInput.Valid _ -> true
        | _ -> false
    member __.IsInvalid = __.IsValid |> not

let init() =
    { Email = StringInput.Empty
      Password = StringInput.Empty
      LoginStatus = HasNotStartedYet
      Errors = [] }

let private toMsg = SessionMsg >> Cmd.ofMsg

let inline set raw value =
    match value with
    | Ok e -> StringInput.Valid e
    | Error _ -> StringInput.Invalid raw

let update (api: ISessionApiCmds) (msg: SessionMsg) (model: SessionPageModel) =
    match msg with
    | ChangeEmail msg ->
        let newEmail = StringInput.create Email.create msg
        { model with Email = newEmail }, Cmd.none

    | ChangePassword msg ->
        let newPassword = StringInput.create Password.create msg
        { model with Password = newPassword }, Cmd.none

    | SessionMsg.ClearErrors -> 
        { model with Errors = [] }, Cmd.none

    | SignIn (Start ()) ->
        match model.Email, model.Password with
        | (StringInput.Valid email, StringInput.Valid password) ->
            model, api.Login(email, password) 

        | _ -> failwith "Application error, tried to submit invalid form"
    
    | SignIn (Finished loginError) ->
        { model with Errors = "Misslyckades att logga in" :: model.Errors }, Cmd.none

        
open Fable.React.Props
open Fulma


type SessionProps =
    { Model : SessionPageModel
      Dispatch : Dispatch<Msg> }
let view = Utils.elmishView "Session" (fun (props: SessionProps) ->
    let model = props.Model
    let dispatch = props.Dispatch

    let emailInput =
        let isValid, emailStr = model.Email |> StringInput.tryValid
        Field.div []
            [ Label.label [] [ str "Email" ]
              Control.div 
                  [ Control.HasIconLeft; Control.HasIconRight ] 
                  [ yield Input.email [ Input.Value emailStr
                                        Input.Placeholder "Email" 
                                        Input.OnChange (fun ev -> ev.Value |> ChangeEmail |> SessionMsg |> dispatch) 
                                        Input.Props [ Required true ] ] 
                    yield Icon.icon 
                        [ Icon.Size IsSmall; Icon.IsLeft ] 
                        [ Fa.i [ Fa.Solid.Envelope ] [] ] 
                    if not isValid then yield Icon.icon [ Icon.Size IsSmall; Icon.IsRight ] 
                                                        [ Fa.i [ Fa.Solid.ExclamationTriangle] [] ]
                    if not isValid then yield Help.help [ Help.Color IsDanger ] [ str "Ogiltig epostadress"] ]]

    let passwordInput =
        let isValid, pwStr = model.Password |> StringInput.tryValid
        Field.div []
            [ Label.label [] [ str "Lösenord" ]
              Control.div 
                  [ Control.HasIconLeft; Control.HasIconRight ] 
                  [ yield Input.password [ Input.Value pwStr
                                           Input.Placeholder "Lösenord" 
                                           Input.OnChange (fun ev -> ev.Value |> ChangePassword |> SessionMsg |> dispatch) 
                                           Input.Props [ Required true ] ] 
                    yield Icon.icon 
                        [ Icon.Size IsSmall; Icon.IsLeft ] 
                        [ Fa.i [ Fa.Solid.Key ] [] ] 
                    if not isValid then yield Icon.icon [ Icon.Size IsSmall; Icon.IsRight ]
                                                        [ Fa.i [ Fa.Solid.ExclamationTriangle] [] ]
                    if not isValid then yield Help.help [ Help.Color IsDanger ] [ str "Ange ett lösenord"] ]]
    
    let running = Deferred.inProgress model.LoginStatus

    let submitButton text =
        Button.button              
            [ Button.OnClick (fun ev -> ev.preventDefault(); (SignIn (AsyncOperationStatus.Start())) |> SessionMsg |> dispatch )
              Button.Disabled (model.IsInvalid || running) ] 
            [ str text ]

    let errorView =
        let errorFor item = 
            item
            |> ViewComponents.apiErrorMsg (fun _ -> SessionMsg.ClearErrors |> SessionMsg |> dispatch) 

        model.Errors |> List.map errorFor

    let hasErrors = model.Errors |> List.isEmpty |> not

    Hero.hero
        [ Hero.IsFullHeight
          Hero.CustomClass "login-img" ] 
        [ Hero.body []
            [ Container.container
                []
                [ 
                    yield Column.column 
                        [ Column.Width (Screen.All, Column.Is6); Column.Offset (Screen.All, Column.Is6)]
                        [ 
                            form [] 
                                [ Box.box' [] 
                                    [ Field.div [] 
                                        [ emailInput 
                                          passwordInput
                                          submitButton "Logga in" ] ] ] 
                        ] 
                    if hasErrors then yield Section.section [] errorView
                ] 
            ]
        ]
        )
        
