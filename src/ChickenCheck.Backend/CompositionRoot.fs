module ChickenCheck.Backend.CompositionRoot

open ChickenCheck.Domain
open Events
open FsToolkit.ErrorHandling
open ChickenCheck.Infrastructure
open System

let getEnvironmentVariable key =
    try 
        let value = Environment.GetEnvironmentVariable key
        if String.IsNullOrWhiteSpace value then invalidArg key "cannot be empty"
        else value
    with exn -> raise exn

let getEnvironmentVariableOrDefault defaultValue key =
    try
        getEnvironmentVariable key
    with exn -> defaultValue

let private connectionString = 
    getEnvironmentVariableOrDefault "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8" "ChickenCheckDbConnectionString"
    |> ConnectionString

let appendEvents = SqlStore.appendEvents connectionString
let appendEvent = List.singleton >> appendEvents

let getStatus() =
    let now = DateTime.Now
    now.ToString("yyyyMMdd HH:mm:ss") |> sprintf "Ok at %s" 

module User =
    let getUserByEmail = SqlUserStore.getUserByEmail connectionString

    let createSession : CreateSessionApi = 
        fun command ->
            asyncResult {
                let! user = command.Email |> getUserByEmail |> AsyncResult.mapError Database
                match user with
                | None -> return! UserDoesNotExist |> Login |> Error
                | Some user ->
                    if ChickenCheck.PasswordHasher.verifyPasswordHash (user.PasswordHash, command.Password) then
                        let token = Authentication.generateToken user.Name.Value
                        return {
                            Session.Token = token
                            Session.UserId = user.Id
                            Session.Name = user.Name
                        }
                    else return! PasswordIncorrect |> Login |> Error
            }

module Chicken =
    let getChickens : GetChickensApi = 
        fun request ->
            asyncResult {
                    let! _ = request |> Authentication.validate |> Result.mapError Authentication
                    return!
                        SqlChickenStore.getChickens connectionString ()
                        |> AsyncResult.mapError Database
            }

let chickenCheckApi : IChickenCheckApi = {
    GetStatus = fun () -> async { return getStatus() }
    CreateSession = User.createSession 
    GetChickens = Chicken.getChickens }
