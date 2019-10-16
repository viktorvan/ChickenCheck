module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Client
open ChickenCheck.Domain
open Fable.Remoting.Client
open Elmish
open ChickenCheck.Client.ApiHelpers
open Fable.Core
open ChickenCheck.Client.Router
open ChickenCheck.Client.Domain


let private getToken session =
    match session with
    | Some session -> session.Token
    | None -> failwith "Cannot access api without token"

let private chickenApi : IChickenApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Api.routeBuilder
    #if !DEBUG
    |> Remoting.withBaseUrl "https://chickencheck-functions.azurewebsites.net"
    #endif
    |> Remoting.buildProxy<IChickenApi>

let private chickenCardApi session : ChickenCard.Api =
    let token = getToken session
    
    let addEgg =
        fun cmd ->
            callSecureApi
                token
                chickenApi.AddEgg
                cmd
                (fun _ -> ChickenCard.Msg.AddedEgg)
                ChickenCard.AddEggFailed

    let removeEgg =
        fun cmd ->
            callSecureApi
                token
                chickenApi.RemoveEgg
                cmd
                (fun _ -> ChickenCard.Msg.RemovedEgg)
                ChickenCard.RemoveEggFailed

    { AddEgg = addEgg
      RemoveEgg = removeEgg }

let private signinApi : Signin.Api =
    let ofSuccess result =
        match result with
        | Ok session -> Signin.Msg.LoginCompleted session
        | Error err ->
            let msg =
                match err with
                | Login l ->
                    match l with
                    | UserDoesNotExist -> "Användaren saknas"
                    | PasswordIncorrect -> "Fel lösenord"
                | _ -> GeneralErrorMsg
            msg |> Signin.Msg.AddError

    let createSession =
        fun cmd ->
            Cmd.OfAsync.either
                chickenApi.CreateSession
                cmd
                ofSuccess
                (handleApiError Signin.Msg.AddError)

    { CreateSession = createSession }


let private chickensApi session : Chickens.Api =
    let token = getToken session
    let getChickens =
        fun () ->
            callSecureApi
                token
                chickenApi.GetChickens 
                () 
                Chickens.FetchedChickens 
                Chickens.AddError 

    let getTotalCount =
        fun () -> 
            callSecureApi
                token
                chickenApi.GetTotalEggCount
                ()
                Chickens.FetchedTotalCount
                Chickens.AddError 

    let getCountOnDate =
        fun date -> 
            callSecureApi
                token
                chickenApi.GetEggCountOnDate 
                date 
                (fun res -> Chickens.FetchedEggCountOnDate (date, res)) 
                Chickens.AddError 
    

    { GetChickens = getChickens 
      GetTotalCount = getTotalCount
      GetCountOnDate = getCountOnDate }


module Routing =
    let setRoute (result: Option<Router.Route>) (model : Model) =
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
                    let (chickenModel, chickenCmd) = Chickens.init

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

module Signin =
    let handle msg (model: Model) signinModel =
        let (pageModel, result) = Signin.update signinApi msg signinModel
        match result with
        | Signin.External (Signin.ExternalMsg.SignedIn session) ->
            Session.store session
            { model with Session = Some session } 
            |> Routing.setRoute (Router.ChickenRoute.Chickens |> Router.Chicken |> Some)
        | Signin.Internal msg ->
            { model with ActivePage = pageModel |> Page.Signin }, Cmd.map SigninMsg msg

module Chickens =
    let handle msg model =
        match model.ActivePage with
        | Page.Chickens chickensPageModel ->
            let chickensApi = chickensApi model.Session
            let chickenCardApi = chickenCardApi model.Session
            let (pageModel, subMsg) = Chickens.update chickensApi chickenCardApi msg chickensPageModel
            { model with ActivePage = pageModel |> Page.Chickens }, Cmd.map ChickenMsg subMsg
        | _ -> model, Cmd.none

module Signout =
    let handle model =
        Session.delete()
        let signinModel = Signin.init()
        { model with
            Session = None
            ActivePage = Page.Signin signinModel
        }, SessionRoute.Signout |> Session |> newUrl

module Navbar =
    let handle msg model =
        let (navbarModel, extMsg) = Navbar.update msg model.Navbar
        let model = { model with Navbar = navbarModel }
        match extMsg with
        | Navbar.ExternalMsg.NoOp ->
            model, Cmd.none
        | Navbar.ExternalMsg.Signout ->
            model, Signout |> Cmd.ofMsg

module Authentication =
    let handleExpiredToken _ =
        let sub dispatch =
            Session.expired.Publish.Add
                (fun _ -> 
                    Signout |> dispatch)
        Cmd.ofSub sub


