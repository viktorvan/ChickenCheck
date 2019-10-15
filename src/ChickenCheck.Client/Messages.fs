module ChickenCheck.Client.Messages
open ChickenCheck.Client


type Msg =
    | SigninMsg of Signin.Msg
    | Signout
    | NavbarMsg of Navbar.Msg
    | ChickenMsg of Chickens.Msg
    
