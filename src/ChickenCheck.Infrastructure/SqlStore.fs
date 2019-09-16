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
                | ChickenEvent ev -> 
                    match ev with
                    | EggAdded ev -> SqlChickenStore.addEgg connection (ev.ChickenId, ev.Date)

        asyncResult {
            try
                return! events |> List.map handleEvent |> List.sequenceAsyncResultM
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }
