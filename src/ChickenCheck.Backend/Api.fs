namespace ChickenCheck.Backend
open ChickenCheck.Domain

type GetAllChickensResponse =
    { Chicken: Chicken
      OnDate: EggCount
      Total: EggCount }

type IChickenApi =
    { CreateSession: (Email * Password) -> AsyncResult<Session, LoginError> 
      GetAllChickensWithEggs: SecureRequest<Date> -> AsyncResult<GetAllChickensResponse list, AuthenticationError>
      GetEggCountOnDate: SecureRequest<Date> -> AsyncResult<Map<ChickenId, EggCount>, AuthenticationError>
      AddEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, AuthenticationError> 
      RemoveEgg: SecureRequest<ChickenId * Date> -> AsyncResult<unit, AuthenticationError> }

module Api =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/api/%s/%s" typeName methodName

