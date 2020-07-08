namespace ChickenCheck.Client

open Fable.Core.JsInterop

type private ITurbolinksLib =
    abstract start : unit -> unit
    abstract setProgressBarDelay : int -> unit
    abstract clearCache : unit -> unit
    abstract visit : string -> unit
        
type ITurbolinks =
    abstract Start : unit -> unit
    abstract Reset : string -> unit
    abstract Visit : string -> unit
    
type Turbolinks() =
    let turbolinks : ITurbolinksLib = importDefault "turbolinks"
    
    interface ITurbolinks with
        member this.Start() = turbolinks.start()
        member this.Reset(url) =
            turbolinks.clearCache()
            turbolinks.visit (url)
        member this.Visit(url) = turbolinks.visit(url)
