// Learn more about F# at http://fsharp.org

open ChickenCheck.WebTests.Tests
open canopy.classic
open canopy.runner.classic
open canopy.types
   
[<EntryPoint>]
let main _ =
    start BrowserStartMode.ChromeHeadless
    // start BrowserStartMode.Chrome
    
    Chickens.all()
        
    run()
    
    quit()
    
    failedCount
