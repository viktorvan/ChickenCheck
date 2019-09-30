module ChickenCheck.Client.Messages
open ChickenCheck.Client


type Msg =
    | SigninMsg of Signin.Msg
    | ChickenMsg of Chickens.Msg
    
