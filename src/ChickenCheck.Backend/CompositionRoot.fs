module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Domain
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open ChickenCheck.Infrastructure
open System


module Async =
    let retn = fun x -> async { return x }

let private (>>=) ar f = AsyncResult.bind f ar
let private (>=>) f1 f2 ar = f1 ar >>= f2

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

let private validate<'T> = 
    fun token ->
        token 
        |> Authentication.validate<'T> Config.tokenSecret 
        |> Result.mapError Authentication
        |> Async.retn

let appendEvents = SqlStore.appendEvents Config.connectionString
let appendEvent = List.singleton >> appendEvents >> AsyncResult.mapError Database

let getStatus() =
    let now = DateTime.Now
    now.ToString("yyyyMMdd HH:mm:ss") |> sprintf "Ok at %s" 

module User =

    let private getUserByEmail = SqlUserStore.getUserByEmail Config.connectionString

    let createSession (email, password) = 
        asyncResult {
            let! user = email |> getUserByEmail |> AsyncResult.mapError Database
            match user with
            | None -> return! UserDoesNotExist |> Login |> Error
            | Some user ->
                if ChickenCheck.PasswordHasher.verifyPasswordHash (user.PasswordHash, password) then
                    let! token = 
                        Authentication.generateToken Config.tokenSecret user.Name.Value 
                        |> Result.mapError Authentication
                    return {
                        Session.Token = token
                        Session.UserId = user.Id
                        Session.Name = user.Name
                    }
                else return! PasswordIncorrect |> Login |> Error
        }

module Chicken =
    let getAllChickens = 
        SqlChickenStore.getChickens Config.connectionString
        >> AsyncResult.mapError Database

    let getEggsOnDate = 
        SqlChickenStore.getEggCountOnDate Config.connectionString
        >> AsyncResult.mapError Database

    let getTotalEggCount =
        SqlChickenStore.getTotalEggCount Config.connectionString
        >> AsyncResult.mapError Database

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
      GetAllChickens = validate >=> Chicken.getAllChickens
      GetEggCountOnDate = validate >=> Chicken.getEggsOnDate
      GetTotalEggCount = validate >=> Chicken.getTotalEggCount
      AddEgg = validate >=> Chicken.addEgg
      RemoveEgg = validate >=> Chicken.removeEgg }