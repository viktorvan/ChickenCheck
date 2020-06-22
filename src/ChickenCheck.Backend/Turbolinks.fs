module ChickenCheck.Backend.Turbolinks

open Saturn
open Microsoft.AspNetCore.Http
open Giraffe
open Microsoft.Extensions.Primitives

module TurbolinksHelpers =
    // use isXhr instead of isTurboLink?
    let isXhr (ctx: HttpContext) =
        ctx.Request.Headers.["X-Requested-With"].ToString() = "XMLHttpRequest"

    let isTurbolink (ctx: HttpContext) =
        ctx.Request.Headers.ContainsKey "Turbolinks-Referrer"

    let internal handleTurbolinks (ctx: HttpContext) =
        if isTurbolink ctx then ctx.Response.Headers.Add("Turbolinks-Location", StringValues (ctx.Request.Path.Value + ctx.Request.QueryString.Value))

///HttpHandler enabling Turbolinks support for given pipelines
let turbolinks (nxt: HttpFunc) (ctx: HttpContext): HttpFuncResult =
    TurbolinksHelpers.handleTurbolinks ctx
    nxt ctx
