module ChickenCheck.Client.Tests.ChickenCardTests

open Swensen.Unquote
open Expecto
open ChickenCheck.Domain
open ChickenCheck.Client
open Elmish
open System

[<Tests>]
let tests =
    testList "Chickens" [
        test "AddEgg starts running AddEgg request" {
            true =! false
        }
    ]