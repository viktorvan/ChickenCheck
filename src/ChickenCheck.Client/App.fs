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
        
let isChickenCard (target: Element) =
    target.closest(".chicken-card") |> Option.isSome
let isEggIcon (target: Element) =
    target.closest(".egg-icon") |> Option.isSome
    
let isNavbarBurger (target: Element) =
    target.closest(".navbar-burger") |> Option.isSome
    
let (|ChickenCard|_|) (target: Element) =
    if isChickenCard target && not (isEggIcon target) then
        Some (ChickenCard target.ChickenId)
    else 
        None
    
let (|EggIcon|_|) (target: Element) =
    if isEggIcon target then
        Some (EggIcon target.ChickenId)
    else 
        None
        
let (|NavbarBurger|_|) (target: Element) =
    if isNavbarBurger target then
        Some NavbarBurger
    else
        None


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
