module ChickenCheck.Client.Messages
open ChickenCheck.Client


type Msg =
    | SigninMsg of Signin.Msg
    | Signout
    | ChickenMsg of Chickens.Msg
    
