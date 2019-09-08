module Client

open Elmish
open Elmish.React
open Elmish.Navigation
open Elmish.UrlParser
open ChickenCheck.Client
open Messages
open Fable.Remoting.Client
open ChickenCheck.Client.Pages
open ChickenCheck.Domain
open ChickenCheck.Domain.Session

let urlUpdate (result : Option<Page>) model =
    match model.Session, result with
    | None, _ -> { model with CurrentPage = SigninPage.init }, Cmd.none

    | _, None ->
        // Fable.Import.Browser.console.error ("Error parsing url: " + Fable.Import.Browser.window.location.href)
        model, model.CurrentPage |> modifyUrl

    | _, Some page -> { model with CurrentPage = page }, Cmd.none

// defines the initial state and initial command (= side-effect) of the application
let init result : Model * Cmd<Msg> =
    let model =
        { Session = None
          CurrentPage = ChickensPage.init }
    urlUpdate result model

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

    match msg, model.CurrentPage with
    | SigninMsg msg, SigninPage signinModel ->
        let (pageModel, subMsg, extraMsg) = Session.Signin.update chickenCheckApi msg signinModel
        let newModel =
            match extraMsg with
            | Session.Signin.NoOp -> model
            | Session.Signin.SignedIn session -> { model with Session = Some session }
        { newModel with CurrentPage = pageModel |> SigninPage }, Cmd.map SigninMsg subMsg

    | ChickenMsg (IndexMsg msg), ChickensPage chickensPageModel -> 
        let (pageModel, subMsg) = Chicken.Index.update chickenCheckApi (requestBuilder()) msg chickensPageModel
        { model with CurrentPage = pageModel |> ChickensPage }, Cmd.map (IndexMsg >> ChickenMsg) subMsg

    | GoToChickens, _ ->
        let page = ChickensPage.init
        { model with CurrentPage = page }, page |> newUrl 

    | _ -> notImplemented()


#if DEBUG

open Elmish.Debug
open Elmish.HMR
#endif

let pageParser : Parser<Page -> Page, Page> =
    oneOf [ map (ChickensPage.init) top
            map (SigninPage.init) (s "signin") ] 

Program.mkProgram init update View.view
|> Program.toNavigable (parseHash pageParser) urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif

|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif

|> Program.run
