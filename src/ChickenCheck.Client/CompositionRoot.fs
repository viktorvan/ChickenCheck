module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Client
open ChickenCheck.Client.ApiCommands
open ChickenCheck.Domain
open Fable.Remoting.Client
open Elmish
open Fable.Core
open ChickenCheck.Client.Router


let private getToken session =
    match session with
    | Resolved session -> session.Token
    | HasNotStartedYet | InProgress ->
        failwith "Cannot access api without token"

let private chickenApi : IChickenApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    #if !DEBUG
    |> Remoting.withBaseUrl "https://chickencheck-functions.azurewebsites.net"
    #endif
    |> Remoting.buildProxy<IChickenApi>

let sessionCmds = SessionApiCmds(chickenApi)
let chickenCmds = ChickenApiCmds(chickenApi)

module Session =
    let handle msg (model: Model) =
        match model.ActivePage with
        | (Page.Signin sessionModel) ->
            let (pageModel, cmds) = Session.update sessionCmds msg sessionModel 
            { model with ActivePage = pageModel |> Page.Signin }, cmds
        | _ -> model, Cmd.none
        
module Chickens =
    let handle (msg: ChickenMsg) model =
        match model.ActivePage with
        | Page.Chickens chickensPageModel ->
            match model.Session with
            | Resolved session ->
                let (pageModel, cmds) = Chickens.update session.Token chickenCmds msg chickensPageModel 
                { model with ActivePage = pageModel |> Page.Chickens }, cmds
            | HasNotStartedYet | InProgress -> model, Cmd.none
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
            | Router.Chicken _ ->
                match model.Session with
                | Resolved session ->
                    let (chickenModel, chickenCmd) = Chickens.init session.Token chickenCmds

                    { model with
                        ActivePage =
                            chickenModel
                            |> Page.Chickens
                    }, chickenCmd

                | HasNotStartedYet | InProgress ->
                    model, Session SessionRoute.Signin |> Router.newUrl  

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
