module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open ChickenCheck.Domain.Queries
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open ChickenCheck.Infrastructure
open System

let getEnvironmentVariable key =
    let value = Environment.GetEnvironmentVariable key
    if String.IsNullOrWhiteSpace value then invalidArg key "cannot be empty"
    else value

let getEnvironmentVariableOrDefault defaultValue key =
    try
        getEnvironmentVariable key
    with exn -> defaultValue

let private connectionString = 
    getEnvironmentVariableOrDefault "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8" "CHICKENCHECK_CONNECTIONSTRING"
    |> ConnectionString

let private tokenSecret =
    getEnvironmentVariableOrDefault "42be52e5a41d414d8855b6684aad48c2" "CHICKENCHECK_TOKEN_SECRET"

let private validate<'T> = 
    fun token ->
        token 
        |> Authentication.validate<'T> tokenSecret 
        |> Result.mapError Authentication

let appendEvents = SqlStore.appendEvents connectionString
let appendEvent = List.singleton >> appendEvents >> AsyncResult.mapError Database

let getStatus() =
    let now = DateTime.Now
    now.ToString("yyyyMMdd HH:mm:ss") |> sprintf "Ok at %s" 

module User =
    let getUserByEmail = SqlUserStore.getUserByEmail connectionString

    let createSession (CreateSession (Email = email; Password = password)) = 
        asyncResult {
            let! user = email |> getUserByEmail |> AsyncResult.mapError Database
            match user with
            | None -> return! UserDoesNotExist |> Login |> Error
            | Some user ->
                if ChickenCheck.PasswordHasher.verifyPasswordHash (user.PasswordHash, password) then
                    let! token = 
                        Authentication.generateToken tokenSecret user.Name.Value 
                        |> Result.mapError Authentication
                    return {
                        Session.Token = token
                        Session.UserId = user.Id
                        Session.Name = user.Name
                    }
                else return! PasswordIncorrect |> Login |> Error
        }

module Chicken =

    let getAllChickens () = 
        SqlChickenStore.getChickens connectionString ()
        |> AsyncResult.map Response.Chickens
        |> AsyncResult.mapError Database

    let getEggsOnDate onDate =
        asyncResult {
            return!
                SqlChickenStore.getEggCountOnDate connectionString onDate
                |> AsyncResult.map Response.EggCountOnDate
                |> AsyncResult.mapError Database
        }

    let getTotalEggCount () =
        asyncResult {
            return!
                SqlChickenStore.getTotalEggCount connectionString ()
                |> AsyncResult.map Response.TotalEggCount
                |> AsyncResult.mapError Database
        }

    let addEgg cmd =
        asyncResult {
            let event = cmd |> ChickenCommandHandler.handleAddEgg |> Events.ChickenEvent
            let! _ = event |> appendEvent
            return ()
        }

    let removeEgg cmd =
        asyncResult {
            let event = cmd |> ChickenCommandHandler.handleRemoveEgg |> Events.ChickenEvent
            let! _ = event |> appendEvent
            return ()
        }

let handleQuery request =
    asyncResult {
        let! query = request |> validate
        match query with
        | AllChickens -> return! Chicken.getAllChickens()
        | Queries.EggCountOnDate date -> return! Chicken.getEggsOnDate date
        | Queries.TotalEggCount -> return! Chicken.getTotalEggCount()

    }

let handleCommand request =
    asyncResult {
        let! cmd = request |> validate
        match cmd with
        | AddEgg c -> return! Chicken.addEgg c
        | RemoveEgg c-> return! Chicken.removeEgg c
    }

let chickenApi : IChickenApi =
    { Session = User.createSession 
      Query = handleQuery
      Command = handleCommand }