namespace ChickenCheck.Domain
open ChickenCheck.Domain.Commands
open Session

type CreateSessionApi = CreateSession -> AsyncResult<Session, DomainError>
type GetChickensApi = SecureRequest<unit> -> AsyncResult<Chicken list, DomainError>
type GetEggCountOnDateApi = SecureRequest<Date> -> AsyncResult<Map<ChickenId, NaturalNum>, DomainError>
type GetTotalEggCountApi = SecureRequest<unit> -> AsyncResult<Map<ChickenId, NaturalNum>, DomainError>
type AddEggApi = SecureRequest<AddEgg> -> AsyncResult<unit, DomainError>
type RemoveEggApi = SecureRequest<RemoveEgg> -> AsyncResult<unit, DomainError>

type IChickenCheckApi =
    { GetStatus: unit -> Async<string>
      CreateSession: CreateSessionApi 
      GetChickens: GetChickensApi 
      GetEggCountOnDate : GetEggCountOnDateApi
      GetTotalEggCount : GetTotalEggCountApi
      AddEgg : AddEggApi 
      RemoveEgg : RemoveEggApi }

module Api =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/api/%s/%s" typeName methodName

