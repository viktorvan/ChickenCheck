module ChickenCheck.UnitTests.WorkflowTests

open ChickenCheck.Backend
open Expecto
open Swensen.Unquote
open FsToolkit.ErrorHandling

let date = NotFutureDate.today()
let yesterday = date |> NotFutureDate.addDays -1
let noEggs = EggCount.zero
let oneEgg = EggCount.create 1
let twoEggs = EggCount.create 2

let withChickenDb f =
    async {
        use testDb = new TestDb()
        return! f testDb
    }
    
let testFixtureAsync setup =
    Seq.map (fun (name, partialTest) ->
    testCaseAsync name <| setup partialTest)
    
let addEggs (addEgg: Database.AddEgg) count date chickenId  =
    async {
        let! _ =
            [ for _ in 1..count do
                  addEgg chickenId date ]
            |> Async.Sequential
        return ()
    }
    
let removeEggs (removeEgg: Database.RemoveEgg) count date chickenId  =
    async {
        let! _ =
            [ for _ in 1..count do
                  removeEgg chickenId date ]
            |> Async.Sequential
        return ()
    }
    
let getAllChickens (db: TestDb) date = Workflows.getAllChickens db.GetAllChickens db.GetEggCount db.GetTotalEggCount date
    
let addEggTests =
    testList "AddEgg" [
        yield! testFixtureAsync withChickenDb [
            "Adding eggs, increases count",
            fun db ->
                async {
                    // arrange
                    let! beforeCount = 
                        db.GetEggCount [DbChickens.bjork.Id] date
                        |> Async.map (fun x -> x.[DbChickens.bjork.Id])
                    // act
                    let expectedAdded = 3
                    do! addEggs db.AddEgg expectedAdded date DbChickens.bjork.Id
                    // assert
                    let! newCount = 
                        db.GetEggCount [DbChickens.bjork.Id] date
                        |> Async.map (fun x -> x.[DbChickens.bjork.Id])
                    let added = newCount.Value - beforeCount.Value
                    test <@ added = expectedAdded @>
                }
            "Adding eggs to one chicken, does not increase count of another",
            fun db ->
                async {
                    do! addEggs db.AddEgg 1 date DbChickens.bjork.Id 
                    let! count = db.GetEggCount [ DbChickens.bjork.Id; DbChickens.bodil.Id ] date
                    test <@ count.[DbChickens.bjork.Id] = oneEgg @>
                    test <@ count.[DbChickens.bodil.Id] = noEggs @>
                }
        ]
    ]
    
let removeEggTests =
    testList "RemoveEgg" [
        yield! testFixtureAsync withChickenDb [
            "Removing an egg when count is already zero keeps count at zero",
            fun db ->
                async {
                    do! Workflows.removeEgg db.RemoveEgg DbChickens.bjork.Id date
                    let! count = db.GetEggCount [ DbChickens.bjork.Id ] date
                    test <@ count.[DbChickens.bjork.Id] = noEggs @>
                }
            "Removing egg decreases count",
            fun db ->
                async {
                    do! addEggs db.AddEgg 3 date DbChickens.bjork.Id 
                    do! removeEggs db.RemoveEgg 2 date DbChickens.bjork.Id 
                    let! count = db.GetEggCount [ DbChickens.bjork.Id ] date
                    test <@ count.[DbChickens.bjork.Id] = oneEgg @>
                }
        ]
    ]
    
let getAllChickensTests =
    testList "GetAllChickens" [
        yield! testFixtureAsync withChickenDb [
            "Returns chickens with egg counts for a date and the total count",
            fun db ->
                async {
                    // arrange
                    let! initialChickens = getAllChickens db date
                        
                    let chickenIds =
                        initialChickens
                        |> List.map (fun r -> r.Chicken.Id)
                    
                    // add eggs for date and date -1 day
                    do! chickenIds
                        |> List.map (fun id ->
                            async {
                                do! addEggs db.AddEgg 1 date id
                                do! addEggs db.AddEgg 1 yesterday id
                            })
                        |> Async.Sequential
                        |> Async.map ignore
                        
                    // act
                    let! newChickens = getAllChickens db date
                    
                    // assert
                    // all chickens have 1 egg on date
                    newChickens
                    |> List.iter (fun r ->
                        test <@ r.Count = (date, oneEgg) @>)
                    // all chickens have 2 eggs total
                    newChickens
                    |> List.iter (fun r ->
                        test <@ r.TotalCount = twoEggs @>)
                }
        ]
    ]
    
[<Tests>]
let tests = testList "Workflows" [
        addEggTests
        removeEggTests
        getAllChickensTests
    ]
