module ChickenCheck.Infrastructure.SqlHelpers

open ChickenCheck.Domain
open System

[<Literal>]
let DevConnectionString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

let now() = DateTime.UtcNow

let toDatabaseError (ValidationError (param,msg)) =
    (param,msg) ||> sprintf "Failed to parse data from database %s %s" |> DatabaseError
