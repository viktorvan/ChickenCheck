module ChickenCheck.Infrastructure.SqlChickenStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Infrastructure.SqlHelpers
open Store.Chicken


type internal GetChickensSql = SqlCommandProvider<"
                        SELECT TOP 100 c.Id, c.Name, c.Breed, c.ImageUrl FROM Chicken c
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

type internal GetEggCountOnDateSql = SqlCommandProvider<"
                            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount FROM Chicken c
                            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                                             AND e.Date = @date
                            GROUP BY c.Id
                            ", DevConnectionString>

let getEggCountOnDate (ConnectionString conn) : GetEggCountOnDate =
    let toDomain (entity: GetEggCountOnDateSql.Record) =
        result {
            let! chickenId = entity.ChickenId |> ChickenId.create |> Result.mapError toDatabaseError
            let! eggsOrZero = Option.defaultValue 0 entity.EggCount |> NaturalNum.create |> Result.mapError toDatabaseError
            return chickenId, eggsOrZero
        }

    fun date ->
        let date = System.DateTime(date.Year, date.Month, date.Day)
        asyncResult {
            try
                use cmd = new GetEggCountOnDateSql(conn)
                let! result = cmd.AsyncExecute(date)
                let! mapped = result |> Seq.map toDomain |> Seq.toList |> List.sequenceResultM
                return mapped |> Map.ofList
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }

type internal GetTotalEggCountSql = SqlCommandProvider<"
                            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount FROM Chicken c
                            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                            GROUP BY c.Id
                            ", DevConnectionString>

let getTotalEggCount (ConnectionString conn) : GetTotalEggCount =
    let toDomain (entity: GetTotalEggCountSql.Record) =
        result {
            let! id = entity.ChickenId |> ChickenId.create |> Result.mapError toDatabaseError
            let! count = entity.EggCount |> Option.defaultValue 0 |> NaturalNum.create |> Result.mapError toDatabaseError
            return id, count
        }

    fun () ->
        asyncResult {
            try
                use cmd = new GetTotalEggCountSql(conn)
                let! result = cmd.AsyncExecute()
                let! mapped = result |> Seq.map toDomain |> Seq.toList |> List.sequenceResultM
                return mapped |> Map.ofList
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }

type internal AddEggSql = SqlCommandProvider<"
                            DECLARE @chickenId UNIQUEIDENTIFIER 
                            SET @chickenId = @theChickenId
                            DECLARE @date DATE
                            SET @date = @theDate
                            DECLARE @now DATETIME2(0)
                            SET @now = @theNow

                            DECLARE @oldCount INT 

                            SELECT @oldCount = ISNULL((SELECT e.eggCount FROM Egg e
                            WHERE e.ChickenId = @chickenId
                            AND e.Date = @date),0)

                            IF @oldCount = 0
                            BEGIN
                                INSERT INTO Egg 
                                    (ChickenId, [Date], EggCount, Created, LastModified)
                                    VALUES (@chickenId, @date, 1, @now, @now)
                            END
                            ELSE
                            BEGIN
                                UPDATE Egg SET EggCount = @oldcount + 1
                                WHERE ChickenId = @chickenId
                                AND Date = @date
                            END
                            ", DevConnectionString>

let addEgg (ConnectionString conn) : AddEgg =
    fun (chickenId, (date: Date)) ->
        let date = System.DateTime(date.Year, date.Month, date.Day)
        asyncResult {
            try
                use cmd = new AddEggSql(conn)
                let! _ = cmd.AsyncExecute(chickenId.Value, date, System.DateTime.Now)
                return ()
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }

type internal RemoveEggSql = SqlCommandProvider<"
                            DECLARE @chickenId UNIQUEIDENTIFIER 
                            SET @chickenId = @theChickenId
                            DECLARE @date DATE
                            SET @date = @theDate
                            DECLARE @now DATETIME2(0)
                            SET @now = @theNow

                            DECLARE @oldCount INT 

                            SELECT @oldCount = ISNULL((SELECT e.eggCount FROM Egg e
                            WHERE e.ChickenId = @chickenId
                            AND e.Date = @date),0)

                            IF @oldCount > 1
                            BEGIN
                                UPDATE Egg SET EggCount = @oldcount - 1
                                WHERE ChickenId = @chickenId
                                AND Date = @date
                            END
                            ELSE
                            BEGIN
                                DELETE FROM Egg
                                WHERE ChickenId = @chickenId
                                AND Date = @date
                            END
                            ", DevConnectionString>

let removeEgg (ConnectionString conn) : RemoveEgg =
    fun (chickenId, (date: Date)) ->
        let date = System.DateTime(date.Year, date.Month, date.Day)
        asyncResult {
            try
                use cmd = new RemoveEggSql(conn)
                let! _ = cmd.AsyncExecute(chickenId.Value, date, System.DateTime.Now)
                return ()
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }
