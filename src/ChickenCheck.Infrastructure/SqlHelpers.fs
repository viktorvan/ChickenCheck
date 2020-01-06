module ChickenCheck.Infrastructure.SqlHelpers

open ChickenCheck.Domain
open System
open FsToolkit.ErrorHandling

[<Literal>]
let DevConnectionString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

let now() = DateTime.UtcNow

let inline throwOnParsingError result = 
    result 
    |> Result.defaultWith (fun () -> invalidArg "entity" "could not parse database entity to domain")
    
