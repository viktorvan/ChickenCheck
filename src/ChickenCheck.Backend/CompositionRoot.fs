module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Shared
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open System
open FSharpPlus
open Microsoft.Extensions.Logging

module Result =
    let bindAsync fAsync argResult = 
        match argResult with
        | Ok arg -> fAsync arg |> Async.map Ok
        | Error err -> Error err |> Async.retn
    
let config = Configuration.config.Value

let getStatus() =
    let now = DateTime.Now
    now.ToString("yyyyMMdd HH:mm:ss") |> sprintf "Ok at %s" 
    
// services
let chickenStore = Database.ChickenStore config.ConnectionString

// workflows
let healthCheck() = Workflows.healthCheck chickenStore ()
    
let getAllChickens date = Workflows.getAllChickens chickenStore date
    
let addEgg (chicken, date) = Workflows.addEgg chickenStore chicken date
    
let removeEgg (chicken, date) = Workflows.removeEgg chickenStore chicken date

let api : IChickensApi =
    { AddEgg = addEgg
      RemoveEgg = removeEgg }
