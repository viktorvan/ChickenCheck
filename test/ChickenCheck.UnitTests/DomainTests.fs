module ChickenCheck.UnitTests.DomainTests

open ChickenCheck.Backend
open Expecto
open Swensen.Unquote
open System

let testCase = Expecto.Tests.test

let notFutureDateTests = testList "NotFutureDateTests" [
    testCase "tryParse with invalid date string is InvalidDateString error" {
        let result = NotFutureDate.tryParse "not a date"
        test <@ result = Error (NotFutureDate.InvalidDateString "not a date") @>
    }
    testCase "tryParse with future date is FutureDateError" {
        let tomorrow = DateTime.Today.AddDays(1.)
        let result = NotFutureDate.tryParse (tomorrow.ToString("yyyy-MM-dd"))
        test <@ result = Error (NotFutureDate.FutureDateError (FutureDate tomorrow)) @>
    }
]
    
[<Tests>]
let tests = testList "Workflows" [
        notFutureDateTests
    ]
