namespace ChickenCheck.Backend

open ChickenCheck.Shared

module ChickenWithEggCount =
    let create date (countOnDate, totalCount) chicken =
        let onDate = Map.tryFindWithDefault EggCount.zero chicken.Id countOnDate 
        let total = Map.tryFindWithDefault EggCount.zero chicken.Id totalCount
        { Chicken = chicken
          Count = date, onDate
          TotalCount = total }
