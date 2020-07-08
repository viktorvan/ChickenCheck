module Chickens

open Browser.Types
open ChickenCheck.Client
open Browser
open HtmlHelper.EventTargets
    
CompositionRoot.turbolinks.Start()

document.onpointerdown <-
    fun ev ->
        let target = ev.target :?> Element
        match target with
        | ChickenCard chickenId -> CompositionRoot.addEgg chickenId 
        | EggIcon chickenId -> CompositionRoot.removeEgg chickenId 
        | NavbarBurger -> CompositionRoot.toggleNavbarMenu()
        | _ -> ()
        
document.addEventListener("turbolinks:load", fun _ ->
    CompositionRoot.browserService.RecallScrollPosition()
    CompositionRoot.initDatepicker())

