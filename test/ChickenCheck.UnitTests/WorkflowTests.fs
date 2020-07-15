module ChickenCheck.UnitTests.WorkflowTests

open ChickenCheck.Backend
open Expecto
open Swensen.Unquote
open ChickenCheck.Shared
open FsToolkit.ErrorHandling

let date = NotFutureDate.today()
let yesterday = date |> NotFutureDate.addDays -1
let noEggs = EggCount.zero
let oneEgg = EggCount.create 1
let twoEggs = EggCount.create 2

let withChickenStore f =
    async {
        use testDb = new TestDb()
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
                        chickenStore.GetEggCount [DbChickens.bjork.Id] date
                        |> Async.map (fun x -> x.[DbChickens.bjork.Id])
                    // act
                    do! addEggs chickenStore 3 date DbChickens.bjork.Id
                    // assert
                    let! newCount = 
                        chickenStore.GetEggCount [DbChickens.bjork.Id] date
                        |> Async.map (fun x -> x.[DbChickens.bjork.Id])
                    let added = newCount.Value - beforeCount.Value
                    test <@ added = 3 @>
                }
            "Adding eggs to one chicken, does not increase count of another",
            fun chickenStore ->
                async {
                    do! addEggs chickenStore 1 date DbChickens.bjork.Id 
                    let! count = chickenStore.GetEggCount [ DbChickens.bjork.Id; DbChickens.bodil.Id ] date
                    test <@ count.[DbChickens.bjork.Id] = oneEgg @>
                    test <@ count.[DbChickens.bodil.Id] = noEggs @>
                }
        ]
    ]
    
let removeEggTests =
    testList "RemoveEgg" [
        yield! testFixtureAsync withChickenStore [
            "Removing an egg when count is already zero keeps count at zero",
            fun chickenStore ->
                async {
                    do! Workflows.removeEgg chickenStore DbChickens.bjork.Id date
                    let! count = chickenStore.GetEggCount [ DbChickens.bjork.Id ] date
                    test <@ count.[DbChickens.bjork.Id] = noEggs @>
                }
            "Removing egg decreases count",
            fun chickenStore ->
                async {
                    do! addEggs chickenStore 3 date DbChickens.bjork.Id 
                    do! removeEggs chickenStore 2 date DbChickens.bjork.Id 
                    let! count = chickenStore.GetEggCount [ DbChickens.bjork.Id ] date
                    test <@ count.[DbChickens.bjork.Id] = oneEgg @>
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
