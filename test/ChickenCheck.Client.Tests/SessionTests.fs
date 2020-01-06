module ChickenCheck.Client.Tests.SessionTests

open Swensen.Unquote
open System
open Expecto
open ChickenCheck.Client
open ChickenCheck.Domain
open ChickenCheck.Client.Tests
open TestHelpers

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

[<Tests>]
let tests =
    testList "Session.update" [
        test "On Submit sets LoginStatus to running" {
            let model, _ = Session.update Submit validModel
            model =! { validModel with LoginStatus = Running }
        }
        test "On Submit should send createSession" {
            let _, cmds = Session.update Submit validModel
            cmds =! [ CmdMsg.CreateSession (email, password) ]
        }

        [ { validModel with Email = StringInput.Empty }
          { validModel with Email = StringInput.Invalid "abc" }
          { validModel with Password = StringInput.Empty }
          { validModel with Password = StringInput.Invalid "abc" }
          { validModel with 
              Email = StringInput.Empty
              Password = StringInput.Empty } ]
        |> List.map (fun model -> test "Submit should throw if model is invalid" {
            toAction2 Session.update Submit model
            |> Expect.throwsWithMessage "tried to submit invalid form"
        })
        |> testList "Submit with invalid models"

        test "On LoginCompleted Sets ApiCallStatus = Completed" {
            let msg = LoginCompleted session
            let newModel, _ = Session.update msg validModel
            newModel =! { validModel with LoginStatus = Completed }
        }

        test "On LoginCompleted sends SignedIn msg" {
            let msg = LoginCompleted session
            let _, cmds = Session.update msg validModel
            let expectedCmd = SignedIn session |> CmdMsg.OfMsg
            cmds =! [ expectedCmd ]
        }

        test "On AddError should add error message and set LoginStatus to completed" {
            let msg = SessionMsg.AddError "something failed"
            let model, _ = Session.update msg validModel
            model =! { validModel with
                        Errors = [ "something failed"]
                        LoginStatus = Completed }
        }
    ]