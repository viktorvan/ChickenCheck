module ChickenCheck.Backend.Extensions

open System.IO
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.CookiePolicy
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.StaticFiles
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Primitives
open Microsoft.Net.Http.Headers
open Saturn

type HttpContext with
    member this.FullPath = this.Request.Path.Value + this.Request.QueryString.Value

module String =
    let notNullOrEmpty str =
        if System.String.IsNullOrEmpty str then None
        else Some str
        
module Map =
    let keys map = map |> Map.toList |> List.map fst
    let values map = map |> Map.toList |> List.map snd
    
    let change key f map =
        Map.tryFind key map
        |> f
        |> function
        | Some v -> Map.add key v map
        | None -> Map.remove key map

    let tryFindWithDefault defaultValue key map =
        map
        |> Map.tryFind key
        |> Option.defaultValue defaultValue
        
module Option =
    let ofResult r =
        match r with
        | Ok v -> Some v
        | Error _ -> None
        
module List =
    let batchesOf size input = 
        // Inner function that does the actual work.
        // 'input' is the remaining part of the list, 'num' is the number of elements
        // in a current batch, which is stored in 'batch'. Finally, 'acc' is a list of
        // batches (in a reverse order)
        let rec loop input num batch acc =
            match input with
            | [] -> 
                // We've reached the end - add current batch to the list of all
                // batches if it is not empty and return batch (in the right order)
                if batch <> [] then (List.rev batch)::acc else acc
                |> List.rev
            | x::xs when num = size - 1 ->
                // We've reached the end of the batch - add the last element
                // and add batch to the list of batches.
                loop xs 0 [] ((List.rev (x::batch))::acc)
            | x::xs ->
                // Take one element from the input and add it to the current batch
                loop xs (num + 1) (x::batch) acc
        loop input 0 [] []
