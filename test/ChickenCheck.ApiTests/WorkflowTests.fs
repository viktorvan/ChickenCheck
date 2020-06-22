module ChickenCheck.ApiTests.WorkflowTests

open System
open ChickenCheck.Backend
open Swensen.Unquote
open Expecto
open ChickenCheck.Shared
open FsToolkit.ErrorHandling

let testChicken1 = ChickenId.create (Guid.Parse("b65e8809-06dd-4338-a55e-418837072c0f"))
let testChicken2 = ChickenId.create (Guid.Parse("dedc5301-b404-49f4-8d5c-7bfac10c6950"))
let date = NotFutureDate.create DateTime.UtcNow
let yesterday = DateTime.UtcNow.AddDays(-1.) |> NotFutureDate.create
let mockDb =
    { new Database.IChickenStore with
        member this.GetAllChickens () = failwith ""
        member this.GetEggCount chickens date  = failwith ""
        member this.GetTotalEggCount(chickens: ChickenId list) = failwith ""
        member this.AddEgg chicken date = failwith ""
        member this.RemoveEgg chicken date = failwith "" }

let withChickenStore f =
    async {
        let dbFile = Guid.NewGuid().ToString("N") + ".db"
        use testDb = new TestDb(dbFile)
        return! f (testDb.ChickenStore :> Database.IChickenStore)
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
    
let addEggTests =
    testList "AddEgg" [
        yield! testFixtureAsync withChickenStore [
            "Adding eggs, increases count",
            fun (chickenStore: Database.IChickenStore) ->
                async {
                    // arrange
                    let! beforeCount = 
                        chickenStore.GetEggCount [testChicken1] date
                        |> Async.map (fun x -> x.[testChicken1])
                    // act
                    do! addEggs chickenStore 3 date testChicken1
                    // assert
                    let! newCount = 
                        chickenStore.GetEggCount [testChicken1] date
                        |> Async.map (fun x -> x.[testChicken1])
                    let added = newCount.Value - beforeCount.Value
                    added =! 3
                }
            "Adding eggs to one chicken, does not increase count of another",
            fun chickenStore ->
                async {
                    do! addEggs chickenStore 1 date testChicken1 
                    let! count = chickenStore.GetEggCount [ testChicken1; testChicken2 ] date
                    count.[testChicken1] =! EggCount.create(1)
                    count.[testChicken2] =! EggCount.zero
                }
        ]
    ]
    
let removeEggTests =
    testList "RemoveEgg" [
        yield! testFixtureAsync withChickenStore [
            "Removing an egg when count is already zero keeps count at zero",
            fun chickenStore ->
                async {
                    do! Workflows.removeEgg chickenStore testChicken1 date
                    let! count = chickenStore.GetEggCount [ testChicken1 ] date
                    count.[testChicken1] =! EggCount.zero
                }
            "Removing egg decreases count",
            fun chickenStore ->
                async {
                    do! addEggs chickenStore 3 date testChicken1 
                    do! removeEggs chickenStore 2 date testChicken1 
                    let! count = chickenStore.GetEggCount [ testChicken1 ] date
                    count.[testChicken1] =! EggCount.create(1)
                }
        ]
    ]
    
let getAllChickensTests =
    testList "GetAllChickens" [
        yield! testFixtureAsync withChickenStore [
            "Returns chickens with egg counts for a date and the total count",
            fun chickenStore ->
                async {
                    // arrange
                    let! initialChickens = Workflows.getAllChickens chickenStore date
                        
                    let chickenIds =
                        initialChickens
                        |> List.map (fun r -> r.Chicken.Id)
                    
                    // add eggs for date and date -1 day
                    do! chickenIds
                        |> List.map (fun id ->
                            async {
                                do! addEggs chickenStore 1 date id
                                do! addEggs chickenStore 1 yesterday id
                            })
                        |> Async.Sequential
                        |> Async.map ignore
                        
                    // act
                    let! newChickens = Workflows.getAllChickens chickenStore date
                    
                    // assert
                    // all chickens have 1 egg on date
                    newChickens
                    |> List.iter (fun r ->
                        r.Count =! (date, EggCount.create(1)))
                    // all chickens have 2 eggs total
                    newChickens
                    |> List.iter (fun r ->
                        r.TotalCount =! EggCount.create(2))
                }
        ]
    ]
            
[<Tests>]
let tests = testList "Workflows" [
        addEggTests
        removeEggTests
        getAllChickensTests
    ]
