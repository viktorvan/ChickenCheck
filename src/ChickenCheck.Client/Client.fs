module Client

open Elmish
open Elmish.React
open Elmish.Navigation
open Elmish.UrlParser
open ChickenCheck.Client
open Messages
open Fable.Remoting.Client
open ChickenCheck.Domain
open Fable.Core
open Elmish.Navigation
open Fulma
open Fable.React
open Fable.React.Props
open ChickenCheck.Client.Router

[<RequireQualifiedAccess>]
type Page =
    | Signin of Signin.Model
    | Chickens of Chickens.Model
    | Loading
    | NotFound

type Model =
    { CurrentRoute: Router.Route option
      Session: Session option
      Navbar: Navbar.Model
      ActivePage: Page }

let private setRoute (result: Option<Router.Route>) (model : Model) =
    let model = { model with CurrentRoute = result }
    match result with
    | None ->
        let requestedUrl = Browser.Dom.window.location.href

        JS.console.error("Error parsing url: " + requestedUrl)

        { model with
            ActivePage = Page.NotFound
        }, Cmd.none

    | Some route ->
        Router.modifyLocation route
        match route with
        | Router.Chicken chickenRoute ->
            match model.Session with
            | Some session ->
                let (chickenModel, chickenCmd) = Chickens.init chickenRoute

                { model with
                    ActivePage =
                        chickenModel
                        |> Page.Chickens
                }, Cmd.map ChickenMsg chickenCmd

            | None ->
                model, SessionRoute.Signin |> Session |> Router.newUrl

        | Router.Session s ->
            match s with
            | SessionRoute.Signin ->
                let signinModel = Signin.init()
                { model with
                    ActivePage =
                        Page.Signin signinModel
                }, Cmd.none
            | SessionRoute.Signout -> 
                model, Signout |> Cmd.ofMsg

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
        setRoute optRoute model
    | None ->
        setRoute (SessionRoute.Signin |> Session |> Some) model

let chickenApi : IChickenApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Api.routeBuilder
    #if !DEBUG
    |> Remoting.withBaseUrl "https://chickencheck-functions.azurewebsites.net"
    #endif
    |> Remoting.buildProxy<IChickenApi>


let getToken model =
    match model.Session with
    | Some s -> s.Token
    | None -> failwith "this action requires an authenticated user"

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    match msg, model.ActivePage with
    | SigninMsg msg, Page.Signin signinModel ->
            let (pageModel, subMsg, extraMsg) = Signin.update chickenApi msg signinModel
            match extraMsg with
            | Signin.NoOp ->
                { model with ActivePage = pageModel |> Page.Signin }, Cmd.map SigninMsg subMsg
            | Signin.SignedIn session -> 
                Session.store session
                { model with Session = Some session } 
                |> setRoute (Router.ChickenRoute.Chickens |> Router.Chicken |> Some)

    | ChickenMsg msg, page -> 
        match page with
        | Page.Chickens chickensPageModel ->
            let apiToken =
                match model.Session with
                | Some session -> session.Token
                | None -> failwith "Cannot request secure page without session"
            let (pageModel, subMsg) = Chickens.update chickenApi apiToken msg chickensPageModel
            { model with ActivePage = pageModel |> Page.Chickens }, Cmd.map ChickenMsg subMsg
        | _ -> model, Cmd.none

    | Signout, _ ->
        Session.delete()
        let signinModel = Signin.init()
        { model with
            Session = None
            ActivePage = Page.Signin signinModel
        }, SessionRoute.Signout |> Session |> newUrl

    | NavbarMsg msg, _ ->
        let (navbarModel, extMsg) = Navbar.update msg model.Navbar
        let model = { model with Navbar = navbarModel }
        match extMsg with
        | Navbar.ExternalMsg.NoOp ->
            model, Cmd.none
        | Navbar.ExternalMsg.Signout ->
            model, Signout |> Cmd.ofMsg

    | msg, page -> 
        { model with ActivePage = Page.NotFound }, Cmd.none


let view model dispatch =

    let pageHtml (page : Page) =
        match page with
        | Page.Signin pageModel -> lazyView2 Signin.view pageModel (SigninMsg >> dispatch)
        | Page.Chickens pageModel -> lazyView2 Chickens.view pageModel (ChickenMsg >> dispatch)
        | Page.NotFound -> lazyView NotFound.view model
        | Page.Loading -> failwith "Not Implemented"

    let isLoggedIn, loggedInUsername =
        match model.Session with
        | None -> false, ""
        | Some session -> true, session.Name.Value

    div [] 
        [ if isLoggedIn then
              yield Navbar.view model.Navbar (NavbarMsg >> dispatch)
          yield div [] [ pageHtml model.ActivePage ] ] 


let handleExpiredToken _ =
    let sub dispatch =
        Session.expired.Publish.Add
            (fun _ -> 
                Signout |> dispatch)
    Cmd.ofSub sub


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
|> Program.withSubscription handleExpiredToken
|> Program.toNavigable (parseHash Router.pageParser) setRoute
#if DEBUG
|> Program.withConsoleTrace
#endif

|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif

|> Program.run
