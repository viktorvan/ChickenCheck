namespace ChickenCheck.Backend
open ChickenCheck.Domain
open ChickenCheck.Domain.Commands


type IChickenApi =
    { CreateSession: CreateSession -> AsyncResult<Session, DomainError> 
      Query: SecureRequest<Queries.DomainQuery> -> AsyncResult<Queries.Response, DomainError> 
      Command: SecureRequest<Commands.DomainCommand> -> AsyncResult<unit, DomainError> }

module Api =
    let routeBuilder (typeName: string) (methodName: string) =
        sprintf "/api/%s/%s" typeName methodName

