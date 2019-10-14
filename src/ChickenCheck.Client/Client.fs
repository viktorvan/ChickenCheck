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

[<RequireQualifiedAccess>]
type Page =
    | Signin of Signin.Model
    | Chickens of Chickens.Model
    | Loading
    | NotFound

type Model =
    { CurrentRoute: Router.Route option
      Session: Session option
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
        match route with
        | Router.Chicken chickenRoute ->
            match model.Session with
            | Some session ->
                let (chickenModel, chickenCmd) = Chickens.init session chickenRoute

                { model with
                    ActivePage =
                        chickenModel
                        |> Page.Chickens
                }, Cmd.map ChickenMsg chickenCmd

            | None ->
                model, Router.Login |> Router.newUrl

        | Router.Login ->
            let signinModel = Signin.init ()
            { model with
                ActivePage =
                    Page.Signin signinModel
            }, Cmd.none

// defines the initial state and initial command (= side-effect) of the application
let private init (optRoute : Router.Route option) =
    match Session.tryGet () with
    | Some session ->
        {
            CurrentRoute = None
            ActivePage = Page.Loading
            Session = Some session
        }
        |> setRoute optRoute

    | None ->
        {
            CurrentRoute = None
            ActivePage = Page.Loading
            Session = None
        }
        |> setRoute (Some Router.Login)

let chickenCheckApi : IChickenCheckApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Api.routeBuilder
    #if !DEBUG
    |> Remoting.withBaseUrl "https://chickencheck-functions.azurewebsites.net"
    #endif
    |> Remoting.buildProxy<IChickenCheckApi>

let getToken model =
    match model.Session with
    | Some s -> s.Token
    | None -> failwith "this action requires an authenticated user"

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
let update (msg : Msg) (model : Model) : Model * Cmd<Msg> =
    let requestBuilder() =  
        match model.Session with
        | Some session -> SecureRequestBuilder(session.Token)
        | None -> failwith "Cannot request secure resource when not logged in"

    match msg, model.ActivePage with
    | SigninMsg msg, Page.Signin signinModel ->
        let (pageModel, subMsg, extraMsg) = Signin.update chickenCheckApi msg signinModel
        match extraMsg with
        | Signin.NoOp ->
            { model with ActivePage = pageModel |> Page.Signin }, Cmd.map SigninMsg subMsg
        | Signin.SignedIn session -> 
            Session.store session
            { model with Session = Some session } 
            |> setRoute (Router.ChickenRoute.Chickens |> Router.Chicken |> Some)

    | ChickenMsg msg, Page.Chickens chickensPageModel -> 
        let (pageModel, subMsg) = Chickens.update chickenCheckApi (requestBuilder()) msg chickensPageModel
        { model with ActivePage = pageModel |> Page.Chickens }, Cmd.map ChickenMsg subMsg

    | _ -> 
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

    let navbar =
        Navbar.navbar [ Navbar.Color IsInfo ]
            [ Navbar.Brand.div [ ]
                [ Navbar.Item.a [ Navbar.Item.Props [ Href "#" ] ]
                    [ img [ Style [ Width "2.5em" ] // Force svg display
                            Src "https://chickencheck.z6.web.core.windows.net/Icons/android-chrome-192x192.png" ] ]  
                  Navbar.Item.a [ Navbar.Item.Props [ Href "#" ] ]
                    [ str "Mina hönor" ] ] ]

    div [] 
        [ if isLoggedIn then
              yield navbar 
          yield div [] [ pageHtml model.ActivePage ] ] 

#if DEBUG

open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
|> Program.toNavigable (parseHash Router.pageParser) setRoute
#if DEBUG
|> Program.withConsoleTrace
#endif

|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif

|> Program.run
