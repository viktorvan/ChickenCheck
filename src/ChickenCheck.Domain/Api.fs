namespace ChickenCheck.Domain
open System
open ChickenCheck.Domain.Commands
open Session

type CreateSessionApi = CreateSession -> AsyncResult<Session, DomainError>
type GetChickensApi = SecureRequest<unit> -> AsyncResult<Chicken list, DomainError>

type IChickenCheckApi =
    { GetStatus: unit -> Async<string>
      CreateSession: CreateSessionApi 
      GetChickens: GetChickensApi }

module Api =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/api/%s/%s" typeName methodName

