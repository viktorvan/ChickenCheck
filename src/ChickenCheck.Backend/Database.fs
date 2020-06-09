[<RequireQualifiedAccess>]
module ChickenCheck.Backend.Database

open ChickenCheck.Shared
open System
open Microsoft.Data.Sqlite
open FsToolkit.ErrorHandling
open Dapper

type ConnectionString = private ConnectionString of string
module ConnectionString =
    let create str =
        if String.IsNullOrEmpty str then invalidArg "ConnectionString" "ConnectionString cannot be empty"
        else str |> ConnectionString


[<AutoOpen>]
module private DbHelpers =
    let getConnection (ConnectionString str) =
        async {
            let conn = new SqliteConnection(str)
            let! _ = conn.OpenAsync() |> Async.AwaitTask
            return conn
        }

    let execute (conn: SqliteConnection) (sql: string) parameters =
        match parameters with
        | None -> conn.ExecuteAsync(sql) |> Async.AwaitTask
        | Some p -> conn.ExecuteAsync(sql, p) |> Async.AwaitTask

    let query<'T> (conn: SqliteConnection) (sql: string) parameters =
        async {
            let! results =
                match parameters with
                | None -> conn.QueryAsync<'T>(sql) |> Async.AwaitTask
                | Some p -> conn.QueryAsync<'T>(sql, p) |> Async.AwaitTask
            return List.ofSeq results
        }

    let querySingle<'T> (conn: SqliteConnection) (sql: string) parameters =
        async {
            let! result = query<'T> conn sql parameters
            return List.tryHead result
        }
        
    let (!) p = Some (box p)

let inline private throwOnParsingError result =
    let throw err =
        sprintf "Could not parse database entity to domain: %s" err
        |> invalidArg "entity"
    result |> Result.valueOr throw

type private ChickenEntity =
    { Id: string
      Name: string
      Breed: string
      ImageUrl: string option }

let getAllChickens (conn: ConnectionString) =
    let sql = """SELECT c.Id, c.Name, c.Breed, c.ImageUrl 
                FROM Chicken c
                 ORDER BY c.Name"""

    let toDomain (entity:ChickenEntity) =
        result {
            let id = entity.Id |> ChickenId.parse
            let name = 
                entity.Name 
                |> String.notNullOrEmpty
            let breed = 
                entity.Breed 
                |> String.notNullOrEmpty
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

type private EggCountEntity =
    { ChickenId: string
      EggCount: int64 option } 
    
module private EggCountEntity =
    let toDomain (entity: EggCountEntity) =
        let chickenId = 
            entity.ChickenId
            |> ChickenId.parse
        let eggsOrZero = 
            Option.defaultValue 0L entity.EggCount |> int
            |> EggCount.create
        chickenId, eggsOrZero
        
let getEggCount (conn: ConnectionString) =
    let sql = """
            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount 
            FROM Chicken c
            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                                  AND e.Date = @date
            WHERE e.ChickenId in @chickenIds
            GROUP BY c.Id"""
            
    let toDomain (entity: EggCountEntity list) =
        entity
        |> List.map EggCountEntity.toDomain
        |> Map.ofList

    fun (chickenIds: ChickenId list) { Year = year; Month = month; Day = day } ->
        let dateTime = System.DateTime(year, month, day)
        let chickenStringIds = chickenIds |> List.map (fun (ChickenId id) -> id.ToString())
        async {
            use! connection = getConnection conn
            let! entities = query connection sql !{| date = dateTime; chickenIds = chickenStringIds |}
            let result = toDomain entities
            return 
                chickenIds 
                |> Seq.map (fun id -> id, Map.tryFindWithDefault EggCount.zero id result)
                |> Map.ofSeq
        }
        
let getTotalEggCount (conn: ConnectionString) =
    let sql = """
            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount 
            FROM Chicken c
            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
            WHERE e.ChickenId in @chickenIds
            GROUP BY c.Id"""

    fun chickenIds ->
        let chickenStringIds = chickenIds |> List.map (fun (c:ChickenId) -> c.Value.ToString())
        async {
            use! connection = getConnection conn
            let! result = query connection sql !{| chickenIds = chickenStringIds |}
            return 
                result 
                |> Seq.map EggCountEntity.toDomain 
                |> Map.ofSeq
        }
        
let addEgg (conn: ConnectionString) =
    let sql = """
            UPDATE Egg 
                SET 
                    EggCount = EggCount + 1, 
                    LastModified = date('now')
                WHERE ChickenId = @chickenId
                AND Date = @date;
                  
            INSERT INTO Egg 
            ( ChickenId
            , Date
            , EggCount
            , Created
            , LastModified
            )
            SELECT @chickenId, @date, 1 , date('now') , date('now')
            WHERE (SELECT Changes() = 0);"""

    fun (ChickenId id) date ->
        let date = NotFutureDate.toDateTime date
        async {
            use! connection = getConnection conn
            let! _ = execute connection sql !{| date = date; chickenId = id.ToString() |}
            return ()
        }

let removeEgg (conn: ConnectionString) =
    let sql = """
            UPDATE Egg 
                SET 
                    EggCount = EggCount - 1, 
                    LastModified = date('now')
                WHERE ChickenId = @chickenId
                AND Date = @date;
                  
            DELETE FROM Egg 
            WHERE ChickenId = @chickenId
            AND Date = @date
            AND EggCount < 1;"""

    fun (ChickenId id) date ->
        let date = NotFutureDate.toDateTime date
        async {
            use! connection = getConnection conn
            let! _ = execute connection sql !{| chickenId = id.ToString(); date = date |}
            return ()
        }

type IChickenStore =
    abstract GetAllChickens: unit -> Async<Chicken list>
    abstract GetEggCount: ChickenId list -> NotFutureDate -> Async<Map<ChickenId, EggCount>>
    abstract GetTotalEggCount: ChickenId list -> Async<Map<ChickenId, EggCount>>
    abstract AddEgg: ChickenId -> NotFutureDate -> Async<unit>
    abstract RemoveEgg: ChickenId -> NotFutureDate -> Async<unit>
    
    
type ChickenStore(connectionString) =
    interface IChickenStore with
        member this.GetAllChickens () = getAllChickens connectionString ()
        member this.GetEggCount chickens date  = getEggCount connectionString chickens date
        member this.GetTotalEggCount(chickens: ChickenId list) = getTotalEggCount connectionString chickens
        member this.AddEgg chicken date = addEgg connectionString chicken date
        member this.RemoveEgg chicken date = removeEgg connectionString chicken date
