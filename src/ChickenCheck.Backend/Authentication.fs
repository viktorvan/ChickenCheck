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
            do! ctx.ChallengeAsync("Auth0", AuthenticationProperties(RedirectUri = returnUrl))
            return! next ctx
        }
        
let requiresLoggedIn : HttpHandler = requiresAuthentication challenge
let authorizeUser role : HttpHandler = 
    let forbidden =
        RequestErrors.FORBIDDEN
            "Permission denied. You do not have access to this application."
    requiresLoggedIn >> requiresRole role forbidden

let logout : HttpHandler =
    Auth.signOut "Auth0"
    >=> Auth.signOut CookieAuthenticationDefaults.AuthenticationScheme
    
