module ChickenCheck.Client.Utils

open Fable.React

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
    let developmentError (error: exn) =
        if isDevelopment
        then Browser.Dom.console.error(error)


let inline elmishView name render = FunctionComponent.Of(render, name, equalsButFunctions)


