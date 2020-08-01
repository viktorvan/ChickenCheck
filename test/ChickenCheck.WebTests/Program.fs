// Learn more about F# at http://fsharp.org

open System
open ChickenCheck.WebTests.Pages
open ChickenCheck.WebTests.Tests
open canopy.classic
open canopy.runner.classic
open canopy.types
   
[<EntryPoint>]
let main args =
    let rootUrl = args |> Array.tryHead |> Option.defaultValue "http://localhost:8085"
    start BrowserStartMode.ChromeHeadless
    // start BrowserStartMode.Chrome
    
    "Root url redirects to chickens page" &&& fun _ ->
        let today = DateTime.Today
        let chickensPage = ChickensPage.url rootUrl today
        url rootUrl
        onn chickensPage
    
    Chickens.all rootUrl

    "Health endpoint returns time in timezone /Europe/Stockholm" &&& fun _ ->
        let now = DateTime.Now
        url (rootUrl + "/health")
        contains (now.ToString("yyyy-MM-dd hh:mm")) (read "#healthCheckTime")
        
    run()
    
    quit()
    
    failedCount
