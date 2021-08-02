module ChickenCheck.Backend.Workflows

open ChickenCheck.Backend
open FsToolkit.ErrorHandling
        
let getAllChickensWithEggCounts (getChickens: Database.GetAllChickens) (getEggCount: Database.GetChickenEggCount) (getTotalEggCount: Database.GetChickenTotalEggCount) =
    let getEggCounts chickenIds date  =
        async {
            let! eggCountA = getEggCount chickenIds date |> Async.StartChild
            let! totalEggCountA = getTotalEggCount chickenIds |> Async.StartChild
            
            let! eggCount = eggCountA
            let! totalEggCount = totalEggCountA
            return eggCount, totalEggCount
        }
          
    fun (date: NotFutureDate) ->
        async {
            let! chickens = getChickens()
            let chickenIds = chickens |> List.map (fun c -> c.Id)
            
            let! eggCounts = getEggCounts chickenIds date 
            
            return
                chickens
                |> List.map (ChickenWithEggCount.create date eggCounts)
        }
        
let getChickenWithEggCount (getChicken: Database.GetChicken) (getEggCount: Database.GetChickenEggCount) =
    fun chickenId date ->
        async {
            let! chickenA = getChicken chickenId |> Async.StartChild
            let! eggCountA = getEggCount [ chickenId ] date |> Async.StartChild
            
            let! chicken = chickenA
            let! eggCount = eggCountA
            return
                chicken
                |> Option.map (fun c ->
                    {| Name = c.Name
                       Breed = c.Breed
                       ImageUrl = c.ImageUrl
                       EggCount = eggCount.[c.Id] |})
        }

let getTotalEggCountOnDate (getEggCount: Database.GetTotalEggCount) date =
    getEggCount date
    
let addEgg (addEggToDb: Database.AddEgg) chicken date =
    addEggToDb chicken date
        
let removeEgg (removeEgg: Database.RemoveEgg) =
    fun chicken (date: NotFutureDate) ->
        removeEgg chicken date
        
let healthCheck (testDbAccess: Database.TestDatabaseAccess) =
    fun () ->
        async {
            do! testDbAccess()
            return System.DateTime.Now
        }
