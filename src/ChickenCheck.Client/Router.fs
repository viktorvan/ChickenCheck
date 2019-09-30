module ChickenCheck.Client.Router

open Browser
open Fable.React.Props
open Elmish.Navigation
open Elmish.UrlParser


[<RequireQualifiedAccess>]
type ChickenRoute =
    | Chickens

type Route =
    | Chicken of ChickenRoute
    | Login

let pageParser: Parser<Route -> Route, Route> =
    oneOf
        [
            map (ChickenRoute.Chickens |> Chicken) top
            map (Login) (s "login" )
        ]


let signinPageUrl = "#signin"
let chickensPageUrl = "#"
let notFoundUrl = "#notfound"

let private toHash route =
    match route with
    | Login -> signinPageUrl
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
    window.location.href <- toHash route