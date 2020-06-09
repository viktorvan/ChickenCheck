namespace ChickenCheck.Client.Chickens

open ChickenCheck.Client
open ChickenCheck.Shared
open Elmish

// Types

type ChickenDetails =
    { Id: ChickenId
      Name : string
      ImageUrl : ImageUrl option 
      Breed : string
      TotalEggCount : EggCount
      EggCountOnDate : EggCount
      IsLoading : bool }
type ChickensPageModel =
    { Chickens : Deferred<Map<ChickenId, ChickenDetails>>
      CurrentDate : NotFutureDate
      Errors : string list }
type ChickenMsg =
    | GetAllChickens of AsyncOperationStatus<NotFutureDate, Result<ChickenWithEggCount list, string>>
    | GetEggCount of AsyncOperationStatus<NotFutureDate * ChickenId list, Result<NotFutureDate * Map<ChickenId, EggCount>, string>>
    | ChangeDate of NotFutureDate
    | AddEgg of AsyncOperationStatus<ChickenId * NotFutureDate, Result<ChickenId * NotFutureDate, ChickenId * string>> 
    | RemoveEgg of AsyncOperationStatus<ChickenId * NotFutureDate, Result<ChickenId * NotFutureDate, ChickenId * string>>
    | AddError of string
    | ClearErrors
    | NoOp

type IChickenApiCmds =
    abstract GetAllChickens : NotFutureDate -> Cmd<ChickenMsg> 
    abstract GetEggCount : NotFutureDate * ChickenId list -> Cmd<ChickenMsg> 
    abstract AddEgg: ChickenId * NotFutureDate -> Cmd<ChickenMsg> 
    abstract RemoveEgg: ChickenId * NotFutureDate -> Cmd<ChickenMsg>
    
// Implementations
    
module ChickensPageModel =
    let init date =
        { Chickens = InProgress
          Errors = []
          CurrentDate = date }

type ChickenApiCmds(api: IChickenApi, editApi: IChickenEditApi) =
    let getAllChickensWithEggs date =
        async {
            try
                let! result = api.GetAllChickensWithEggs(date)
                return Ok result |> Finished |> GetAllChickens
            with exn ->
                return Error exn.Message |> Finished |> GetAllChickens
        }
    let getEggCountOnDate date chickens =
        async {
            try
                let! result = api.GetEggCount(date, chickens)
                return Ok (date, result) |> Finished |> GetEggCount
            with exn ->
                return (Error exn.Message |> Finished |> GetAllChickens)
        }
            
    let addEgg id date =
        async {
            try
                let! _ = editApi.AddEgg(id, date)
                return Ok (id, date) |> Finished |> AddEgg
            with exn ->
                return Error (id, exn.Message) |> Finished |> AddEgg
        }
            
    let removeEgg id date =
        async {
            try
                let! _ = editApi.RemoveEgg(id, date)
                return Ok (id, date) |> Finished |> RemoveEgg
            with exn ->
                return Error (id, exn.Message) |> Finished |> RemoveEgg
        }
            
    interface IChickenApiCmds with
    
        member __.GetAllChickens(date) =
            getAllChickensWithEggs date 
            |> Cmd.OfAsync.result
                
        member __.GetEggCount(date, chickens) =
            getEggCountOnDate date chickens
            |> Cmd.OfAsync.result
        member __.AddEgg(id, date) =
            addEgg id date
            |> Cmd.OfAsync.result
        
        member __.RemoveEgg(id, date) = 
            removeEgg id date
            |> Cmd.OfAsync.result
            

