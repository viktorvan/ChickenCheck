module ChickenCheck.Backend.Routing

open ChickenCheck.Shared
let eggsPage (date: NotFutureDate) =
    date.ToString()
    |> sprintf "/eggs/%s" 
