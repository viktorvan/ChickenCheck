module ChickenCheck.Infrastructure.SqlChickenStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Infrastructure.SqlHelpers
open Store.Chicken


type internal GetChickensSql = SqlCommandProvider<"
                        SELECT TOP 100 Id, Name, Breed, ImageUrl FROM Chicken
                        ", DevConnectionString>

let getChickens (ConnectionString conn) : GetChickens =
    let toDomain (entity:GetChickensSql.Record) =
        result {
            let id = entity.Id |> ChickenId
            let! name = entity.Name |> String200.create "name" |> Result.mapError toDatabaseError
            let! breed = entity.Breed |> String200.create "breed" |> Result.mapError toDatabaseError
            let! imageUrl = 
                entity.ImageUrl 
                |> Option.map (ImageUrl.create >> (Result.mapError toDatabaseError)) 
                |> Option.sequenceResult
            return 
                { Chicken.Id = id
                  Name = name
                  Breed = breed
                  ImageUrl = imageUrl }
        }

    fun () ->
        asyncResult {
            try
                use cmd = new GetChickensSql(conn)
                let! result = cmd.AsyncExecute()
                return! result |> Seq.map toDomain |> Seq.toList |> List.sequenceResultM
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }
