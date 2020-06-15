namespace ChickenCheck.Client

open ChickenCheck.Client.Chickens
open ChickenCheck.Shared


    
[<RequireQualifiedAccess>]
type Page =
    | Chickens of ChickensPageModel
    | NotFound

type Model =
    { Settings: Deferred<AuthenticationSettings>
      User: User
      CurrentUrl: Url
      CurrentPage: Page }
      
