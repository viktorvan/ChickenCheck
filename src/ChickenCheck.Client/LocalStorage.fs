module ChickenCheck.Client.LocalStorage

open Fable.Core
open Browser.WebStorage

let load<'T> key =
    localStorage.getItem(key) |> unbox
    |> Option.map (JS.JSON.parse >> unbox<'T>)

let save key (data: 'T) =
    localStorage.setItem(key, JS.JSON.stringify data)

let delete key =
    localStorage.removeItem(key)
