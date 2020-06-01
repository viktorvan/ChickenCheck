module ChickenCheck.ApiTests.WorkflowTests

open System.IO
open System.Reflection
open Swensen.Unquote
open System
open Expecto
open ChickenCheck.Domain
open ChickenCheck.Backend
open FsToolkit.ErrorHandling
open Microsoft.Data.Sqlite
open SimpleMigrations
open SimpleMigrations.DatabaseProvider

let testChicken1 = ChickenId.create (Guid.Parse("b65e8809-06dd-4338-a55e-418837072c0f"))
let testChicken2 = ChickenId.create (Guid.Parse("dedc5301-b404-49f4-8d5c-7bfac10c6950"))
let date = Date.create DateTime.UtcNow

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

let withChickenStore f =
    async {
        let dbFile = Guid.NewGuid().ToString("N") + ".db"
        use testDb = new TestDb(dbFile)
        return! f testDb.ChickenStore
    }
    
let testFixtureAsync setup =
    Seq.map (fun (name, partialTest) ->
    testCaseAsync name <| setup partialTest)
    
let addEggs chickenStore count date chickenId  =
    async {
        let! _ =
            [ for _ in 1..count do
                  Workflows.addEgg chickenStore chickenId date ]
            |> Async.Sequential
        return ()
    }
    
    
let removeEggs chickenStore count date chickenId  =
    async {
        let! _ =
            [ for _ in 1..count do
                  Workflows.removeEgg chickenStore chickenId date ]
            |> Async.Sequential
        return ()
    }
            
[<Tests>]
let tests = testList "Workflow tests" [
    yield! testFixtureAsync withChickenStore [
        "Without adding eggs, GetEggCount returns zero",
        fun chickenStore ->
            async {
                let! count = Workflows.getEggCount chickenStore [ testChicken1 ] date
                count.[testChicken1] =! EggCount.zero
            }
        "Adding eggs, increases count",
        fun chickenStore ->
            async {
                do! addEggs chickenStore 3 date testChicken1 
                let! count = Workflows.getEggCount chickenStore [ testChicken1 ] date
                let expected = EggCount.create 3
                count.[testChicken1] =! expected
            }
        "Adding eggs to one chicken, does not increase count of another",
        fun chickenStore ->
            async {
                do! addEggs chickenStore 1 date testChicken1 
                let! count = Workflows.getEggCount chickenStore [ testChicken1; testChicken2 ] date
                count.[testChicken1] =! EggCount.create(1)
                count.[testChicken2] =! EggCount.zero
            }
        "Removing an egg when count is already zero keeps count at zero",
        fun chickenStore ->
            async {
                do! Workflows.removeEgg chickenStore testChicken1 date
                let! count = Workflows.getEggCount chickenStore [ testChicken1 ] date
                count.[testChicken1] =! EggCount.zero
            }
        "Removing egg decreases count",
        fun chickenStore ->
            async {
                do! addEggs chickenStore 3 date testChicken1 
                do! removeEggs chickenStore 2 date testChicken1 
                let! count = Workflows.getEggCount chickenStore [ testChicken1 ] date
                count.[testChicken1] =! EggCount.create(1)
            }
        "GetAllChickens returns chickens with egg counts for a date and the total count",
        fun chickenStore ->
            async {
                let! allChickens = Workflows.getAllChickens chickenStore date
                let allChickens =
                    allChickens
                    |> List.map (fun r -> r.Chicken.Id, {| Chicken = r.Chicken; EggCount = r.OnDate; TotalEggCount = r.Total |})
                    |> Map.ofList
                    
                let chickenIds = Map.keys allChickens
                
                // add eggs for date and date -1 day
                let! _ =
                    chickenIds
                    |> List.map (fun id ->
                        async {
                            do! addEggs chickenStore 1 date id
                            do! addEggs chickenStore 1 (Date.create (date.ToDateTime().AddDays(-1.0))) id
                        })
                    |> Async.Sequential
                    
                let! newChickens = Workflows.getAllChickens chickenStore date
                
                // all chickens have 1 egg on date
                newChickens
                |> List.iter (fun r ->
                    r.OnDate =! EggCount.create(1))
                // all chickens have 2 eggs total
                newChickens
                |> List.iter (fun r ->
                    r.Total =! EggCount.create(2))
            }
    ]
]
