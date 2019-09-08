namespace ChickenCheck.Domain

open ChickenCheck.Domain
open ChickenCheck.Domain.Events

type ConnectionString = ConnectionString of string
type AppendEvents = DomainEvent list -> AsyncResult<unit list, DatabaseError>


module Store =
    module User =
        type GetUserByEmail = Email -> AsyncResult<User option, DatabaseError>
    module Chicken =
        type GetChickens = unit -> AsyncResult<Chicken list, DatabaseError>
