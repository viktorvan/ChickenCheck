// Learn more about F# at http://fsharp.org

open System
open Microsoft.Data.Sqlite
open SimpleMigrations
open SimpleMigrations.DatabaseProvider
open SimpleMigrations.Console
open Argu
open System.Reflection

type CLIArguments =
    | ConnectionString of connectionString : string
    | To of string 
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | ConnectionString _ -> "specify the connection string to use"
            | To _ -> "Migrate to version"

let parser = ArgumentParser.Create<CLIArguments>(programName = "ChickenCheck.Migrations.exe")


let printUsage() = parser.PrintUsage() |> printfn "%s"

[<EntryPoint>]
let main argv =
        let args = parser.Parse argv

        match args.TryGetResult ConnectionString with
        | Some connectionString ->
            let migrationAssembly = Assembly.GetEntryAssembly()
            use connection = new SqliteConnection(connectionString)
            connection.Open()
            let provider = SqliteDatabaseProvider(connection)
            let migrator = SimpleMigrator(migrationAssembly, provider)
            let consoleRunner = ConsoleRunner(migrator)
            match args.TryGetResult To with
            | None -> "up" |> Array.singleton |> consoleRunner.Run
            | Some version -> [| "to"; version |] |> consoleRunner.Run
        | None -> 
            printUsage()
            0


//  /// <summary>
//         /// Assign <see cref="SubCommands"/> (and optionally <see cref="DefaultSubCommand"/>)
//         /// </summary>
//         protected virtual void CreateSubCommands()
//         {
//             this.SubCommands = new List<SubCommand>()
//             {
//                 new SubCommand()
//                 {
//                     Command = "up",
//                     Description = "up: Migrate to the latest version",
//                     Action = this.MigrateUp,
//                 },
//                 new SubCommand()
//                 {
//                     Command = "to",
//                     Description = "to <n>: Migrate up/down to the version n",
//                     Action = this.MigrateTo
//                 },
//                 new SubCommand()
//                 {
//                     Command = "reapply",
//                     Description = "reapply: Re-apply the current migration",
//                     Action = this.ReApply
//                 },
//                 new SubCommand()
//                 {
//                     Command = "list",
//                     Description = "list: List all available migrations",
//                     Action = this.ListMigrations
//                 },
//                 new SubCommand()
//                 {
//                     Command = "baseline",
//                     Description = "baseline <n>: Move the database to version n, without apply migrations",
//                     Action = this.Baseline
//                 },
//             };

//             this.DefaultSubCommand = this.SubCommands[0];
//         }