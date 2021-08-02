module ChickenCheck.Backend.Views.Htmx

open Giraffe.ViewEngine

let hxGet = attr "hx-get"
let hxPost = attr "hx-post"
let hxDelete = attr "hx-delete"
let hxTrigger = attr "hx-trigger"
let hxTarget = attr "hx-target"
let hxSwap = attr "hx-swap"
let hxIndicator = attr "hx-indicator"
let hxHeaders = attr "hx-headers"
let hxSelect = attr "hx-select"