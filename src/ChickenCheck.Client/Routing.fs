module ChickenCheck.Client.Routing

open ChickenCheck.Client.Chickens
open Feliz.Router
open ChickenCheck.Shared
open Elmish
 
let handleUrlChange nextUrl model =
    
    let show page = { model with CurrentPage = page; CurrentUrl = nextUrl }
    let chickensPage date = show (Page.Chickens (ChickensPageModel.init date))
    let getAllChickens date = GetAllChickens (Start date) |> ChickenMsg |> Cmd.ofMsg
    
    match nextUrl with
    | Url.Home -> model, Router.navigate "/chickens"
    | Url.Chickens date -> chickensPage date, getAllChickens date
    | Url.NotFound -> show Page.NotFound, Cmd.none
    | Url.LogIn destination ->
        // auth log in
        match destination with
        | Some d ->
            model, Router.navigate d.Href
        | None ->
            model, Router.navigate "/chickens"
    | Url.LogOut ->
        // auth log out
            { model with User = Anonymous }, Router.navigate "/chickens"
        

