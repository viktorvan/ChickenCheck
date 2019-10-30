module ChickenCheck.Domain.ChickenCommandHandler

open ChickenCheck.Domain

let handleAddEgg : Commands.AddEgg -> Events.ChickenEvent =
    fun cmd ->
        Events.EggAdded
            { ChickenId = cmd.ChickenId
              Date = cmd.Date }

let handleRemoveEgg : Commands.RemoveEgg -> Events.ChickenEvent =
    fun cmd ->
        Events.EggRemoved
            { ChickenId = cmd.ChickenId
              Date = cmd.Date }
