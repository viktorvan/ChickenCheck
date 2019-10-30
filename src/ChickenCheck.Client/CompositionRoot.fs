module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Client
open ChickenCheck.Domain
open ChickenCheck.Backend
open Fable.Remoting.Client
open Elmish
open ChickenCheck.Client.ApiHelpers
open Fable.Core
open ChickenCheck.Client.Router


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

let private signinApi : Session.Api =
    let ofSuccess result =
        match result with
        | Ok session -> SessionMsg.LoginCompleted session |> SessionMsg
        | Error err ->
            let msg =
                match err with
                | Login l ->
                    match l with
                    | UserDoesNotExist -> "Användaren saknas"
                    | PasswordIncorrect -> "Fel lösenord"
                | _ -> GeneralErrorMsg
            msg |> SessionMsg.AddError |> SessionMsg

    let createSession =
        fun cmd ->
            Cmd.OfAsync.either
                chickenApi.CreateSession
                cmd
                ofSuccess
                (handleApiError (SessionMsg.AddError >> SessionMsg))

    { CreateSession = createSession }


let private chickensApi session : Chickens.Api =
    let token = getToken session
    let getChickens =
        fun () ->
            callSecureApi
                token
                chickenApi.GetChickens 
                () 
                (FetchedChickens >> ChickenMsg)
                (AddError >> ChickenMsg)

    let getTotalCount =
        fun () -> 
            callSecureApi
                token
                chickenApi.GetTotalEggCount
                ()
                (FetchedTotalCount >> ChickenMsg)
                (AddError >> ChickenMsg) 

    let getCountOnDate =
        fun date -> 
            callSecureApi
                token
                chickenApi.GetEggCountOnDate 
                date 
                (fun res -> FetchedEggCountOnDate (date, res) |> ChickenMsg) 
                (AddError >> ChickenMsg) 
    
    let addEgg =
        fun cmd ->
            callSecureApi
                token
                chickenApi.AddEgg
                cmd
                (fun _ -> (cmd.ChickenId, cmd.Date) |> AddedEgg |> ChickenMsg)
                (fun err -> (cmd.ChickenId, err) |> AddEggFailed |> ChickenMsg)

    let removeEgg =
        fun cmd ->
            callSecureApi
                token
                chickenApi.RemoveEgg
                cmd
                (fun _ -> (cmd.ChickenId, cmd.Date) |> RemovedEgg |> ChickenMsg)
                (fun err -> (cmd.ChickenId, err) |> RemoveEggFailed |> ChickenMsg)

    { GetChickens = getChickens 
      GetTotalCount = getTotalCount
      GetCountOnDate = getCountOnDate 
      AddEgg = addEgg
      RemoveEgg = removeEgg }


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
                    }, chickenCmd

                | None ->
                    model, SessionRoute.Signin |> Session |> Router.newUrl

            | Router.Session s ->
                match s with
                | SessionRoute.Signin ->
                    let signinModel = Session.init()
                    { model with
                        ActivePage =
                            Page.Signin signinModel
                    }, Cmd.none
                | SessionRoute.Signout -> 
                    model, Signout |> Cmd.ofMsg

module Session =
    let handle msg (model: Model) =
        match model.ActivePage with
        | (Page.Signin sessionModel) ->
            let (pageModel, msg) = Session.update signinApi msg sessionModel
            { model with ActivePage = pageModel |> Page.Signin }, msg
        | _ -> model, Cmd.none

module Chickens =
    let handle (msg: ChickenMsg) model =
        match model.ActivePage with
        | Page.Chickens chickensPageModel ->
            let chickensApi = chickensApi model.Session
            let (pageModel, msg) = Chickens.update chickensApi msg chickensPageModel
            { model with ActivePage = pageModel |> Page.Chickens }, msg
        | _ -> model, Cmd.none

module Authentication =
    let handleExpiredToken _ =
        let sub dispatch =
            SessionHandler.expired.Publish.Add
                (fun _ -> Signout |> dispatch)
        Cmd.ofSub sub


