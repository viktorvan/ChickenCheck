module ChickenCheck.Infrastructure.SqlChickenStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Infrastructure.SqlHelpers
open Store.Chicken


type internal GetChickensSql = SqlCommandProvider<"
                        SELECT TOP 100 c.Id, c.Name, c.Breed, c.ImageUrl, SUM(e.EggCount) AS EggCount FROM Chicken c
                        INNER JOIN Egg e on e.ChickenId = c.Id
                        GROUP BY c.Id, c.Name, c.Breed, c.ImageUrl
                        ORDER BY c.Name
                        ", DevConnectionString>

let getChickens (ConnectionString conn) : GetChickens =
    let toDomain (entity:GetChickensSql.Record) =
        result {
            let id = entity.Id |> ChickenId
            let! name = 
                entity.Name 
                |> String200.create "name" 
                |> Result.mapError toDatabaseError
            let! breed = 
                entity.Breed 
                |> String200.create "breed" 
                |> Result.mapError toDatabaseError
            let! eggCount = 
                entity.EggCount 
                |> Option.defaultValue 0 
                |> NaturalNum.create 
                |> Result.mapError toDatabaseError
            let! imageUrl = 
                entity.ImageUrl 
                |> Option.map (ImageUrl.create >> (Result.mapError toDatabaseError)) 
                |> Option.sequenceResult
            return 
                { Chicken.Id = id
                  Name = name
                  Breed = breed
                  ImageUrl = imageUrl 
                  TotalEggCount = eggCount }
        }

    fun () ->
        asyncResult {
            try
                use cmd = new GetChickensSql(conn)
                let! result = cmd.AsyncExecute()
                return! result |> Seq.map toDomain |> Seq.toList |> List.sequenceResultM
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }

type internal GetEggsOnDateSql = SqlCommandProvider<"
                            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount FROM Chicken c
                            INNER JOIN Egg e on e.ChickenId = c.Id
                            WHERE e.Date = @date
                            GROUP BY c.Id
                            ", DevConnectionString>

let getEggsOnDate (ConnectionString conn) : GetEggsOnDate =
    let toDomain (entity: GetEggsOnDateSql.Record) =
        result {
            let! chickenId = entity.ChickenId |> ChickenId.create |> Result.mapError toDatabaseError
            let! eggsOrZero = Option.defaultValue 0 entity.EggCount |> NaturalNum.create |> Result.mapError toDatabaseError
            return chickenId, eggsOrZero
        }

    fun date ->
        asyncResult {
            try
                use cmd = new GetEggsOnDateSql(conn)
                let! result = cmd.AsyncExecute(date)
                return! result |> Seq.map toDomain |> Seq.toList |> List.sequenceResultM
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }



