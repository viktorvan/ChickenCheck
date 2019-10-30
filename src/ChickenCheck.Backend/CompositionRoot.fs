module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Domain
open ChickenCheck.Domain.Commands
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
let appendEvent = List.singleton >> appendEvents

let getStatus() =
    let now = DateTime.Now
    now.ToString("yyyyMMdd HH:mm:ss") |> sprintf "Ok at %s" 

module User =
    let getUserByEmail = SqlUserStore.getUserByEmail connectionString

    let createSession command = 
        asyncResult {
            let! user = command.Email |> getUserByEmail |> AsyncResult.mapError Database
            match user with
            | None -> return! UserDoesNotExist |> Login |> Error
            | Some user ->
                if ChickenCheck.PasswordHasher.verifyPasswordHash (user.PasswordHash, command.Password) then
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
    let getChickens request = 
        asyncResult {
            let! _ = request |> validate
            return!
                SqlChickenStore.getChickens connectionString ()
                |> AsyncResult.mapError Database
        }

    let getEggsOnDate request =
        asyncResult {
            let! onDate = request |> validate
            return!
                SqlChickenStore.getEggCountOnDate connectionString onDate
                |> AsyncResult.mapError Database
        }

    let getTotalEggCount request =
        asyncResult {
            let! _ = request |> validate
            return!
                SqlChickenStore.getTotalEggCount connectionString ()
                |> AsyncResult.mapError Database
        }

    let addEgg request =
        asyncResult {
            let! cmd = request |> validate
            let event = cmd |> ChickenCommandHandler.handleAddEgg |> Events.ChickenEvent
            let! _ = event |> (appendEvent >> AsyncResult.mapError Database)
            return ()
        }

    let removeEgg request =
        asyncResult {
            let! cmd = request |> validate
            let event = cmd |> ChickenCommandHandler.handleRemoveEgg |> Events.ChickenEvent
            let! _ = event |> (appendEvent >> AsyncResult.mapError Database)
            return ()
        }

let chickenApi : IChickenApi = 
    { GetStatus = fun () -> async { return getStatus() }
      CreateSession = User.createSession 
      GetChickens = Chicken.getChickens 
      GetEggCountOnDate = Chicken.getEggsOnDate 
      GetTotalEggCount = Chicken.getTotalEggCount
      AddEgg = Chicken.addEgg 
      RemoveEgg = Chicken.removeEgg }
