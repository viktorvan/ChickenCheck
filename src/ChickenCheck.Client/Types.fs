namespace ChickenCheck.Client
open ChickenCheck.Shared
open FsToolkit.ErrorHandling

type Deferred<'T> =
    | HasNotStartedYet
    | InProgress
    | Resolved of 'T

type AsyncOperationStatus<'T1,'T2> =
    | Start of 'T1
    | Finished of 'T2

/// Utility functions around `Deferred<'T>` types.
module Deferred =
    let map (transform: 'T -> 'U) (deferred: Deferred<'T>) : Deferred<'U> =
        match deferred with
        | HasNotStartedYet -> HasNotStartedYet
        | InProgress -> InProgress
        | Resolved value -> Resolved (transform value)

    /// Returns whether the `Deferred<'T>` value has been resolved or not.
    let resolved value =
        match value with
        | HasNotStartedYet -> false
        | InProgress -> false
        | Resolved _ -> true

    /// Returns whether the `Deferred<'T>` value is in progress or not.
    let inProgress = function
        | HasNotStartedYet -> false
        | InProgress -> true
        | Resolved _ -> false

    /// Verifies that a `Deferred<'T>` value is resolved and the resolved data satisfies a given requirement.
    let exists (predicate: 'T -> bool) = function
        | HasNotStartedYet -> false
        | InProgress -> false
        | Resolved value -> predicate value

    let defaultValue defValue deferred =
        match deferred with
        | HasNotStartedYet -> defValue
        | InProgress -> defValue
        | Resolved value -> value

    let ofOption deferred =
        match deferred with
        | HasNotStartedYet
        | InProgress -> None
        | Resolved value -> Some value

type DeferredResult<'T1,'T2> = Deferred<Result<'T1, 'T2>>
module DeferredResult =
    let map f = Deferred.map (Result.map f)
    let mapError f = Deferred.map (Result.mapError f)
    let defaultValue defValue deferred =
        match deferred with
        | HasNotStartedYet -> defValue
        | InProgress -> defValue
        | Resolved (Error _) -> defValue
        | Resolved (Ok value) -> value
    let ofOption deferredResult =
        match deferredResult with
        | HasNotStartedYet
        | InProgress -> None
        | Resolved (Ok value) -> Some value
        | Resolved (Error _) -> None

type StringInput<'a> =
    | Valid of 'a
    | Invalid of string
    | Empty
    
module StringInput = 
    let inline create createFunc =
        fun msg ->
            match createFunc msg with
            | Ok value -> StringInput.Valid value
            | Error _ -> StringInput.Invalid msg
    let inline tryValid input =
        match input with
        | StringInput.Valid a ->
            let value = (^a : (member Val : string) a)
            true, value
        | StringInput.Invalid value -> 
            false, value
        | StringInput.Empty -> 
            false, ""

    let inline isValid input = input |> tryValid |> fst
    
type UserProfile = 
    {
        AccessToken: JWT
        Name       : string
        Email      : string
        Picture    : string
        UserId     : string
    }
and JWT = string
