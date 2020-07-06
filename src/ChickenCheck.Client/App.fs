module Chickens

open Browser.Types
open ChickenCheck.Client
open Browser
open HtmlHelper.EventTargets
    
CompositionRoot.turbolinks.Start()

document.onpointerdown <-
    fun ev ->
        let currentDate = HtmlHelper.DataAttributes.currentDate()
        let target = ev.target :?> Element
        match target with
        | ChickenCard chickenId -> CompositionRoot.addEgg chickenId currentDate
        | EggIcon chickenId -> CompositionRoot.removeEgg chickenId currentDate
        | NavbarBurger -> CompositionRoot.toggleNavbarMenu()
        | _ -> ()
        
document.addEventListener("turbolinks:load", fun _ ->
    CompositionRoot.browserService.RecallScrollPosition()
    CompositionRoot.initDatepicker (HtmlHelper.DataAttributes.currentDate()))

