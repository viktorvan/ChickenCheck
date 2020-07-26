namespace ChickenCheck.Client

open Browser

type IBrowserService =
    abstract StopPropagation : unit -> unit
    abstract GetElementById : string -> Types.HTMLElement option
    abstract QuerySelector : string -> Types.Element option
    abstract UrlQueryString : string with get
    abstract UrlPath : string with get
    
type BrowserService() =
    
    let mutable scrollPosition = None
    interface IBrowserService with
        member __.StopPropagation() = window.event.stopPropagation()
        member __.GetElementById id = document.getElementById id |> Option.ofObj
        member __.QuerySelector selector = document.querySelector selector |> Option.ofObj
        member __.UrlPath = window.location.pathname
        member __.UrlQueryString = window.location.search
