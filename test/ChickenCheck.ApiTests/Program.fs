open Expecto

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssembly { defaultConfig with allowDuplicateNames = true } argv
