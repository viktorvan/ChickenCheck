module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Client
open ChickenCheck.Client.ApiCommands
open ChickenCheck.Shared
open Fable.Remoting.Client
open Elmish


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

let parseUrl urlString = 
    let home = Url.Chickens NotFutureDate.today
    match urlString with
    | [] -> home
    | [ "login" ] -> Url.Login
    | [ "logout" ] -> Url.Logout
    | [ "chickens" ] -> home
//    | [ "chickens"; Date ]
    | _ -> Url.NotFound

module Authentication =
    let handleExpiredToken _ =
        let sub dispatch =
            SessionHandler.expired.Publish.Add
                (fun _ -> Logout |> dispatch)
        Cmd.ofSub sub

