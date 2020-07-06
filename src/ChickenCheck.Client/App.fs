module Chickens

open Browser.Types
open ChickenCheck.Client

open Browser
open ChickenCheck.Shared
open Turbolinks
open Fable.Core.JsInterop


type Types.Element with
    member this.ChickenId =
        this?dataset?chickenId
        |> ChickenId.parse
        
let isEggIcon (target: Element) =
    target.closest(".egg-icon")
let isChickenCard (target: Element) =
    if (isEggIcon target |> Option.isSome) then None
    else 
        target.closest(".chicken-card")
let isNavbarBurger (target: Element) =
    target.closest(".navbar-burger")
    
let (|ChickenCard|_|) (target: Element) =
    target
    |> isChickenCard
    |> Option.map (fun e -> ChickenCard e.ChickenId)
    
let (|EggIcon|_|) (target: Element) =
    target
    |> isEggIcon
    |> Option.map (fun e -> EggIcon e.ChickenId)
        
let (|NavbarBurger|_|) (target: Element) =
    target
    |> isNavbarBurger
    |> Option.map (fun _ -> NavbarBurger)


TurbolinksLib.start()

document.onpointerdown <-
    fun ev ->
        let currentDate = 
            document.querySelector(sprintf "[%s]" DataAttributes.CurrentDate)?dataset?currentDate
            |> NotFutureDate.parse
    
        let target = ev.target :?> Element
        match target with
        | ChickenCard chickenId -> CompositionRoot.addEgg chickenId currentDate
        | EggIcon chickenId -> CompositionRoot.removeEgg chickenId currentDate
        | NavbarBurger -> CompositionRoot.toggleNavbarMenu()
        | _ -> ()
        
document.addEventListener("turbolinks:load", fun _ -> CompositionRoot.browserService.RecallScrollPosition())
