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

let createSession query =

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

    Cmd.OfAsync.either
        chickenApi.Session
        query
        ofSuccess
        (handleApiError (SessionMsg.AddError >> SessionMsg))


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
let toCmd =
    let fromCmdMsgs session cmdMsgs =

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

        let toSingleCmd cmdMsg =
            match cmdMsg with
            | CmdMsg.ApiQuery query -> 
                callSecureApi
                    (getToken session)
                    chickenApi.Query
                    query
                    (queryResponseParser query)
                    (AddError >> ChickenMsg)

            | CmdMsg.ApiCommand cmd ->
                callSecureApi
                    (getToken session)
                    chickenApi.Command
                    cmd
                    (cmdResponseParser cmd)
                    (AddError >> ChickenMsg)

            | CmdMsg.SessionQuery query ->
                createSession query

            | Routing routing ->
                match routing with
                | NewRoute route -> Router.newUrl route

            | CmdMsg.Msg msg -> msg |> Cmd.ofMsg

            | CmdMsg.NoCmdMsg -> Cmd.none
            
        match cmdMsgs with
        | [] -> Cmd.none
        | [ cmd ] -> toSingleCmd cmd
        | cmds -> cmds |> List.map toSingleCmd |> Cmd.batch

    (fun (m: Model, cmds: CmdMsg list) -> m, (fromCmdMsgs m.Session cmds))

module Session =
    let handle msg (model: Model) =
        match model.ActivePage with
        | (Page.Signin sessionModel) ->
            let (pageModel, cmds) = Session.update msg sessionModel 
            { model with ActivePage = pageModel |> Page.Signin }, cmds
        | _ -> model, [ CmdMsg.NoCmdMsg ]
        
module Chickens =

    let handle (msg: ChickenMsg) model =
        match model.ActivePage with
        | Page.Chickens chickensPageModel ->
            let (pageModel, cmds) = Chickens.update msg chickensPageModel 
            { model with ActivePage = pageModel |> Page.Chickens }, cmds
        | _ -> model, [ CmdMsg.NoCmdMsg ]

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
            }, [ CmdMsg.NoCmdMsg ]

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
                    model, [ Session SessionRoute.Signin |> RoutingMsg.NewRoute |> Routing ] 

            | Router.Session s ->
                match s with
                | SessionRoute.Signin ->
                    let signinModel = Session.init()
                    { model with
                        ActivePage =
                            Page.Signin signinModel
                    }, [ CmdMsg.NoCmdMsg ]
                | SessionRoute.Signout -> 
                    model, [ Signout |> CmdMsg.Msg ]
