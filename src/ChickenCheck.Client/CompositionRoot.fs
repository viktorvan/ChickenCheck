module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Client
open ChickenCheck.Client.Chickens
open ChickenCheck.Shared
open Fable.Remoting.Client

let private chickenApi : IChickenApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IChickenApi>
    
let private chickenEditApi : IChickenEditApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IChickenEditApi>

let chickenCmds = ChickenApiCmds(chickenApi, chickenEditApi)

let parseUrl urlString = 
    match urlString with
    | [] -> Url.Home
    | [ "chickens" ] -> Url.Chickens NotFutureDate.today
//    | [ "chickens"; Date ]
    | [ "login" ] -> Url.LogIn None
    | [ "logout" ] -> Url.LogOut
    | _ -> Url.NotFound
