module ChickenCheck.Client.ApiHelpers
open ChickenCheck.Domain
open Elmish

[<Literal>]
let GeneralErrorMsg = "Någonting gick fel"
let handleApiError msgAction (exn:exn) = exn.ToString() |> sprintf "Exception: %s" |> msgAction

let callSecureApi apiToken apiFunc arg successMsg errorMsg =
    let request = { Content = arg; Token = apiToken }

    let ofSuccess = function
        | Ok res -> res |> successMsg
        | Result.Error (err:DomainError) -> 
            match err with
            | Authentication (UserTokenExpired) -> 
                SessionHandler.expired.Trigger()
                "Token expired" |> errorMsg
            | _ ->
                err.ErrorMsg |> errorMsg
    let ofError _ = "Serverfel" |> errorMsg
    Cmd.OfAsync.either apiFunc request ofSuccess ofError

let createSession apiFunc query =
    let ofSuccess result =
        match result with
        | Ok session -> SessionMsg.LoginCompleted session |> SessionMsg
        | Error err ->
            let msg =
                match err with
                | Login l ->
                    match l with
                    | UserDoesNotExist -> "Användaren saknas"
                    | PasswordIncorrect -> "Fel lösenord"
                | _ -> GeneralErrorMsg
            msg |> SessionMsg.AddError |> SessionMsg

    Cmd.OfAsync.either
        apiFunc
        query
        ofSuccess
        (handleApiError (SessionMsg.AddError >> SessionMsg))
