module ChickenCheck.Client.ApiCommands

open ChickenCheck.Domain
open Elmish

type ChickenApiCmds(api: IChickenApi) =
    let handleAuthError onSuccess result =
        async {
            match! result with
            | Ok value -> return onSuccess value
            | Error err -> return LoginFailed err
        }
        
    let handleApiError (exn: exn) =
        ApiError exn.Message
        |> Async.retn
    let getAllChickensWithEggs token date =
        try
            api.GetAllChickensWithEggs(SecureRequest.create token date)
            |> handleAuthError (Finished >> GetAllChickens >> ChickenMsg)
        with exn ->
            handleApiError exn
    let getEggCountOnDate token date chickens =
        try
            api.GetEggCount(SecureRequest.create token (date, chickens))
            |> handleAuthError (fun res -> Finished (date, res) |> GetEggCount |> ChickenMsg)
        with exn ->
            handleApiError exn
            
    let addEgg token id date =
        try
            api.AddEgg(SecureRequest.create token (id,date))
            |> handleAuthError (fun _ -> Finished(id, date) |> AddEgg |> ChickenMsg)
        with exn ->
            handleApiError exn
            
    let removeEgg token id date =
        try
            api.RemoveEgg(SecureRequest.create token (id,date))
            |> handleAuthError (fun _ -> Finished(id, date) |> RemoveEgg |> ChickenMsg)
        with exn ->
            handleApiError exn
            
    interface IChickenApiCmds with
    
        member __.GetAllChickens(token, date) =
            getAllChickensWithEggs token date 
            |> Cmd.OfAsync.result
                
        member __.GetEggCount(token, date, chickens) =
            getEggCountOnDate token date chickens
            |> Cmd.OfAsync.result
        member __.AddEgg(token, id, date) =
            addEgg token id date
            |> Cmd.OfAsync.result
        
        member __.RemoveEgg(token, id, date) = 
            removeEgg token id date
            |> Cmd.OfAsync.result
            

module SessionApiCmds =
    let createSession api email pw =
        async {
            try
                match! api.CreateSession(email,pw) with
                | Ok session -> return SignedIn session
                | Error loginError -> return loginError |> (Finished >> SignIn >> SessionMsg)
            with exn ->
                return ApiError exn.Message
        }
    
type SessionApiCmds(api: IChickenApi) =
        
    interface ISessionApiCmds with
        member __.Login(email, pw) =
            SessionApiCmds.createSession api email pw
            |> Cmd.OfAsync.result
       

