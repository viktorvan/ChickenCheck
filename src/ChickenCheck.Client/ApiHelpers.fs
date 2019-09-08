module ChickenCheck.Client.ApiHelpers

[<Literal>]
let GeneralErrorMsg = "Någonting gick fel"
let handleApiError msgAction (exn:exn) = exn.ToString() |> sprintf "Exception: %s" |> msgAction