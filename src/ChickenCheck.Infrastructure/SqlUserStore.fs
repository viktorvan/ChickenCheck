module ChickenCheck.Infrastructure.SqlUserStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Infrastructure.SqlHelpers


type internal GetUserByEmailSql = SqlCommandProvider<"
                        SELECT TOP 2 Id, Name, Email, PasswordHash, Salt FROM [USER]
                        WHERE Email = @Email
                        ", DevConnectionString, SingleRow=true>

let getUserByEmail (ConnectionString conn) =
    let toDomain (entity:GetUserByEmailSql.Record) =
        result {
            let hash = entity.PasswordHash |> PasswordHash.toByteArray
            let salt = entity.Salt |> PasswordHash.toByteArray
            let! email = entity.Email |> Email.create 
            let! name = entity.Name |> String200.create "name"
            let! id = entity.Id |> UserId.create 
            return 
                { User.Id = id
                  Name = name
                  Email = email
                  PasswordHash = { Hash = hash; Salt = salt} }
        } |> throwOnParsingError
        
    fun (Email email) ->
        async {
            use cmd = GetUserByEmailSql.Create(conn)
            let! result = cmd.AsyncExecute(email)
            return Option.map toDomain result
        }
        
