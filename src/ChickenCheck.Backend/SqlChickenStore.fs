module ChickenCheck.Backend.SqlChickenStore

open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Backend.SqlHelpers
open System


type ChickenEntity =
    { Id: Guid
      Name: string
      Breed: string
      ImageUrl: string option }

let getChickens (conn: ConnectionString) =
    let sql = """
                SELECT TOP 100 c.Id, c.Name, c.Breed, c.ImageUrl FROM Chicken c
                ORDER BY c.Name"""

    let toDomain (entity:ChickenEntity) =
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
            use! connection = getConnection conn
            let! result = query connection sql None
            return 
                result 
                |> Seq.map toDomain 
                |> Seq.toList 
        }

type EggCountEntity =
    { ChickenId: Guid
      EggCount: int option }

let getEggCountOnDate (conn: ConnectionString) =
    let sql = """
            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount FROM Chicken c
            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                             AND e.Date = @date
            GROUP BY c.Id"""
    let toDomain (entity: EggCountEntity) =
        result {
            let! chickenId = 
                entity.ChickenId 
                |> ChickenId.create 
            let eggsOrZero = 
                Option.defaultValue 0 entity.EggCount 
                |> NaturalNum.create 
                |> EggCount
            return chickenId, eggsOrZero
        } |> throwOnParsingError

    fun { Date.Year = year; Month = month; Day = day } ->
        let dateTime = System.DateTime(year, month, day)
        async {
            use! connection = getConnection conn
            let! result = query connection sql !{| date = dateTime |}
            return 
                result 
                |> Seq.map toDomain 
                |> Map.ofSeq
        }

let getTotalEggCount (conn: ConnectionString) =
    let sql = """
                                SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount FROM Chicken c
                            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                            GROUP BY c.Id"""
    let toDomain (entity: EggCountEntity) =
        result {
            let! id = 
                entity.ChickenId 
                |> ChickenId.create 
            let count = 
                entity.EggCount 
                |> Option.defaultValue 0 
                |> NaturalNum.create 
                |> EggCount 
            return id, count
        } |> throwOnParsingError

    fun () ->
        async {
            use! connection = getConnection conn 
            let! result = query connection sql None
            return 
                result 
                |> Seq.map toDomain
                |> Map.ofSeq
        }


let addEgg (conn: ConnectionString) =
    let sql = """
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
        END"""

    fun (ChickenId id, { Year = year; Month = month; Day = day }) ->
        let date = System.DateTime(year, month, day)
        async {
            use! connection = getConnection conn
            return! execute connection sql !{| date = date; chickenId = id; now = now() |} |> Async.Ignore
        }

let removeEgg (conn: ConnectionString) =
    let sql = """
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
            END"""

    fun (ChickenId id, (date: Date)) ->
        let date = System.DateTime(date.Year, date.Month, date.Day)
        async {
            use! connection = getConnection conn
            return! execute connection sql !{| chickenId = id; date = date |} |> Async.Ignore
        }
