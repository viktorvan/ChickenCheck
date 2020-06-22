module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Shared
open Fable.Remoting.Client
open Turbolinks


let api : IChickensApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IChickensApi>
    
let browserService = BrowserService()
let turbolinks = Turbolinks()
    
// Workflows
let addEgg idStr dateStr  =
    let chickenId = ChickenId.parse idStr
    let date = NotFutureDate.parse dateStr
    Chickens.addEgg api browserService turbolinks chickenId date
let removeEgg idStr dateStr  = 
    let chickenId = ChickenId.parse idStr
    let date = NotFutureDate.parse dateStr
    Chickens.removeEgg api browserService turbolinks chickenId date
    
