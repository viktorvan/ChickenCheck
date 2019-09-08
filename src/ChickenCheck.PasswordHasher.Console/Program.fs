// Learn more about F# at http://fsharp.org

open System
open Argu
open ChickenCheck.PasswordHasher
open ChickenCheck.Domain
open FsToolkit.ErrorHandling
open System.Security.Cryptography

type CLIArguments =
    | Password of password : string
    | PasswordAndHash of password : string * hash : string * salt : string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Password _ -> "specify a password to hash"
            | PasswordAndHash _ -> "specify a password and hash with salt to verify"

let parser = ArgumentParser.Create<CLIArguments>(programName = "ChickenCheck.PasswordHasher.Console.exe")


let printUsage() = parser.PrintUsage() |> printfn "%s"

[<EntryPoint>]
let main argv =
    result {

        let args = parser.Parse argv
        match args.TryGetResult Password, args.TryGetResult PasswordAndHash with
        | Some _, Some _ -> 
            printUsage()
            return 0
        | Some pw, _ ->
            return!
                pw
                |> Password.create
                |> Result.map hashPassword 
                |> Result.map (fun (hash:PasswordHash) ->
                    hash.Hash |> PasswordHash.toBase64String |> printfn "hash: %s" 
                    hash.Salt |> PasswordHash.toBase64String |> printfn "salt: %s"
                    0)

        | _, Some _ -> return failwith "not implemented"
        | None, None -> 
            printUsage()
            return 0

    } 
    |> function 
    | Ok _ -> 0
    | Error error -> 
        printfn "%A" error
        printUsage() 
        0
