module ChickenCheck.Client.CompositionRoot


open ChickenCheck.Client
open ChickenCheck.Domain
open Fable.Remoting.Client
open Elmish
open ChickenCheck.Client.ApiHelpers

let chickenApi : IChickenApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Api.routeBuilder
    #if !DEBUG
    |> Remoting.withBaseUrl "https://chickencheck-functions.azurewebsites.net"
    #endif
    |> Remoting.buildProxy<IChickenApi>

let chickenCardApi token : ChickenCard.Api =
    let addEgg =
        fun cmd ->
            callSecureApi
                token
                chickenApi.AddEgg
                cmd
                (fun _ -> ChickenCard.Msg.AddedEgg)
                ChickenCard.AddEggFailed

    let removeEgg =
        fun cmd ->
            callSecureApi
                token
                chickenApi.RemoveEgg
                cmd
                (fun _ -> ChickenCard.Msg.RemovedEgg)
                ChickenCard.RemoveEggFailed

    { AddEgg = addEgg
      RemoveEgg = removeEgg }

let signinApi : Signin.Api =
    let ofSuccess result =
        match result with
        | Ok session -> Signin.Msg.LoginCompleted session
        | Error err ->
            let msg =
                match err with
                | Login l ->
                    match l with
                    | UserDoesNotExist -> "Användaren saknas"
                    | PasswordIncorrect -> "Fel lösenord"
                | _ -> GeneralErrorMsg
            msg |> Signin.Msg.AddError

    let createSession =
        fun cmd ->
            Cmd.OfAsync.either
                chickenApi.CreateSession
                cmd
                ofSuccess
                (handleApiError Signin.Msg.AddError)

    { CreateSession = createSession }


let chickensApi token : Chickens.Api =
    let getChickens =
        fun () ->
            callSecureApi
                token
                chickenApi.GetChickens 
                () 
                Chickens.FetchedChickens 
                Chickens.AddError 

    let getTotalCount =
        fun () -> 
            callSecureApi
                token
                chickenApi.GetTotalEggCount
                ()
                Chickens.FetchedTotalCount
                Chickens.AddError 

    let getCountOnDate =
        fun date -> 
            callSecureApi
                token
                chickenApi.GetEggCountOnDate 
                date 
                (fun res -> Chickens.FetchedEggCountOnDate (date, res)) 
                Chickens.AddError 
    

    { GetChickens = getChickens 
      GetTotalCount = getTotalCount
      GetCountOnDate = getCountOnDate }