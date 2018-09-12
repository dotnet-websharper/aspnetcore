namespace WebSharper.AspNetCore

open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open WebSharper

[<Extension>]
type WebContextExtensions =

    /// <summary>Get the ASP.NET Core <c>HttpContext</c> for the current request.</summary>
    [<Extension>]
    static member HttpContext(this: Web.Context) =
        this.Environment.["WebSharper.AspNetCore.HttpContext"] :?> HttpContext
