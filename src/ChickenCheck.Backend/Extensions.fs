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

type Feliz.ViewEngine.prop with
    static member disableTurbolinks = Feliz.ViewEngine.prop.custom ("data-turbolinks", "false")
            
