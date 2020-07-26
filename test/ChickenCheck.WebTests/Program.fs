// Learn more about F# at http://fsharp.org

open System
open ChickenCheck.WebTests.Pages
open ChickenCheck.WebTests.Tests
open canopy.classic
open canopy.runner.classic
open canopy.types
   
[<EntryPoint>]
let main args =
    let port = args |> Array.tryHead |> Option.map int |> Option.defaultValue 8085
    start BrowserStartMode.ChromeHeadless
    // start BrowserStartMode.Chrome
    
    "Root url redirects to chickens page" &&& fun _ ->
        let rootUrl = sprintf "http://localhost:%i" port
        let today = DateTime.Today
        let chickensPage = ChickensPage.url port today
        url rootUrl
        onn chickensPage
    
    Chickens.all port
        
    run()
    
    quit()
    
    failedCount
