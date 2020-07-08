module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Shared
open Fable.Remoting.Client


let api : IChickensApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IChickensApi>
    
let browserService = BrowserService() :> IBrowserService
let turbolinks = Turbolinks() :> ITurbolinks
let currentDate = HtmlHelper.DataAttributes.currentDate()
    
// Workflows
let toggleNavbarMenu() =
    Navbar.toggleNavbarMenu browserService
let addEgg chickenId =
    Chickens.addEgg api browserService turbolinks chickenId currentDate
let removeEgg chickenId = 
    Chickens.removeEgg api browserService turbolinks chickenId currentDate
let initDatepicker () =
        Datepicker.init browserService turbolinks currentDate
