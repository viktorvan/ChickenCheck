namespace ChickenCheck.Client

open Browser

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
        
