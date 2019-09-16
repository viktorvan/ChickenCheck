module ChickenCheck.Client.Messages
open ChickenCheck.Client

type ChickenMsg =
    | IndexMsg of Chicken.Index.Msg


type Msg =
    | SigninMsg of Session.Signin.Msg
    | ChickenMsg of ChickenMsg
    | GoToChickens
    
