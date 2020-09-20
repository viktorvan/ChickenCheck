module ChickenCheck.Client.CompositionRoot

open ChickenCheck.Shared
open Fable.SimpleHttp
    
type Api() =
    let eggRequest (id: ChickenId) (date: NotFutureDate) =
        let addCsrfHeader req =
            let token = HtmlHelper.Csrf.getToken()
            req
            |> Http.header (Headers.create "RequestVerificationToken" token)
            
        let route = sprintf "/chickens/%s/eggs/%s" (id.Value.ToString()) (date.ToString())
        
        Http.request route
        |> addCsrfHeader
        
        
    let addEgg : ChickenId * NotFutureDate -> Async<unit> =
        fun (id, date) ->
            async {
                let! response =
                    eggRequest id date
                    |> Http.method PUT
                    |> Http.send
                if (response.statusCode = 202) then return ()
                else failwith response.responseText
            }
            
    let removeEgg : ChickenId * NotFutureDate -> Async<unit> =
        fun (id, date) ->
            async {
                let! response =
                    eggRequest id date
                    |> Http.method DELETE
                    |> Http.send
                if (response.statusCode = 202) then return ()
                else failwith response.responseText
            }
            
    
    interface IChickensApi with
        member this.AddEgg (id, date) = addEgg (id, date)
        member this.RemoveEgg (id, date) = removeEgg (id, date)
    
let api = Api()
let scrollPositionService = ScrollPositionService() :> IScrollPositionService
let browserService = BrowserService() :> IBrowserService
let turbolinks = Turbolinks() :> ITurbolinks
let currentDate() = HtmlHelper.DataAttributes.parseCurrentDate()
    
// Workflows
let toggleNavbarMenu() =
    Navbar.toggleNavbarMenu browserService
//let addEgg chickenId =
//    Chickens.addEgg api browserService turbolinks scrollPositionService chickenId (currentDate())
//let removeEgg chickenId = 
//    Chickens.removeEgg api browserService turbolinks scrollPositionService chickenId (currentDate())
let initDatepicker() =
    Datepicker.init browserService turbolinks (currentDate())
