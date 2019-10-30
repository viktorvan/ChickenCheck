namespace ChickenCheck.Backend
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands


type IChickenApi =
    { GetStatus: unit -> Async<string>
      CreateSession: CreateSession -> AsyncResult<Session, DomainError> 
      GetChickens: SecureRequest<unit> -> AsyncResult<Chicken list, DomainError> 
      GetEggCountOnDate : SecureRequest<Date> -> AsyncResult<Map<ChickenId, EggCount>, DomainError>
      GetTotalEggCount : SecureRequest<unit> -> AsyncResult<Map<ChickenId, EggCount>, DomainError>
      AddEgg : SecureRequest<AddEgg> -> AsyncResult<unit, DomainError> 
      RemoveEgg : SecureRequest<RemoveEgg> -> AsyncResult<unit, DomainError> }

module Api =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/api/%s/%s" typeName methodName

