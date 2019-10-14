module ChickenCheck.Client.Router

open Browser
open Fable.React.Props
open Elmish.Navigation
open Elmish.UrlParser


[<RequireQualifiedAccess>]
type ChickenRoute =
    | Chickens

[<RequireQualifiedAccess>]
type SessionRoute =
    | Signin
    | Signout

type Route =
    | Chicken of ChickenRoute
    | Session of SessionRoute

let pageParser: Parser<Route -> Route, Route> =
    oneOf
        [
            map (ChickenRoute.Chickens |> Chicken) top
            map (SessionRoute.Signin |> Session) (s "session" </> s "signin")
            map (SessionRoute.Signout |> Session) (s "session" </> s "signout")
        ]


let signinPageUrl = "#session/signin"
let signoutPageUrl = "#session/signout"
let chickensPageUrl = "#"
let notFoundUrl = "#notfound"

let private toHash route =
    match route with
    | (Session s)-> 
        match s with
        | SessionRoute.Signin -> signinPageUrl
        | SessionRoute.Signout -> signoutPageUrl
    | Chicken c -> chickensPageUrl

let href route =
    route
    |> toHash

let modifyUrl route =
    route
    |> toHash
    |> Navigation.modifyUrl

let newUrl route =
    route
    |> toHash
    |> Navigation.newUrl

let modifyLocation route =
    printfn "setting route %A"route
    window.location.href <- toHash route