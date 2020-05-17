module ChickenCheck.Client.Tests.ChickenCardTests

open Swensen.Unquote
open Expecto
open ChickenCheck.Domain
open ChickenCheck.Client
open System
open TestHelpers
open FsToolkit.ErrorHandling

let chickenId = 
    Guid.NewGuid() 
    |> ChickenId.create 
    |> Result.okValue
    
let chicken =
    { Chicken.Breed = "Appenzeller" |> String200.create "breed" |> Result.okValue
      Chicken.Id = chickenId
      Name = "Siouxie Sioux" |> String200.create "name" |> Result.okValue
      ImageUrl = None }
    
let onDateCount =
    EggCount.zero
    |> EggCount.increase |> Result.okValue
    
let totalCount =
    EggCount.zero
    |> EggCount.increase |> Result.okValue
    |> EggCount.increase |> Result.okValue
    
let model, cmds = Chickens.init

[<Tests>]
let tests =
    testList "Chickens.update" [
        test "On FetchedChickensWithEggs stores chicken details in map" {
            let (newModel, _) = Chickens.update (FetchedChickensWithEggs [ { Chicken = chicken; OnDate = onDateCount; Total = totalCount } ]) model
            let expectedDetails =
                { Id = chicken.Id
                  Name = chicken.Name
                  Breed = chicken.Breed
                  ImageUrl = chicken.ImageUrl
                  TotalEggCount = totalCount
                  EggCountOnDate = onDateCount }
            let expectedModel =
                { model with Chickens = [ (chicken.Id, expectedDetails) ] |> Map.ofList }
            newModel =! expectedModel
        }
    ]