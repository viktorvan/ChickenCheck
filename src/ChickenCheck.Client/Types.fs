namespace ChickenCheck.Client

open Browser
open Turbolinks

type IBrowserService =
    abstract StopPropagation : unit -> unit
    abstract GetElementById : string -> Types.HTMLElement option
    abstract FullUrlPath : string with get
    
type BrowserService() = 
    interface IBrowserService with
        member this.StopPropagation() = window.event.stopPropagation()
        member this.GetElementById id = document.getElementById id |> Option.ofObj
        member this.FullUrlPath = window.location.pathname + window.location.search
        
type ITurbolinks =
    abstract Reset : string -> unit
    
type Turbolinks() =
    interface ITurbolinks with
        member this.Reset(url) =
            TurbolinksLib.clearCache()
            TurbolinksLib.visit (url)

