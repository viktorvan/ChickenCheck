module ChickenCheck.Backend.Routing

open ChickenCheck.Shared
let chickensPage (date: NotFutureDate) =
    date.ToString()
    |> sprintf "/chickens?date=%s" 
