module ChickenCheck.Domain.Events

open ChickenCheck.Domain

type EggAdded =
    { ChickenId : ChickenId
      Date: Date }

type EggRemoved =
    { ChickenId : ChickenId
      Date: Date }

type ChickenEvent =
    | EggAdded of EggAdded
    | EggRemoved of EggRemoved

type DomainEvent =
    | ChickenEvent of ChickenEvent