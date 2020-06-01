module ChickenCheck.Client.Session

open Elmish
open ChickenCheck.Domain
open Feliz
open Feliz.Bulma

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
            { model with LoginStatus = InProgress }, api.Login(email, password) 

        | _ -> failwith "Application error, tried to submit invalid form"
    
    | SignIn (Finished loginError) ->
        { model with
            LoginStatus = Resolved loginError
            Errors = "Misslyckades att logga in" :: model.Errors }, Cmd.none

type SessionProps =
    { Model : SessionPageModel
      Dispatch : Dispatch<Msg> }
let view = Utils.elmishView "Session" (fun (props: SessionProps) ->
    let model = props.Model
    let dispatch = props.Dispatch

    let emailInput =
        let isValid, emailStr = model.Email |> StringInput.tryValid
        Bulma.field.div [
            Bulma.label "Email"
            Bulma.control.div [
                control.hasIconsLeft
                control.hasIconsRight
                prop.children [
                    Bulma.input.email [
                        prop.valueOrDefault emailStr
                        prop.placeholder "Email"
                        prop.onChange (fun value -> value |> ChangeEmail |> SessionMsg |> dispatch)
                        prop.required true
                    ]
                    Bulma.icon [
                        icon.isSmall
                        icon.isLeft
                        prop.children [
                            Html.i [
                                prop.className "fas fa-envelope"
                            ]
                        ]
                    ]
                    if not isValid then Bulma.icon [
                        icon.isSmall
                        icon.isRight
                        prop.children [
                            Html.i [
                                prop.className "fas fa-exclamation-triangle"
                            ]
                        ]
                    ]
                    if not isValid then Bulma.help [
                        color.isDanger
                        prop.text "Ogiltig epostadress"
                    ]
                ]
            ]
        ]

    let passwordInput =
        let isValid, pwStr = model.Password |> StringInput.tryValid
        Bulma.field.div [
            Bulma.label "Lösenord"
            Bulma.control.div [
                control.hasIconsLeft
                control.hasIconsRight
                prop.children [
                    Bulma.input.password [
                        prop.valueOrDefault pwStr
                        prop.placeholder "Lösenord"
                        prop.onChange (fun value -> value |> ChangePassword |> SessionMsg |> dispatch)
                        prop.required true
                    ]
                    Bulma.icon [
                        icon.isSmall
                        icon.isLeft
                        prop.children [
                            Html.i [
                                prop.className "fas fa-key"
                            ]
                        ]
                    ]
                    if not isValid then Bulma.icon [
                        icon.isSmall
                        icon.isRight
                        prop.children [
                            Html.i [
                                prop.className "fas fa-exclamation-triangle"
                            ]
                        ]
                    ]
                    if not isValid then Bulma.help [
                        color.isDanger
                        prop.text "Ange ett lösenord"
                    ]
                ]
            ]
        ]

    let running = Deferred.resolved model.LoginStatus

    let submitButton (text: string) =
        Bulma.button.button [
            prop.onClick (fun ev -> ev.preventDefault(); (Start()) |> SignIn |> SessionMsg |> dispatch)
            prop.disabled (model.IsInvalid || running)
            prop.text text
        ]

    let errorView =
        let errorFor item =
            item
            |> SharedViews.apiErrorMsg (fun _ -> SessionMsg.ClearErrors |> SessionMsg |> dispatch)

        model.Errors |> List.map errorFor

    let hasErrors = model.Errors |> List.isEmpty |> not

    Bulma.hero [
        hero.isFullHeight
        prop.children (
            Bulma.heroBody [
                Bulma.container [
                    Bulma.column [
                        column.is6
                        column.isOffset6
                        prop.children [
                            Html.form [
                                Bulma.box [
                                    Bulma.field.div [
                                        prop.children [
                                            emailInput
                                            passwordInput
                                            submitButton "Logga in"
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    if hasErrors then Bulma.section errorView
                ]
            ]
        )
    ]
)


