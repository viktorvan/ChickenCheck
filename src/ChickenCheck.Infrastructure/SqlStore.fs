module ChickenCheck.Infrastructure.SqlStore

open ChickenCheck.Domain.Events
open FsToolkit.ErrorHandling
open FSharpPlus


let appendEvents connection (events: seq<DomainEvent>) =
    let handleEvent event =
        match event with
        | ChickenEvent ev -> 
            match ev with
            | EggAdded ev -> SqlChickenStore.addEgg connection (ev.ChickenId, ev.Date)
            | EggRemoved ev -> SqlChickenStore.removeEgg connection (ev.ChickenId, ev.Date)

    events
    |> traverse handleEvent
    |> Async.Ignore
    
