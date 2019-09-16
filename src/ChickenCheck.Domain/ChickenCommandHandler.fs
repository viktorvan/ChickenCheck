module ChickenCheck.Domain.ChickenCommandHandler

open ChickenCheck.Domain
open System


type AddEgg = Commands.AddEgg -> Events.ChickenEvent

let addEgg : AddEgg =
    fun cmd ->
        Events.EggAdded
            { ChickenId = cmd.ChickenId
              Date = cmd.Date }
