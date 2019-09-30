module ChickenCheck.Client.ViewComponents 

open Fable.React.Props
open Fable.React
open Fable.Core.JsInterop
open ChickenCheck.Client
open ChickenCheck.Domain
open Elmish
open ChickenCheck.Client.ApiHelpers
open Fulma
open Fulma.Elmish
open System

let loading = str "loading"

let apiErrorMsg clearAction status =
    let (isOpen, msg) = 
        match status with
        | NotStarted | ApiCallStatus.Completed | Running -> false, ""
        | Failed msg -> true, msg
    div
        [] 
        [ str msg] 

let centered content =
    div []
        content
