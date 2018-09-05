namespace WebSharper.AspNetCore

open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open WebSharper

[<Extension>]
type WebContextExtensions =

    [<Extension>]
    static member HttpContext(this: Web.Context) =
        this.Environment.["WebSharper.AspNetCore.HttpContext"] :?> HttpContext

    [<Extension>]
    static member Configuration(this: Web.Context) =
        this.Environment.["WebSharper.AspNetCore.Configuration"] :?> IConfiguration

    [<Extension>]
    static member Logger(this: Web.Context) =
        this.Environment.["WebSharper.AspNetCore.Logger"] :?> ILogger
