module ChickenCheck.Backend.SqlUserStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open Dapper
open ChickenCheck.Backend.SqlHelpers
open System


type internal UserEntity =
    { Id: Guid
      Name: string
      Email: string
      PasswordHash: string
      Salt: string }

let getUserByEmail (conn: ConnectionString) =
    let toDomain (entity:UserEntity) =
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
            let sql = """
                        SELECT TOP 2 Id, Name, Email, PasswordHash, Salt FROM [USER]
                        WHERE Email = @Email"""
            use! connection = getConnection conn
            let! result = querySingle connection sql !{| Email = email |}
            return Option.map toDomain result
        }
        
