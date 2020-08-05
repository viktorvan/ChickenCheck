module ChickenCheck.Backend.Extensions

open Microsoft.AspNetCore.Http

type HttpContext with
    member this.FullPath = this.Request.Path.Value + this.Request.QueryString.Value

type Feliz.ViewEngine.prop with
    static member disableTurbolinks = Feliz.ViewEngine.prop.custom ("data-turbolinks", "false")
