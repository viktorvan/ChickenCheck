// Learn more about F# at http://fsharp.org

open Fake.IO
open ViktorVan.Fake

[<EntryPoint>]
let main argv =
    if argv.Length < 2 then invalidArg "argv" "Must provide paths"

    let functionsPath = argv.[0]
    let functionsBinPath = argv.[1]

    let localSettings = Path.combine functionsPath "local.settings.json" |> Seq.singleton
    Shell.copy functionsBinPath localSettings
    Azure.Functions.start functionsBinPath
    1   
