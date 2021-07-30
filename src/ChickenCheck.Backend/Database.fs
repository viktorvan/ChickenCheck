[<RequireQualifiedAccess>]
module ChickenCheck.Backend.Database

open System
open FsToolkit.ErrorHandling
open Dapper
open Npgsql
open ChickenCheck.Backend.Extensions


type TestDatabaseAccess = unit -> Async<unit>
type AddEgg = ChickenId -> NotFutureDate -> Async<unit>
type GetAllChickens = unit -> Async<Chicken list>
type GetChicken = ChickenId -> Async<Chicken option>
type GetEggCount = ChickenId list -> NotFutureDate -> Async<Map<ChickenId, EggCount>>
type GetTotalEggCount = ChickenId list -> Async<Map<ChickenId, EggCount>>
type RemoveEgg = ChickenId -> NotFutureDate -> Async<unit>


[<AutoOpen>]
module private DbHelpers =
    let getConnection (ConnectionString str) =
        async {
            let conn = new NpgsqlConnection(str)
            let! _ = conn.OpenAsync() |> Async.AwaitTask
            return conn
        }

    let execute (conn: NpgsqlConnection) (sql: string) parameters =
        match parameters with
        | None -> conn.ExecuteAsync(sql) |> Async.AwaitTask
        | Some p -> conn.ExecuteAsync(sql, p) |> Async.AwaitTask

    let query<'T> (conn: NpgsqlConnection) (sql: string) parameters =
        async {
            let! results =
                match parameters with
                | None -> conn.QueryAsync<'T>(sql) |> Async.AwaitTask
                | Some p -> conn.QueryAsync<'T>(sql, p) |> Async.AwaitTask
            return List.ofSeq results
        }

    let querySingle<'T> (conn: NpgsqlConnection) (sql: string) parameters =
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
    
let testConnection (conn: ConnectionString) =
    let sql = """SELECT c.id FROM Chicken c LIMIT 1"""
    fun () ->
        async {
            use! connection = getConnection conn
            let! result = query connection sql None
            if result.Length <> 1 then 
                failwith "Test database connection failed"
            else 
                ()
        }

[<CLIMutable>]
type private ChickenEntity =
    { Id: Guid
      Name: string
      Breed: string
      ImageUrl: string option }

let private toDomainChicken (entity: ChickenEntity) =
    result {
        let id = entity.Id |> ChickenId.create
        let! name = 
            entity.Name 
            |> String.notNullOrEmpty
            |> Result.requireSome "Name cannot be empty"
        let! breed = 
            entity.Breed 
            |> String.notNullOrEmpty
            |> Result.requireSome "Breed cannot be empty"
        let! imageUrl = 
            entity.ImageUrl 
            |> Option.traverseResult ImageUrl.create 
        return 
            { Chicken.Id = id
              Name = name
              Breed = breed
              ImageUrl = imageUrl }
    } |> throwOnParsingError

let getAllChickens (conn: ConnectionString) : GetAllChickens =
    let sql = """SELECT c.Id, c.Name, c.Breed, c.ImageUrl 
                 FROM Chicken c
                 ORDER BY c.Name"""

    fun () ->
        async {
            use! connection = getConnection conn
            let! result = query connection sql None
            return 
                result 
                |> Seq.map toDomainChicken
                |> Seq.toList 
        }
        
let getChicken (conn: ConnectionString) : GetChicken =
    let sql = """SELECT c.Id, c.Name, c.Breed, c.ImageUrl 
                 FROM Chicken c
                 WHERE c.Id = @chickenId
                 LIMIT 2"""
                 
    fun (ChickenId chickenId) ->
        async {
            use! connection = getConnection conn
            let! result = querySingle connection sql !{| chickenId = chickenId |}
            return
                result
                |> Option.map toDomainChicken
        }

type private EggCountEntity =
    { ChickenId: Guid
      EggCount: int64 option } 
    
module private EggCountEntity =
    let toDomain (entity: EggCountEntity) =
        let chickenId = 
            entity.ChickenId
            |> ChickenId.create
        let eggsOrZero = 
            Option.defaultValue 0L entity.EggCount |> int
            |> EggCount.create
        chickenId, eggsOrZero
        
let getEggCount (conn: ConnectionString) : GetEggCount =
    let sql = """
            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount 
            FROM Chicken c
            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
                                  AND e.Date = @date
            WHERE e.ChickenId = ANY(@chickenIds)
            GROUP BY c.Id"""
            
    let toDomain (entity: EggCountEntity list) =
        entity
        |> List.map EggCountEntity.toDomain
        |> Map.ofList

    fun (chickenIds: ChickenId list) (date: NotFutureDate) ->
        let chickenGuidIds = chickenIds |> List.map ChickenId.value |> List.toArray
        async {
            use! connection = getConnection conn
            let! entities = query connection sql !{| date = date.ToDateTime(); chickenIds = chickenGuidIds |}
            let result = toDomain entities
            return 
                chickenIds 
                |> Seq.map (fun id -> id, Map.tryFindWithDefault EggCount.zero id result)
                |> Map.ofSeq
        }
        
let getTotalEggCount (conn: ConnectionString) : GetTotalEggCount =
    let sql = """
            SELECT c.Id AS ChickenId, Sum(e.EggCount) AS EggCount 
            FROM Chicken c
            LEFT OUTER JOIN Egg e ON e.ChickenId = c.Id
            WHERE e.ChickenId = ANY(@chickenIds)
            GROUP BY c.Id"""

    fun chickenIds ->
        let chickenGuidIds = chickenIds |> List.map ChickenId.value |> List.toArray
        async {
            use! connection = getConnection conn
            let! result = query connection sql !{| chickenIds = chickenGuidIds |}
            return 
                result 
                |> Seq.map EggCountEntity.toDomain 
                |> Map.ofSeq
        }
        
let private validChickenIds conn =
    getAllChickens conn ()
    |> Async.map (List.map (fun c -> c.Id))

let addEgg (conn: ConnectionString) : AddEgg =
    let sql = """
            INSERT INTO Egg 
            ( ChickenId
            , Date
            , EggCount
            , Created
            , LastModified
            )
            VALUES 
            ( @chickenId
            , @date
            , 1
            , NOW()
            , NOW()
            )
            ON CONFLICT (ChickenId, Date)
            DO UPDATE SET 
                EggCount = Egg.EggCount + 1, 
                LastModified = NOW()
            """
            
    fun (id: ChickenId) (date: NotFutureDate) ->
        async {
            let! validChickenIds = validChickenIds conn
            if not (List.contains id validChickenIds) then 
                return invalidArg "ChickenId" "Invalid chicken-id"
            else
                use! connection = getConnection conn
                let! _ = execute connection sql !{| date = date.ToDateTime(); chickenId = id.Value |}
                return ()
        }

let removeEgg (conn: ConnectionString) : RemoveEgg =
    let sql = """
            UPDATE Egg 
                SET 
                    EggCount = EggCount - 1, 
                    LastModified = NOW()
                WHERE ChickenId = @chickenId
                AND Date = @date;
                  
            DELETE FROM Egg 
            WHERE ChickenId = @chickenId
            AND Date = @date
            AND EggCount < 1;"""

    fun (id: ChickenId) (date: NotFutureDate) ->
        async {
            let! validChickenIds = validChickenIds conn
            if not (List.contains id validChickenIds) then 
                return invalidArg "ChickenId" "Invalid chicken-id"
            else
                use! connection = getConnection conn
                let! _ = execute connection sql !{| chickenId = id.Value; date = date.ToDateTime() |}
                return ()
        }
        
type Database(connString) =
    member this.TestDatabaseAccess = testConnection connString
    member this.AddEgg = addEgg connString
    member this.GetAllChickens = getAllChickens connString
    member this.GetEggCount = getEggCount connString
    member this.GetTotalEggCount = getTotalEggCount connString
    member this.RemoveEgg = removeEgg connString
    member this.GetChicken = getChicken connString
 