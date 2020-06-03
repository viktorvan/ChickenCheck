module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Shared
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open System
open FSharpPlus

module Result =
    let bindAsync fAsync argResult = 
        match argResult with
        | Ok arg -> fAsync arg |> Async.map Ok
        | Error err -> Error err |> Async.retn
    
let config = Configuration.config.Value
let tokenSecret = Configuration.config.Value.TokenSecret

let private validate<'T> token = 
    token 
    |> Authentication.validate<'T> tokenSecret 

let getStatus() =
    let now = DateTime.Now
    now.ToString("yyyyMMdd HH:mm:ss") |> sprintf "Ok at %s" 
    
let runSecure (workflow: 'TRequest -> Async<'TResult>) (req: SecureRequest<'TRequest>) =
    let validateToken = Authentication.validate config.TokenSecret
    asyncResult {
        let! validatedReq = validateToken req
        return! workflow validatedReq
    }
        
// services
let tokenService = Authentication.TokenService config.TokenSecret 
let userStore = Database.UserStore config.ConnectionString 
let chickenStore = Database.ChickenStore config.ConnectionString

// workflows
let createSession (email, pw) =
    Workflows.createSession tokenService userStore (email, pw)
    
let getAllChickens date =
    Workflows.getAllChickens chickenStore date
    
let getEggCount (chickens, date) =
    Workflows.getEggCount chickenStore date chickens
    
let addEgg (date, chicken) =
    Workflows.addEgg chickenStore date chicken
    
let removeEgg (date, chicken) =
    Workflows.removeEgg chickenStore date chicken

let chickenApi : IChickenApi =
    { CreateSession = createSession
      GetAllChickensWithEggs = runSecure getAllChickens
      GetEggCount = runSecure getEggCount
      AddEgg = runSecure addEgg
      RemoveEgg = runSecure removeEgg }
    