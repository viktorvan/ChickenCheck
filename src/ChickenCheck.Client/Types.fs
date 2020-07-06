namespace ChickenCheck.Client

open Browser
open Fable.Core.JsInterop

type IBrowserService =
    abstract StopPropagation : unit -> unit
    abstract GetElementById : string -> Types.HTMLElement option
    abstract UrlQueryString : string with get
    abstract UrlPath : string with get
    abstract SaveScrollPosition : unit -> unit
    abstract RecallScrollPosition : unit -> unit
    
type BrowserService() =
    
    let mutable scrollPosition = None
    interface IBrowserService with
        member this.StopPropagation() = window.event.stopPropagation()
        member this.GetElementById id = document.getElementById id |> Option.ofObj
        member this.UrlPath = window.location.pathname
        member this.UrlQueryString = window.location.search
        member this.SaveScrollPosition() =
            scrollPosition <- Some {| X = window.scrollX; Y = window.scrollY |}
        member this.RecallScrollPosition() =
            scrollPosition 
            |> Option.iter (fun p -> window.scrollTo(p.X, p.Y))
            scrollPosition <- None
        
        
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
