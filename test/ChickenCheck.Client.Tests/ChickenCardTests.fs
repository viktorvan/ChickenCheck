module ChickenCheck.Client.Tests.ChickenCardTests

open Swensen.Unquote
open Expecto
open ChickenCheck.Domain
open ChickenCheck.Client
open Elmish
open System

let chicken = 
    { Id = Guid.NewGuid() |> ChickenId
      Name = "testnamn" |> String200.createOrFail "name"
      ImageUrl = None
      Breed = "testras" |> String200.createOrFail "breed" }

let chickensApi : Chickens.Api =
    { GetChickens = fun () -> Chickens.Msg.FetchedChickens [ chicken ] |> Cmd.ofMsg
      GetTotalCount = fun () -> Chickens.Msg.FetchedTotalCount Map.empty |> Cmd.ofMsg
      GetCountOnDate = fun date -> Chickens.Msg.FetchedEggCountOnDate (date, Map.empty) |> Cmd.ofMsg }

let chickenCardApi : ChickenCard.Api = 
    { AddEgg = fun _ -> ChickenCard.Msg.AddedEgg |> Cmd.ofMsg
      RemoveEgg = fun _ -> ChickenCard.Msg.RemovedEgg |> Cmd.ofMsg }

let count = EggCount.zero |> Some
let model = ChickenCard.init chicken count Date.today

module Result =
    let value result = match result with | Ok v -> v | _ -> failwith "expected a value"

[<Tests>]
let tests =
    testList "update" [
        test "AddEgg starts running AddEgg request" {
            let (newModel, _) = ChickenCard.update chickenCardApi ChickenCard.Msg.AddEgg model
            let expectedModel = { model with AddEggStatus = Running }
            expectedModel =! newModel
        }
        test "AddedEgg increases egg count by one" {
            let initialCount = model.EggCount.Value.Value
            let (newModel, _) = ChickenCard.update chickenCardApi ChickenCard.Msg.AddedEgg model
            newModel.EggCount.Value.Value =! initialCount + 1
        }
        test "AddedEgg twice increases egg count by two" {
            let initialCount = model.EggCount.Value.Value
            let (model, _) = ChickenCard.update chickenCardApi ChickenCard.Msg.AddedEgg model
            let (newModel, _) = ChickenCard.update chickenCardApi ChickenCard.Msg.AddedEgg model
            newModel.EggCount.Value.Value =! initialCount + 2
        }
    ]