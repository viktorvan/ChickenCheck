module ChickenCheck.Client.Authentication.Auth0

open Fable.Core
open Fable.Core.JsInterop

type IAuthLock =
    [<Emit("new $0($1...)")>] 
    abstract Create: string * string -> IAuthLock
    abstract show: unit -> unit
    
let private AuthLock: IAuthLock = importDefault "auth0-lock"

let authLock = AuthLock.Create("","")
