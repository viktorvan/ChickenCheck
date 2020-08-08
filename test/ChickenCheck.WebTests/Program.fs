// Learn more about F# at http://fsharp.org

open System
open ChickenCheck.WebTests.Pages
open ChickenCheck.WebTests.Tests
open canopy.classic
open canopy.runner.classic
open canopy.types
   
canopy.configuration.failFast := true

[<EntryPoint>]
let main args =

    let rootUrl = args |> Array.tryHead |> Option.defaultValue "https://localhost:8085"
    start BrowserStartMode.ChromeHeadless
//    start BrowserStartMode.Chrome
    resize (1920, 1080)
    
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
