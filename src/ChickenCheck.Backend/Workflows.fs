module ChickenCheck.Backend.Workflows

open ChickenCheck.Backend.Authentication
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open ChickenCheck.Domain

let createSession (tokenService: ITokenService) (userDb: Database.IUserStore) =
    let (|Valid|Invalid|) (hash, password) =
        match tokenService.VerifyPasswordHash (hash, password) with
        | true -> Valid
        | false -> Invalid

    fun (email, password) ->
        asyncResult {
            let! user = userDb.GetUserByEmail email
            let! user = user |> Result.requireSome UserDoesNotExist
            let token = tokenService.GenerateUserToken user.Name

            match (user.PasswordHash, password) with
            | Valid ->
                return Session.create user token
            | Invalid ->
                return! Error PasswordIncorrect
        }
        
let getAllChickens (chickenStore: Database.IChickenStore) =
    let toChickenWithEggCount (countOnDate, totalCount) chicken =
        let onDate = Map.tryFindWithDefault EggCount.zero chicken.Id countOnDate 
        let total = Map.tryFindWithDefault EggCount.zero chicken.Id totalCount
        { Chicken = chicken
          OnDate = onDate
          Total = total }
        
    let getEggCounts chickenIds date  =
        async {
            let! eggCountA = chickenStore.GetEggCount chickenIds date |> Async.StartChild
            let! totalEggCountA = chickenStore.GetTotalEggCount chickenIds |> Async.StartChild
            
            let! eggCount = eggCountA
            let! totalEggCount = totalEggCountA
            return eggCount, totalEggCount
        }
          
    fun (date: Date) ->
        async {
            let! chickens = chickenStore.GetAllChickens()
            let chickenIds = chickens |> List.map (fun c -> c.Id)
            
            let! eggCounts = getEggCounts chickenIds date 
            
            return
                chickens
                |> List.map (toChickenWithEggCount eggCounts)
        }
        
let getEggCount (chickenStore: Database.IChickenStore) =
    fun chickens date  ->
        chickenStore.GetEggCount chickens date
        
let addEgg (chickenStore: Database.IChickenStore) =
    fun chicken date ->
        chickenStore.AddEgg chicken date
        
let removeEgg (chickenStore: Database.IChickenStore) =
    fun chicken date ->
        chickenStore.RemoveEgg chicken date
    