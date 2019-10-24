module ChickenCheck.Client.Domain
open ChickenCheck.Client
open ChickenCheck.Domain


type Msg =
    | SigninMsg of Signin.Msg
    | Signout
    | NavbarMsg of Navbar.Msg
    | ChickenMsg of Chickens.Msg
    | ToggleReleaseNotes

[<RequireQualifiedAccess>]
type Page =
    | Signin of Signin.Model
    | Chickens of Chickens.Model
    | Loading
    | NotFound

type Model =
    { CurrentRoute: Router.Route option
      Session: Session option
      Navbar: Navbar.Model
      ActivePage: Page 
      ShowReleaseNotes: bool }

    
