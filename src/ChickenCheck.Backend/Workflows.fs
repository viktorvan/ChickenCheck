module ChickenCheck.Backend.Workflows

open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open ChickenCheck.Shared
        
let getAllChickens (chickenStore: Database.IChickenStore) =
    let getEggCounts chickenIds date  =
        async {
            let! eggCountA = chickenStore.GetEggCount chickenIds date |> Async.StartChild
            let! totalEggCountA = chickenStore.GetTotalEggCount chickenIds |> Async.StartChild
            
            let! eggCount = eggCountA
            let! totalEggCount = totalEggCountA
            return eggCount, totalEggCount
        }
          
    fun (date: NotFutureDate) ->
        async {
            let! chickens = chickenStore.GetAllChickens()
            let chickenIds = chickens |> List.map (fun c -> c.Id)
            
            let! eggCounts = getEggCounts chickenIds date 
            
            return
                chickens
                |> List.map (ChickenWithEggCount.create date eggCounts)
        }
        
let addEgg (chickenStore: Database.IChickenStore) =
    fun chicken (date: NotFutureDate) ->
        chickenStore.AddEgg chicken date
        
let removeEgg (chickenStore: Database.IChickenStore) =
    fun chicken (date: NotFutureDate) ->
        chickenStore.RemoveEgg chicken date
    
