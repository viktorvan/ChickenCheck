module Client

open Elmish
open Elmish.React
open Elmish.Navigation
open Elmish.UrlParser
open ChickenCheck.Client
open Fulma
open Fable.React
open ChickenCheck.Client.Router
open CompositionRoot
open Fulma.Extensions.Wikiki


// defines the initial state and initial command (= side-effect) of the application
let private init (optRoute : Router.Route option) =
    let session = SessionHandler.tryGet() 
    let model = 
        { CurrentRoute = None
          ActivePage = Page.Loading
          IsMenuExpanded = false
          Session = session |> Option.map Resolved |> Option.defaultValue HasNotStartedYet }

    match session with
    | Some _ -> Routing.setRoute optRoute model
    | None -> Routing.setRoute (SessionRoute.Signin |> Session |> Some) model

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match msg with
    | SessionMsg msg -> Session.handle msg model 

    | ChickenMsg msg -> Chickens.handle msg model 

    | ToggleMenu -> { model with IsMenuExpanded = not model.IsMenuExpanded }, Cmd.none
        
    | SignedIn session ->
        SessionHandler.store session
        { model with 
            Session = Deferred.Resolved session
            ActivePage = Session.init() |> Page.Signin }
        |> Routing.setRoute (Router.ChickenRoute.Chickens |> Router.Chicken |> Some) 

    | Signout ->
        SessionHandler.delete()
        { model with
            Session = HasNotStartedYet
            ActivePage = Session.init() |> Page.Signin }, 
        Router.SessionRoute.Signout |> Router.Session |> Router.newUrl
        
    | ApiError _ ->
        failwith "not implemented"
        
    | LoginFailed _ -> 
        failwith "not implemented"

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
        | Page.Signin pageModel -> Session.view { Model = pageModel; Dispatch = dispatch }
        | Page.Chickens pageModel -> Chickens.view { Model = pageModel; Dispatch = dispatch }
        | Page.NotFound -> lazyView NotFound.view model
        | Page.Loading -> loadingPage

    let isLoggedIn = Deferred.resolved model.Session

    div [] 
        [ 
          if isLoggedIn then
              yield Navbar.view { Model = model; Dispatch = dispatch }
          yield div [] [ pageHtml model.ActivePage ] ] 


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

let setRoute route model = Routing.setRoute route model

Program.mkProgram init update view
|> Program.withSubscription Authentication.handleExpiredToken
|> Program.toNavigable (parseHash Router.pageParser) setRoute
#if DEBUG
|> Program.withConsoleTrace
#endif

|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif

|> Program.run
