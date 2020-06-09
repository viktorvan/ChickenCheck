namespace ChickenCheck.Client

open ChickenCheck.Client.Chickens
open ChickenCheck.Client.Navbar

type Msg =
    | UrlChanged of Url
    | NavbarMsg of NavbarMsg
    | ChickenMsg of ChickenMsg

