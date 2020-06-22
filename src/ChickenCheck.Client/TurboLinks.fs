module Turbolinks

open Fable.Core.JsInterop

type ITurbolinksLib =
    abstract start : unit -> unit
    abstract setProgressBarDelay : int -> unit
    abstract clearCache : unit -> unit
    abstract visit : string -> unit
    
let TurbolinksLib : ITurbolinksLib = importDefault "turbolinks"

