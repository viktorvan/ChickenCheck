module ChickenCheck.Infrastructure.SqlUserStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Infrastructure.SqlHelpers
open Store.User

type internal GetUserByEmailSql = SqlCommandProvider<"
                        SELECT TOP 2 Id, Name, Email, PasswordHash, Salt FROM [USER]
                        WHERE Email = @Email
                        ", DevConnectionString, SingleRow=true>

let getUserByEmail (ConnectionString conn) : GetUserByEmail =
    let toDomain (entity:GetUserByEmailSql.Record) =
        result {
            let hash = entity.PasswordHash |> PasswordHash.toByteArray
            let salt = entity.Salt |> PasswordHash.toByteArray
            let! email = entity.Email |> Email.create |> Result.mapError toDatabaseError
            let! name = entity.Name |> String200.create "name" |> Result.mapError toDatabaseError
            let id = entity.Id |> UserId
            return 
                { User.Id = id
                  Name = name
                  Email = email
                  PasswordHash = { Hash = hash; Salt = salt} }
        }

    fun email ->
        asyncResult {
            try
                use cmd = new GetUserByEmailSql(conn)
                let! result = cmd.AsyncExecute(email.Value)
                return! result |> Option.map toDomain |> Option.sequenceResult

            with exn -> return! exn.ToString() |> DatabaseError |> Error
    }
