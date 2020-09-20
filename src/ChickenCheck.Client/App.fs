module Chickens

open Browser.Types
open ChickenCheck.Client
open Browser
open HtmlHelper.EventTargets
    
CompositionRoot.turbolinks.Start()

//let isAuthenticated = HtmlHelper.isAuthenticated()
//let ifAuthenticated f arg = if isAuthenticated then f arg

document.onclick <-
    fun ev ->
        let target = ev.target :?> Element
        match target with
//        | ChickenCard chickenId -> ifAuthenticated CompositionRoot.addEgg chickenId 
//        | EggIcon chickenId -> ifAuthenticated CompositionRoot.removeEgg chickenId 
//        | NavbarBurger -> CompositionRoot.toggleNavbarMenu()
        | GithubRepoLink -> PlausibleAnalytics.raiseCustomEvent "GithubRepo"
        | _ -> ()

document.addEventListener("turbolinks:load", fun _ ->
    CompositionRoot.scrollPositionService.Recall()
    CompositionRoot.initDatepicker())

