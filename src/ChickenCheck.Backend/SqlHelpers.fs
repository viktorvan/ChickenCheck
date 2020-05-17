module ChickenCheck.Backend.SqlHelpers

open ChickenCheck.Domain
open System
open System.Data.SqlClient
open FsToolkit.ErrorHandling
open Dapper

let now() = DateTime.UtcNow

let inline throwOnParsingError result = 
    result 
    |> Result.defaultWith (fun () -> invalidArg "entity" "could not parse database entity to domain")

let (!) p = Some (box p)

[<AutoOpen>]
module DbHelpers =
    let getConnection (ConnectionString str) =
        async {
            let conn = new SqlConnection(str)
            let! _ = conn.OpenAsync() |> Async.AwaitTask
            return conn
        }

    let execute (conn: SqlConnection) (sql: string) parameters =
        match parameters with
        | None -> conn.ExecuteAsync(sql) |> Async.AwaitTask
        | Some p -> conn.ExecuteAsync(sql, p) |> Async.AwaitTask

    let query<'T> (conn: SqlConnection) (sql: string) parameters =
        async {
            let! results =
                match parameters with
                | None -> conn.QueryAsync<'T>(sql) |> Async.AwaitTask
                | Some p -> conn.QueryAsync<'T>(sql, p) |> Async.AwaitTask
            return List.ofSeq results
        }

    let querySingle<'T> (conn: SqlConnection) (sql: string) parameters =
        async {
            let! result = query<'T> conn sql parameters
            return List.tryHead result
        }

