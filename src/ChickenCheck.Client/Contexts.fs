module ChickenCheck.Client.Contexts

open Feliz
open ChickenCheck.Shared

let userContext = React.createContext(name="User", defaultValue=Anonymous)
