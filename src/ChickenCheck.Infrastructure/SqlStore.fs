module ChickenCheck.Infrastructure.SqlStore

open ChickenCheck.Domain
open ChickenCheck.Domain.Events
open FsToolkit.ErrorHandling
open FSharp.Data
open System


let appendEvents connection : AppendEvents =
    fun events ->
        let handleEvent =
            fun event ->
                match event with
                | ChickenEvent ev -> notImplemented()

        asyncResult {
            try
                return! events |> List.map handleEvent |> List.sequenceAsyncResultM
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }
