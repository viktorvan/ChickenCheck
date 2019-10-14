namespace ChickenCheck.Client
open ChickenCheck.Domain
open System
open System.Collections.Generic
open ChickenCheck.Client

[<AutoOpen>]
module DomainError =
    let getClientErrorMsg error =
        match error with
        | Validation _ -> "Ogiltigt värde"
        | Database _ | Duplicate | NotFound | ConfigMissing _ -> "Serverfel"
        | Authentication _ | Login _ -> "Autentiseringsfel"
    type DomainError with
        member this.ErrorMsg = getClientErrorMsg this

type StringInput<'a> =
    | Valid of 'a
    | Invalid of string
    | Empty

module StringInput = 
    let inline tryValid input =
        match input with
        | Valid a ->
            let value = (^a : (member Value : string) a)
            true, value
        | Invalid value -> 
            false, value
        | Empty -> 
            false, ""

    let inline isValid input = input |> tryValid |> fst

module OptionalStringInput =
    let inline tryValid input = 
        match input with
        | Valid a ->
            let value = (^a : (member Value : string) a)
            true, value
        | Invalid value -> 
            false, value
        | Empty -> 
            true, ""

    let inline isValid input = input |> tryValid |> fst

type NumberInput<'a> =
    | Valid of 'a
    | Invalid of string
    | Empty

module NumberInput = 
    let inline tryValid input =
        match input with
        | Valid (NaturalNum value) ->
            true, (value |> string)
        | Invalid value -> 
            false, value
        | Empty -> 
            false, ""

    let inline isValid input = input |> tryValid |> fst

type ApiCallStatus =
    | NotStarted
    | Running
    | Completed
    | Failed of string

module Session =
    let expired = new Event<unit>()

    open Fable.SimpleJson
    let store (session : Session) =
        let json = Json.stringify session
        Browser.WebStorage.localStorage.setItem("session", json)

    let delete() =
        Browser.WebStorage.localStorage.removeItem("session")

    let tryGet() : Session option =
        match Browser.WebStorage.localStorage.getItem("session") with
        | null ->
            None

        | session ->
            match Json.tryParseAs session with
            | Ok session ->
                Some session
            | Error msg ->
                Logger.warning "Error when decoding the stored session"
                Logger.warning msg
                Logger.warning "Cleaning, stored session..."
                Browser.WebStorage.localStorage.removeItem("session")
                None
