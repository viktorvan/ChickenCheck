namespace ChickenCheck.Client.Chickens

open ChickenCheck.Client
open ChickenCheck.Shared
open Elmish

type Msg =
    | GetAllChickens of AsyncOperationStatus<NotFutureDate, Result<ChickenWithEggCount list, string>>
    | GetEggCount of AsyncOperationStatus<NotFutureDate * ChickenId list, Result<NotFutureDate * Map<ChickenId, EggCount>, string>>
    | ChangeDate of NotFutureDate
    | AddEgg of AsyncOperationStatus<ChickenId * NotFutureDate, Result<ChickenId * NotFutureDate, ChickenId * string>> 
    | RemoveEgg of AsyncOperationStatus<ChickenId * NotFutureDate, Result<ChickenId * NotFutureDate, ChickenId * string>>
    | AddError of string
    | ClearErrors

type IChickenApiCmds =
    abstract GetAllChickens : NotFutureDate -> Cmd<Msg> 
    abstract GetEggCount : NotFutureDate * ChickenId list -> Cmd<Msg> 
    abstract AddEgg: ChickenId * NotFutureDate -> Cmd<Msg> 
    abstract RemoveEgg: ChickenId * NotFutureDate -> Cmd<Msg>
    
module ChickenApiCmds =
    let getAllChickensWithEggs api date =
        async {
            try
                let! result = api.GetAllChickensWithEggs(date)
                return Ok result |> Finished |> GetAllChickens
            with exn ->
                return Error exn.Message |> Finished |> GetAllChickens
        }
    let getEggCountOnDate api date chickens =
        async {
            try
                let! result = api.GetEggCount(date, chickens)
                return Ok (date, result) |> Finished |> GetEggCount
            with exn ->
                return (Error exn.Message |> Finished |> GetAllChickens)
        }
            
    let addEgg editApi id date =
        async {
            try
                let! _ = editApi.AddEgg(id, date)
                return Ok (id, date) |> Finished |> AddEgg
            with exn ->
                return Error (id, exn.Message) |> Finished |> AddEgg
        }
            
    let removeEgg editApi id date =
        async {
            try
                let! _ = editApi.RemoveEgg(id, date)
                return Ok (id, date) |> Finished |> RemoveEgg
            with exn ->
                return Error (id, exn.Message) |> Finished |> RemoveEgg
        }
        
type ChickenApiCmds(api: IChickenApi, editApi: IChickenEditApi) =
            
    interface IChickenApiCmds with
    
        member __.GetAllChickens(date) =
            ChickenApiCmds.getAllChickensWithEggs api date 
            |> Cmd.OfAsync.result
                
        member __.GetEggCount(date, chickens) =
            ChickenApiCmds.getEggCountOnDate api date chickens
            |> Cmd.OfAsync.result
        member __.AddEgg(id, date) =
            ChickenApiCmds.addEgg editApi id date
            |> Cmd.OfAsync.result
        
        member __.RemoveEgg(id, date) = 
            ChickenApiCmds.removeEgg editApi id date
            |> Cmd.OfAsync.result
            
