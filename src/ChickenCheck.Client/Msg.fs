namespace ChickenCheck.Client

open ChickenCheck.Client.Chickens
open ChickenCheck.Shared

type Msg =
    | UrlChanged of Url
    | ChickenMsg of Msg
    | ShowLogin
    | Logout
    | LoggedIn
    | LoadSettings of AsyncOperationStatus<unit,Result<AuthenticationSettings, string>>