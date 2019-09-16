namespace ChickenCheck.Domain

open ChickenCheck.Domain
open ChickenCheck.Domain.Events
open System

type ConnectionString = ConnectionString of string
type AppendEvents = DomainEvent list -> AsyncResult<unit list, DatabaseError>


module Store =
    module User =
        type GetUserByEmail = Email -> AsyncResult<User option, DatabaseError>
    module Chicken =
        type GetChickens = unit -> AsyncResult<Chicken list, DatabaseError>
        type GetTotalEggCount = unit -> AsyncResult<Map<ChickenId, NaturalNum>, DatabaseError>
        type GetEggCountOnDate = Date -> AsyncResult<Map<ChickenId, NaturalNum>, DatabaseError>
        type AddEgg = ChickenId * Date -> AsyncResult<unit, DatabaseError>
