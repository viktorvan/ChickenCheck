namespace ChickenCheck.Domain
open System
open ChickenCheck.Domain.Commands
open Session

type CreateSessionApi = CreateSession -> AsyncResult<Session, DomainError>
type GetChickensApi = SecureRequest<unit> -> AsyncResult<Chicken list, DomainError>
type GetEggsOnDateApi = SecureRequest<DateTime> -> AsyncResult<(ChickenId * NaturalNum) list, DomainError>

type IChickenCheckApi =
    { GetStatus: unit -> Async<string>
      CreateSession: CreateSessionApi 
      GetChickens: GetChickensApi 
      GetEggsOnDate : GetEggsOnDateApi}

module Api =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/api/%s/%s" typeName methodName

