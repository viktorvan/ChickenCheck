module ChickenCheck.Client.HtmlHelper

open Browser
open Browser.Types
open Fable.Core.JsInterop
open ChickenCheck.Shared

let toggleClass className (e: Element) =
    let newClasses =
        if e.className.Contains(className) then
            e.className.Replace(className, "")
        else
            e.className + " " + className
    e.className <- newClasses
        
module DataAttributes =
    
    let chickenId (element: Element) =
        element?dataset?chickenId
        |> ChickenId.parse
        
    let currentDate() = 
        document.querySelector(sprintf "[%s]" DataAttributes.CurrentDate)?dataset?currentDate
        |> NotFutureDate.parse
        
type Element with
    member this.ChickenId = DataAttributes.chickenId(this)
        
module EventTargets =
    
    let private isEggIcon (target: Element) =
        target.closest(".egg-icon")
    let private isChickenCard (target: Element) =
        if (isEggIcon target |> Option.isSome) then None
        else 
            target.closest(".chicken-card")
    let private isNavbarBurger (target: Element) =
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
        
