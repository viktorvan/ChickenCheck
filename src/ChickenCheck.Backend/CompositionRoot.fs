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
    
let inline logErrors (logger: ILogger<IChickenApi>) (workflow: 'TRequest -> Async<'TResult>) =
    try
        logger.LogInformation (sprintf "%A" workflow)
        workflow
    with exn ->
        logger.LogError(exn.Message)
        reraise()
    
// services
let chickenStore = Database.ChickenStore config.ConnectionString

// workflows
    
let getAllChickens date =
    Workflows.getAllChickens chickenStore date
    
let getEggCount (chickens, date) =
    Workflows.getEggCount chickenStore date chickens
    
let addEgg (date, chicken) =
    Workflows.addEgg chickenStore date chicken
    
let removeEgg (date, chicken) =
    Workflows.removeEgg chickenStore date chicken

let chickenApi logger : IChickenApi =
    let inline logError workflow = logErrors logger workflow
    { GetAllChickensWithEggs = getAllChickens |> logError
      GetEggCount = getEggCount |> logError }
    
let chickenEditApi logger : IChickenEditApi =
    let inline logError workflow = logErrors logger workflow
    { AddEgg = addEgg |> logError
      RemoveEgg = removeEgg |> logError }
    