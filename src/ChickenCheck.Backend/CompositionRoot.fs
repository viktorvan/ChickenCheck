module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Domain
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open System
open FSharpPlus

module Result =
    let bindAsync fAsync argResult = 
        match argResult with
        | Ok arg -> fAsync arg |> Async.map Ok
        | Error err -> Error err |> Async.retn
    
module private Config =
    let private getEnvironmentVariable key =
        let value = Environment.GetEnvironmentVariable key
        if String.IsNullOrWhiteSpace value then invalidArg key "cannot be empty"
        else value

    let private getEnvironmentVariableOrDefault defaultValue key =
        try
            getEnvironmentVariable key
        with exn -> defaultValue

    let connectionString = 
        getEnvironmentVariableOrDefault "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8" "CHICKENCHECK_CONNECTIONSTRING"
        |> ConnectionString

    let tokenSecret =
        getEnvironmentVariableOrDefault "42be52e5a41d414d8855b6684aad48c2" "CHICKENCHECK_TOKEN_SECRET"

let private validate<'T> token = 
    token 
    |> Authentication.validate<'T> Config.tokenSecret 

let appendEvents = SqlStore.appendEvents Config.connectionString
let appendEvent = Seq.singleton >> appendEvents

let getStatus() =
    let now = DateTime.Now
    now.ToString("yyyyMMdd HH:mm:ss") |> sprintf "Ok at %s" 

module User =
    let private getUserByEmail = SqlUserStore.getUserByEmail Config.connectionString

    let createSession (email, password) =
        let verifyPasswordHash user =
            if ChickenCheck.PasswordHasher.verifyPasswordHash (user.PasswordHash, password) then
                { Token = Authentication.generateToken Config.tokenSecret user.Name.Value
                  UserId = user.Id
                  Name = user.Name } |> Ok
            else PasswordIncorrect |> Error
        getUserByEmail email
        |>> (Result.requireSome UserDoesNotExist)
        |>> (Result.bind verifyPasswordHash)
        
module Chicken =
    let getEggsOnDate = SqlChickenStore.getEggCountOnDate Config.connectionString
    let getTotalEggCount = SqlChickenStore.getTotalEggCount Config.connectionString
    let getAllChickens = SqlChickenStore.getChickens Config.connectionString
        
    let private toChickenWithEggCount (countOnDate) (totalCount) chicken =
        let onDate = Map.find chicken.Id countOnDate 
        let total = Map.find chicken.Id totalCount
        { Chicken = chicken
          OnDate = onDate
          Total = total }
        
    let getAllChickensWithEggs date =
        async {
            let! chickens = getAllChickens()
            let! eggCountOnDate = getEggsOnDate date 
            let! totalEggCount = getTotalEggCount()
            return chickens |> List.map (toChickenWithEggCount eggCountOnDate totalEggCount) 
        }
        
    let addEgg =
        Commands.AddEgg.Create
        >> ChickenCommandHandler.handleAddEgg
        >> Events.ChickenEvent
        >> appendEvent
        
    let removeEgg =
        Commands.RemoveEgg.Create
        >> ChickenCommandHandler.handleRemoveEgg
        >> Events.ChickenEvent
        >> appendEvent

let chickenApi : IChickenApi =
    { CreateSession = User.createSession
      GetAllChickensWithEggs = validate >> Result.bindAsync Chicken.getAllChickensWithEggs
      GetEggCountOnDate = validate >> Result.bindAsync Chicken.getEggsOnDate
      AddEgg = validate >> Result.bindAsync Chicken.addEgg
      RemoveEgg = validate >> Result.bindAsync Chicken.removeEgg }
    