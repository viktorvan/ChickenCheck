namespace ChickenCheck.Client

open ChickenCheck.Client.Chickens
open ChickenCheck.Shared

[<RequireQualifiedAccess>]
type Url =
    | Home
    | Chickens of NotFutureDate
    | NotFound
    | LogIn of Destination: string option
    | LogOut
    
[<RequireQualifiedAccess>]
type Page =
    | Chickens of ChickensPageModel
    | NotFound

type Model =
    { User: Deferred<User>
      CurrentUrl: Url
      CurrentPage: Page
      IsMenuExpanded: bool }
      
