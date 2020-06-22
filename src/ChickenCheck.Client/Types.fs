namespace ChickenCheck.Client

open ChickenCheck.Shared
open Browser
open Turbolinks

type IBrowserService =
    abstract StopPropagation : unit -> unit
    abstract HideEggIcon : ChickenId -> unit
    abstract ShowEggLoader : ChickenId -> unit
    abstract FullPath : string with get
    
type BrowserService() = 
    interface IBrowserService with
        member this.StopPropagation() = window.event.stopPropagation()
        member this.ShowEggLoader(id) =
            let eggIconLoader = document.getElementById("egg-icon-loader-" + id.Value.ToString()) |> Option.ofObj
            eggIconLoader |> Option.iter (fun l -> l.className <- l.className.Replace("is-hidden", ""))
        member this.HideEggIcon(id) =
            let eggIconLoader = document.getElementById("egg-icon-" + id.Value.ToString()) |> Option.ofObj
            eggIconLoader |> Option.iter (fun l -> l.className <- l.className + " is-hidden")
        member this.FullPath = window.location.pathname + window.location.search
        
type ITurbolinks =
    abstract Reset : string -> unit
    
type Turbolinks() =
    interface ITurbolinks with
        member this.Reset(url) =
            TurbolinksLib.clearCache()
            TurbolinksLib.visit (url)

