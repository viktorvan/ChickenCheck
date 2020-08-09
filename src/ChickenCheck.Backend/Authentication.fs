module ChickenCheck.Backend.Authentication

open Microsoft.AspNetCore.Authentication.Cookies
open Giraffe
open ChickenCheck.Shared
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Authentication
open FSharp.Control.Tasks.V2.ContextInsensitive

let challenge : HttpHandler =
    fun next ctx ->
        let returnUrl = 
            ctx.TryGetQueryStringValue "returnUrl"
            |> Option.defaultValue "/"
        task {
            printfn "Using return url: %s" returnUrl
            do! ctx.ChallengeAsync("Auth0", AuthenticationProperties(RedirectUri = returnUrl))
            return! next ctx
        }
        
let requireLoggedIn : HttpHandler = requiresAuthentication challenge
let logout : HttpHandler =
    Giraffe.Auth.signOut "Auth0"
    >=> Giraffe.Auth.signOut CookieAuthenticationDefaults.AuthenticationScheme
    
