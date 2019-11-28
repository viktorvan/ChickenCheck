module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Client
open ChickenCheck.Domain
open ChickenCheck.Backend
open Fable.Remoting.Client
open Elmish
open ChickenCheck.Client.ApiHelpers
open Fable.Core
open ChickenCheck.Client.Router


module Tuple =
    let mapSnd f (fst, snd) = fst, f snd

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

module Api =
    let executeQuery query onSuccess onError =
        fun token ->
            callSecureApi
                token
                chickenApi.Query
                query
                onSuccess
                onError

    let executeCommand cmd onSuccess onError =
        fun token ->
            callSecureApi
                token
                chickenApi.Command
                cmd
                onSuccess
                onError

module Session =
    let handle msg (model: Model) =
        match model.ActivePage with
        | (Page.Signin sessionModel) ->
            let (pageModel, msg) = Session.update signinApi msg sessionModel
            { model with ActivePage = pageModel |> Page.Signin }, msg
        | _ -> model, Cmd.none
        
module Chickens =
    let queryResponseParser query = 

        fun res ->
            match query, res with
            | Queries.AllChickens, Queries.Response.Chickens c -> 
                c |> FetchedChickens |> ChickenMsg
            | Queries.EggCountOnDate _, Queries.Response.EggCountOnDate (date, count) -> 
                (date, count) |> FetchedEggCountOnDate |> ChickenMsg
            | Queries.TotalEggCount _, Queries.Response.TotalEggCount count -> 
                count |> FetchedTotalCount |> ChickenMsg
            | _ -> 
                notImplemented()

    let cmdResponseParser (cmd: Commands.DomainCommand) =
        match cmd with
        | Commands.AddEgg c -> (fun _ -> AddedEgg (c.ChickenId, c.Date))
        | Commands.RemoveEgg c -> (fun _ -> RemovedEgg (c.ChickenId, c.Date))
        >> ChickenMsg

    let toCmd token cmdMsgs =
        let toSingleCmd cmdMsg =
            match cmdMsg with
            | ChickenCmdMsg.ApiQuery query -> 
                callSecureApi
                    token
                    chickenApi.Query
                    query
                    (queryResponseParser query)
                    (AddError >> ChickenMsg)

            | ChickenCmdMsg.ApiCommand cmd ->
                callSecureApi
                    token
                    chickenApi.Command
                    cmd
                    (cmdResponseParser cmd)
                    (AddError >> ChickenMsg)

            | ChickenCmdMsg.Msg msg -> msg |> ChickenMsg |> Cmd.ofMsg

            | ChickenCmdMsg.NoCmdMsg -> Cmd.none
            
        match cmdMsgs with
        | [] -> Cmd.none
        | [ cmd ] -> toSingleCmd cmd
        | cmds -> cmds |> List.map toSingleCmd |> Cmd.batch

    let handle (msg: ChickenMsg) model =
        let token = getToken model.Session 
        
        match model.ActivePage with
        | Page.Chickens chickensPageModel ->
            let (pageModel, cmds) = Chickens.update msg chickensPageModel |> Tuple.mapSnd (toCmd token)
            { model with ActivePage = pageModel |> Page.Chickens }, cmds
        | _ -> model, Cmd.none

module Authentication =
    let handleExpiredToken _ =
        let sub dispatch =
            SessionHandler.expired.Publish.Add
                (fun _ -> Signout |> dispatch)
        Cmd.ofSub sub

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
                    let (chickenModel, chickenCmd) = Chickens.init |> Tuple.mapSnd (Chickens.toCmd session.Token)

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
