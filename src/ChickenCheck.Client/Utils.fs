module ChickenCheck.Client.Utils

let private isDevelopment =
    #if DEBUG
    true
    #else
    false
    #endif

module Log =
    let developmentMessage str =
        if isDevelopment
        then Browser.Dom.console.log(str)
    let developmentError str =
        if isDevelopment
        then Browser.Dom.console.error(str)
    let developmentException (error: exn) =
        if isDevelopment
        then Browser.Dom.console.error(error)

open Fable.Core
open Browser.WebStorage
module LocalStorage =
    let load<'T> key =
        localStorage.getItem(key) |> unbox
        |> Option.map (JS.JSON.parse >> unbox<'T>)
    
    let save key (data: 'T) =
        localStorage.setItem(key, JS.JSON.stringify data)
    
    let delete key =
        localStorage.removeItem(key)
        
let inline elmishView<'T> (name: string) render = Feliz.React.functionComponent<'T>(name, render)
