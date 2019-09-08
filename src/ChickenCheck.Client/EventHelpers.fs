[<RequireQualifiedAccessAttribute>]
module ChickenCheck.Client.FormEvent
let asOption s =
    if System.String.IsNullOrWhiteSpace s then None else Some s