module ChickenCheck.Backend.Views.Htmx

open Giraffe.ViewEngine

let hxPost = attr "hx-post"
let hxTrigger = attr "hx-trigger"
let hxTarget = attr "hx-target"
let hxSwap = attr "hx-swap"