module ChickenCheck.Client.Tests.SessionTests

open ChickenCheck.Client
open ChickenCheck.Client.ApiCommands
open Elmish
open Swensen.Unquote
open System
open Expecto
open ChickenCheck.Domain
open ChickenCheck.Client.Tests
open TestHelpers
open FsToolkit.ErrorHandling

let token = 
    "6cf0fb82-b683-46fc-aea7-40dac558d7ce" 
    |> String1000.create "token"
    |> Result.okValue
    |> SecurityToken

let userId = 
    Guid.NewGuid()
    |> UserId.create
    |> Result.okValue

let username =
    "Testnamn"
    |> String200.create "username"
    |> Result.okValue

let session = 
    { Token = token
      UserId = userId 
      Name = username }
      
let email = 
    "test@example.com"
    |> Email.create
    |> Result.okValue

let password =
    "super-secret"
    |> Password.create
    |> Result.okValue

let validModel = 
    { Session.init() with
        Email = StringInput.Valid email
        Password = StringInput.Valid password }
    
type MockCmds() =
    let mutable activeLogin = None
    member __.ActiveLogin = activeLogin
    interface ISessionApiCmds with
        member this.Login(email, pw) : Elmish.Cmd<Msg> =
            activeLogin <- Some (email,pw)
            Cmd.none
            
let mockCmds = MockCmds()

let mockApi =
    { CreateSession = failwith "notImplemented"
      GetAllChickensWithEggs = failwith "notImplemented"
      GetEggCountOnDate = failwith "notImplemented"
      AddEgg = failwith "notImplemented"
      RemoveEgg = failwith "notImplemented" }

[<Tests>]
let tests =
    testList "Session.update" [
        test "Start SignIn sets LoginStatus to in progress" {
            let msg = SignIn (Start ())
            let model, _ = Session.update mockCmds msg validModel
            model =! { validModel with LoginStatus = InProgress }
        }
        test "Start SignIn should call Login cmd" {
            let msg = SignIn (Start ())
            let _, _ = Session.update mockCmds msg validModel
            mockCmds.ActiveLogin =! Some (email, password)
        }
        [ { validModel with Email = StringInput.Empty }
          { validModel with Email = StringInput.Invalid "abc" }
          { validModel with Password = StringInput.Empty }
          { validModel with Password = StringInput.Invalid "abc" }
          { validModel with 
              Email = StringInput.Empty
              Password = StringInput.Empty } ]
        |> List.map (fun model -> test "Submit should throw if model is invalid" {
            let msg = SignIn (Start ())
            toAction3 Session.update mockCmds msg model
            |> Expect.throwsWithMessage "tried to submit invalid form"
        })
        |> testList "Submit with invalid models"
        test "On SignIn Finished Sets LoginStatus = Resolved" {
            let msg = SignIn (Finished (LoginError.PasswordIncorrect))
            let newModel, _ = Session.update mockCmds msg validModel
            newModel =! { validModel with LoginStatus = Resolved (Ok ()) }
        }
        testList "sessionApiCmds" [
            testAsync "SessionCmds.createSession returns SignedIn on success" {
                let api = { mockApi with CreateSession = fun (email,pw) -> AsyncResult.retn session  }
                let! result = SessionApiCmds.createSession api email password
                result =! SignedIn session
            }
        ]
    ]