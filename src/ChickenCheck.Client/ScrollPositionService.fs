namespace ChickenCheck.Client

open Browser

type IScrollPositionService =
    abstract Save : unit -> unit
    abstract Recall : unit -> unit
    
type ScrollPositionService() =
    
    let mutable scrollPosition = None
    interface IScrollPositionService with
        member __.Save() =
            scrollPosition <- Some {| X = window.scrollX; Y = window.scrollY |}
        member __.Recall() =
            scrollPosition 
            |> Option.iter (fun p -> window.scrollTo(p.X, p.Y))
            scrollPosition <- None
        
