module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Shared
open Fable.Remoting.Client


let api : IChickensApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IChickensApi>
    
let browserService = BrowserService() :> IBrowserService
let turbolinks = Turbolinks()
    
// Workflows
let toggleNavbarMenu() =
    Navbar.toggleNavbarMenu browserService
let addEgg chickenId date =
    Chickens.addEgg api browserService turbolinks chickenId date
let removeEgg chickenId date = 
    Chickens.removeEgg api browserService turbolinks chickenId date
