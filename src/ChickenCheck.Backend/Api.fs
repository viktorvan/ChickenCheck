namespace ChickenCheck.Backend
open ChickenCheck.Domain


type IChickenApi =
    { CreateSession: (Email * Password) -> AsyncResult<Session, DomainError> 
      GetAllChickens: SecureRequest<unit> -> AsyncResult<Chicken list, DomainError>
      GetEggCountOnDate: SecureRequest<Date> -> AsyncResult<Map<ChickenId, EggCount>, DomainError>
      GetTotalEggCount: SecureRequest<unit> -> AsyncResult<Map<ChickenId, EggCount>, DomainError>
      AddEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, DomainError> 
      RemoveEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, DomainError> }

module Api =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/api/%s/%s" typeName methodName

