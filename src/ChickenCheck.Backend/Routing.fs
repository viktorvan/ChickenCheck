module ChickenCheck.Backend.Routing

let eggsPage (date: NotFutureDate) =
    date.ToString()
    |> sprintf "/eggs/%s" 
