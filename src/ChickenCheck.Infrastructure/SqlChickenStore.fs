module ChickenCheck.Infrastructure.SqlChickenStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Infrastructure.SqlHelpers

type internal GetChickensSql = SqlCommandProvider<"
                        SELECT TOP 100 c.Id, c.Name, c.Breed, c.ImageUrl FROM Chicken c
                        ORDER BY c.Name
                        ", DevConnectionString>

let getChickens (ConnectionString conn) =
    let toDomain (entity:GetChickensSql.Record) =
        result {
            let! id = entity.Id |> ChickenId.create
            let! name = 
                entity.Name 
                |> String200.create "name" 
            let! breed = 
                entity.Breed 
                |> String200.create "breed" 
            let! imageUrl = 
                entity.ImageUrl 
                |> Option.traverseResult ImageUrl.create 
            return 
                { Chicken.Id = id
                  Name = name
                  Breed = breed
                  ImageUrl = imageUrl }
        } |> throwOnParsingError

    fun () ->
        async {
            use cmd = GetChickensSql.Create(conn)
            let! result = cmd.AsyncExecute()
            return 
                result 
                |> Seq.map toDomain 
                |> Seq.toList 
        }

type internal GetEggCountOnDateSql = SqlCommandProvider<"
                            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount FROM Chicken c
                            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                                             AND e.Date = @date
                            GROUP BY c.Id
                            ", DevConnectionString>

let getEggCountOnDate (ConnectionString conn) =
    let toDomain (entity: GetEggCountOnDateSql.Record) =
        result {
            let! chickenId = 
                entity.ChickenId 
                |> ChickenId.create 
            let! eggsOrZero = 
                Option.defaultValue 0 entity.EggCount 
                |> NaturalNum.create 
                |> Result.map EggCount
            return chickenId, eggsOrZero
        } |> throwOnParsingError

    fun { Date.Year = year; Month = month; Day = day } ->
        let dateTime = System.DateTime(year, month, day)
        async {
            use cmd = GetEggCountOnDateSql.Create(conn)
            let! result = cmd.AsyncExecute(dateTime)
            return 
                result 
                |> Seq.map toDomain 
                |> Map.ofSeq
        }

type internal GetTotalEggCountSql = SqlCommandProvider<"
                            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount FROM Chicken c
                            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                            GROUP BY c.Id
                            ", DevConnectionString>

let getTotalEggCount (ConnectionString conn) =
    let toDomain (entity: GetTotalEggCountSql.Record) =
        result {
            let! id = 
                entity.ChickenId 
                |> ChickenId.create 
            let! count = 
                entity.EggCount 
                |> Option.defaultValue 0 
                |> NaturalNum.create 
                |> Result.map EggCount 
            return id, count
        } |> throwOnParsingError

    fun () ->
        async {
            use cmd = GetTotalEggCountSql.Create(conn)
            let! result = cmd.AsyncExecute()
            return 
                result 
                |> Seq.map toDomain 
                |> Map.ofSeq
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

let addEgg (ConnectionString conn) =
    fun (ChickenId id, { Year = year; Month = month; Day = day }) ->
        let date = System.DateTime(year, month, day)
        async {
            use cmd = AddEggSql.Create(conn)
            return!
                cmd.AsyncExecute(id, date, System.DateTime.Now) 
                |> Async.Ignore
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

let removeEgg (ConnectionString conn) =
    fun (ChickenId id, (date: Date)) ->
        let date = System.DateTime(date.Year, date.Month, date.Day)
        async {
            use cmd = RemoveEggSql.Create(conn)
            return! cmd.AsyncExecute(id, date, System.DateTime.Now) |> Async.Ignore
        }
