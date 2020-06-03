namespace ChickenCheck.ApiTests

open System
open System.IO
open System.Reflection
open ChickenCheck.Backend
open Microsoft.Data.Sqlite
open SimpleMigrations
open SimpleMigrations.DatabaseProvider

type TestDb(file) =
    let migrateDb connectionString =
        let migrationAssembly = Assembly.GetAssembly(typeof<ChickenCheck.Migrations.CreateUserTable>)
        let connection = new SqliteConnection(connectionString)
        connection.Open()
        let provider = SqliteDatabaseProvider(connection)
        let migrator = SimpleMigrator(migrationAssembly, provider)
        migrator.Load()
        migrator.MigrateToLatest()
        connection
        
    let connectionString = sprintf "Data Source=%s;" file
    let connection = migrateDb connectionString
    do OptionHandler.register()
    
    let chickenStore = Database.ChickenStore (Database.ConnectionString.create connectionString)
    
    member this.ChickenStore = chickenStore
    interface IDisposable with
        member this.Dispose() =
            connection.Close()
            File.Delete file

