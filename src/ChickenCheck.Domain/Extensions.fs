namespace ChickenCheck.Domain

module Async =
    let retn x = async { return x }

module String =
    let notNullOrEmpty str =
        if System.String.IsNullOrEmpty str then invalidArg "" "String cannot be empty"
        else str
