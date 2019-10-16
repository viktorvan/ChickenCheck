module Client

open Elmish
open Elmish.React
open Elmish.Navigation
open Elmish.UrlParser
open ChickenCheck.Client
open ChickenCheck.Client.Domain
open ChickenCheck.Domain
open Fulma
open Fable.React
open ChickenCheck.Client.Router
open CompositionRoot
open Fulma.Extensions.Wikiki


// defines the initial state and initial command (= side-effect) of the application
let private init (optRoute : Router.Route option) =
    let session = Session.tryGet()
    let model = 
        { CurrentRoute = None
          ActivePage = Page.Loading
          Navbar = Navbar.init()
          Session = session
        }

    match session with
    | Some _ ->
        Routing.setRoute optRoute model
    | None ->
        Routing.setRoute (SessionRoute.Signin |> Session |> Some) model


// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match msg, model.ActivePage with
    | SigninMsg msg, Page.Signin signinModel ->
        Signin.handle msg model signinModel

    | ChickenMsg msg, page -> 
        Chickens.handle msg model page

    | Signout, _ ->
        Signout.handle model

    | NavbarMsg msg, _ ->
        Navbar.handle msg model

    | msg, page -> 
        { model with ActivePage = Page.NotFound }, Cmd.none

let view model dispatch =
    let loadingPage =             
        PageLoader.pageLoader 
            [ 
                PageLoader.Color IsInfo
                PageLoader.IsActive true
            ] 
            [ ]

    let pageHtml (page : Page) =
        match page with
        | Page.Signin pageModel -> lazyView2 Signin.view pageModel (SigninMsg >> dispatch)
        | Page.Chickens pageModel -> lazyView2 Chickens.view pageModel (ChickenMsg >> dispatch)
        | Page.NotFound -> lazyView NotFound.view model
        | Page.Loading -> loadingPage

    let isLoggedIn, loggedInUsername =
        match model.Session with
        | None -> false, ""
        | Some session -> true, session.Name.Value

    div [] 
        [ if isLoggedIn then
              yield Navbar.view model.Navbar (NavbarMsg >> dispatch)
          yield div [] [ pageHtml model.ActivePage ] ] 


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
|> Program.withSubscription Authentication.handleExpiredToken
|> Program.toNavigable (parseHash Router.pageParser) Routing.setRoute
#if DEBUG
|> Program.withConsoleTrace
#endif

|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif

|> Program.run
