// Learn more about F# at http://fsharp.org

open System
open SimpleMigrations
open SimpleMigrations.DatabaseProvider
open SimpleMigrations.Console
open Argu
open System.Reflection
open System.Data.SqlClient


type CLIArguments =
    | ConnectionString of connectionString : string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | ConnectionString _ -> "specify the connection string to use"

let parser = ArgumentParser.Create<CLIArguments>(programName = "ChickenCheck.Migrations.exe")


let printUsage() = parser.PrintUsage() |> printfn "%s"


[<EntryPoint>]
let main argv =
        let args = parser.Parse argv
        match args.TryGetResult ConnectionString with
        | Some connectionString ->
            let migrationAssembly = Assembly.GetEntryAssembly()
            use connection = new SqlConnection(connectionString)
            let provider = MssqlDatabaseProvider(connection)
            let migrator = SimpleMigrator(migrationAssembly, provider)
            let consoleRunner = ConsoleRunner(migrator)
            "up" |> Array.singleton |> consoleRunner.Run
        | None -> 
            printUsage()
            0