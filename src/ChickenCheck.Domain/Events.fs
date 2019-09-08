module ChickenCheck.Domain.Events

open ChickenCheck.Domain

type LaidEgg =
    { ChickenId : ChickenId
      Date: System.DateTime }

type ChickenEvent =
    | LaidEgg of LaidEgg

type DomainEvent =
    | ChickenEvent of ChickenEvent