module ChickenCheck.Domain.Events

open ChickenCheck.Domain

type EggAdded =
    { ChickenId : ChickenId
      Date: Date }

type ChickenEvent =
    | EggAdded of EggAdded

type DomainEvent =
    | ChickenEvent of ChickenEvent