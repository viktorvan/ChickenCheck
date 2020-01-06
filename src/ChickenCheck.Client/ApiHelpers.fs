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
        | Result.Error (err:AuthenticationError) ->
            match err with
            | UserTokenExpired -> 
                SessionHandler.expired.Trigger()
                "Token expired" |> errorMsg
            | TokenInvalid _ ->
                Signout
    let ofError _ = "Serverfel" |> errorMsg
    Cmd.OfAsync.either apiFunc request ofSuccess ofError

let createSession apiFunc query =
    let ofSuccess result =
        match result with
        | Ok session -> SessionMsg.LoginCompleted session |> SessionMsg
        | Error err ->
            let msg =
                match err with
                | _ -> GeneralErrorMsg
            msg |> SessionMsg.AddError |> SessionMsg

    Cmd.OfAsync.either
        apiFunc
        query
        ofSuccess
        (handleApiError (SessionMsg.AddError >> SessionMsg))
