module ChickenCheck.Client.SessionHandler

open ChickenCheck.Domain

let expired = new Event<unit>()

open Fable.SimpleJson
let store (session : Session) =
    let json = Json.stringify session
    Browser.WebStorage.localStorage.setItem("session", json)

let delete() =
    Browser.WebStorage.localStorage.removeItem("session")

let tryGet() : Session option =
    match Browser.WebStorage.localStorage.getItem("session") with
    | null ->
        None

    | session ->
        match Json.tryParseAs session with
        | Ok session ->
            Some session
        | Error msg ->
            Logger.warning "Error when decoding the stored session"
            Logger.warning msg
            Logger.warning "Cleaning, stored session..."
            Browser.WebStorage.localStorage.removeItem("session")
            None
