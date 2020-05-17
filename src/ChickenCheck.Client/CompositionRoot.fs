module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Client
open ChickenCheck.Domain
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
    |> Remoting.withRouteBuilder Route.builder
    #if !DEBUG
    |> Remoting.withBaseUrl "https://chickencheck-functions.azurewebsites.net"
    #endif
    |> Remoting.buildProxy<IChickenApi>

module CmdMsg =
    type private SuccessFunc<'T> = 'T -> Msg
    type private ErrorFunc = string -> Msg

    let toCmd =
        let toCmd' session cmdMsg =
            match cmdMsg with
            | CmdMsg.GetAllChickensWithEggs date ->
                callSecureApi
                    (getToken session)
                    chickenApi.GetAllChickensWithEggs
                    date
                    (FetchedChickensWithEggs >> ChickenMsg)
                    (AddError >> ChickenMsg)

            | CmdMsg.GetEggCountOnDate date ->
                callSecureApi
                    (getToken session)
                    chickenApi.GetEggCountOnDate
                    date
                    (fun count -> (date, count) |> FetchedEggCountOnDate |> ChickenMsg)
                    (AddError >> ChickenMsg)

            | CmdMsg.AddEgg (id, date) ->
                callSecureApi
                    (getToken session)
                    chickenApi.AddEgg
                    (id, date)
                    (fun () -> AddedEgg(id, date) |> ChickenMsg)
                    (fun str -> AddEggFailed(id, str) |> ChickenMsg)

            | CmdMsg.RemoveEgg (id, date) ->
                callSecureApi
                    (getToken session)
                    chickenApi.RemoveEgg
                    (id, date)
                    (fun () -> RemovedEgg(id, date) |> ChickenMsg)
                    (fun str -> RemoveEggFailed(id, str) |> ChickenMsg)

            | CmdMsg.CreateSession (email, pw) ->
                createSession chickenApi.CreateSession (email, pw)

            | OfNewRoute route -> Router.newUrl route

            | CmdMsg.OfMsg msg -> msg |> Cmd.ofMsg

            | CmdMsg.NoCmdMsg -> Cmd.none

        let fromCmdMsgs session cmdMsgs =
            match cmdMsgs with
            | [] -> Cmd.none
//            | [ cmdMsg ] -> cmdMsg |> toCmd' session
            | cmdMsgs -> cmdMsgs |> List.map (toCmd' session) |> Cmd.batch

        fun (model: Model, cmds: CmdMsg list) -> model, (fromCmdMsgs model.Session cmds)


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
            | Router.Chicken _ ->
                match model.Session with
                | Some session ->
                    let (chickenModel, chickenCmd) = Chickens.init 

                    { model with
                        ActivePage =
                            chickenModel
                            |> Page.Chickens
                    }, chickenCmd

                | None ->
                    model, [ Session SessionRoute.Signin |> OfNewRoute ] 

            | Router.Session s ->
                match s with
                | SessionRoute.Signin ->
                    let signinModel = Session.init()
                    { model with
                        ActivePage =
                            Page.Signin signinModel
                    }, [ CmdMsg.NoCmdMsg ]
                | SessionRoute.Signout -> 
                    model, [ Signout |> CmdMsg.OfMsg ]
