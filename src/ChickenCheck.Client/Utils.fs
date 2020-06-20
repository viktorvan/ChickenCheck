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

open Fable.Core.JsInterop

[<Emit("typeof $0 === 'function'")>]
let private isFunction (x: obj): bool = jsNative

[<Emit("typeof $0 === 'object' && !$0[Symbol.iterator]")>]
let private isNonEnumerableObject (x: obj): bool = jsNative

let equalsButFunctions (x: 'a) (y: 'a) =
    if obj.ReferenceEquals(x, y) then
        true
    elif isNonEnumerableObject x && not(isNull(box y)) then
        let keys = JS.Constructors.Object.keys x
        let length = keys.Count
        let mutable i = 0
        let mutable result = true
        while i < length && result do
            let key = keys.[i]
            i <- i + 1
            let xValue = x?(key)
            result <- isFunction xValue || xValue = y?(key)
        result
    else
        (box x) = (box y)
        

open Feliz
let memo (name: string) (render: 'props -> ReactElement) =
    Feliz.React.memo(name, render, areEqual = equalsButFunctions)
    
let memoWithKey (name: string) (render: 'props -> ReactElement) (withKey: 'props -> string) =
    Feliz.React.memo(name, render, withKey = withKey, areEqual = equalsButFunctions)
