module Client

open ChickenCheck.Shared
open Elmish
open Elmish.React
open ChickenCheck.Client
open CompositionRoot
open Feliz
open Feliz.Bulma
open Feliz.Router
open Feliz.Bulma.PageLoader


// defines the initial state and initial command (= side-effect) of the application
let private init () =
    let session = SessionHandler.tryGet()
    let initialUrl = parseUrl (Router.currentUrl())
    let defaultState =
        { CurrentUrl = initialUrl
          CurrentPage = Page.Login (Session.init())
          Session = HasNotStartedYet
          IsMenuExpanded = false }
          
    match session with
    | Some session ->
        let defaultState = { defaultState with Session = Resolved session }
            
        match initialUrl with
        | Url.Chickens date -> 
            { defaultState with
                CurrentPage = Page.Chickens (Chickens.init date) }, GetAllChickens (Start date) |> ChickenMsg |> Cmd.ofMsg
        | Url.Login ->
            { defaultState with
                CurrentPage = Page.Login (Session.init()) }, Cmd.none
        | Url.Logout ->
            { defaultState with Session = HasNotStartedYet }, Router.navigate("login", HistoryMode.ReplaceState)
        | Url.NotFound ->
            { defaultState with CurrentPage = Page.NotFound }, Cmd.none
    | None ->
            defaultState, Router.navigate("login", HistoryMode.ReplaceState)

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match msg, model.CurrentPage with
    | UrlChanged nextUrl,_ ->
        let show page = { model with CurrentPage = page; CurrentUrl = nextUrl }
        
        match nextUrl with
        | Url.Chickens date -> show (Page.Chickens (Chickens.init date)), Cmd.none
        | Url.Login -> show (Page.Login (Session.init())), Cmd.none
        | Url.Logout -> { model with Session = HasNotStartedYet }, Router.navigate("/")
        | Url.NotFound -> show Page.NotFound, Cmd.none
        
    | SessionMsg msg, Page.Login pageModel -> 
        let (pageModel, cmds) = Session.update sessionCmds msg pageModel 
        { model with CurrentPage = Page.Login pageModel }, cmds

    | ChickenMsg msg, Page.Chickens pageModel ->
        model.Session
        |> Deferred.map (fun session ->
            let (pageModel, cmds) = Chickens.update session.Token chickenCmds msg pageModel
            { model with CurrentPage = Page.Chickens pageModel }, cmds)
        |> Deferred.defaultValue (model, Cmd.none)

    | ToggleMenu, _ -> { model with IsMenuExpanded = not model.IsMenuExpanded }, Cmd.none
        
    | LoggedIn session, _ ->
        SessionHandler.store session
        { model with Session = Deferred.Resolved session }, Router.navigate("/")

    | Logout, _ ->
        SessionHandler.delete()
        { model with Session = HasNotStartedYet }, Router.navigate("/")
        
    | ApiError _, _ ->
        failwith "not implemented"
        
    | LoginFailed _, _ -> 
        failwith "not implemented"
        
    | msg, page -> 
        sprintf "Unhandled msg: %A, page: %A" msg page |> Utils.Log.developmentError
        model, Cmd.none

let view (model: Model) dispatch =
    let activePage =
        match model.CurrentPage with
        | Page.Login pageModel -> lazyView2 Session.view pageModel dispatch 
        | Page.Chickens pageModel -> lazyView2 Chickens.view pageModel dispatch
        | Page.NotFound -> lazyView NotFound.view model

    let isLoggedIn = Deferred.resolved model.Session

    Router.router [
        Router.onUrlChanged (parseUrl >> UrlChanged >> dispatch)
        Router.application [
            Html.div [
                if isLoggedIn then lazyView2 Navbar.view model dispatch
                Html.div [ activePage ]
            ]
        ]
    ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
|> Program.withSubscription Authentication.handleExpiredToken
#if DEBUG
|> Program.withConsoleTrace
#endif

|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif

|> Program.run
