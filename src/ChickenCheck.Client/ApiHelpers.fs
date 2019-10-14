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
                printfn "callSecureApi: session expired"
                Session.expired.Trigger()
                "Token expired" |> errorMsg
            | _ ->
                err.ErrorMsg |> errorMsg
    let ofError _ = "Serverfel" |> errorMsg
    Cmd.OfAsync.either apiFunc request ofSuccess ofError