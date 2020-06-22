module Turbolinks

open Fable.Core.JsInterop

type ITurbolinks =
    abstract start : unit -> unit
    abstract setProgressBarDelay : int -> unit
    
let Turbolinks : ITurbolinks = importDefault "turbolinks"

let start() = Turbolinks.start()
let setProgressBarDelay delay = 
    Turbolinks.setProgressBarDelay delay



