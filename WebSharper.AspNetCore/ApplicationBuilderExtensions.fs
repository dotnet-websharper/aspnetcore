/// The keys that WebSharper sets in the HttpContext.
namespace WebSharper.AspNetCore

open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open WebSharper.Sitelets

[<Extension>]
type ApplicationBuilderExtensions =

    // Remoting

    [<Extension>]
    static member UseWebSharperRemoting(this: IApplicationBuilder, options: WebSharperOptions) =
        this.Use(Remoting.Middleware options)

    [<Extension>]
    static member UseWebSharperRemoting(this: IApplicationBuilder, env: IHostingEnvironment) =
        this.UseWebSharperRemoting(WebSharperOptions.Create(env))

    [<Extension>]
    static member UseWebSharperRemoting(this: IApplicationBuilder, env: IHostingEnvironment, binDir: string) =
        this.UseWebSharperRemoting(WebSharperOptions.Create(env, binDir))

    // Sitelets

    [<Extension>]
    static member UseWebSharperSitelets(this: IApplicationBuilder, options: WebSharperOptions) =
        this.Use(Sitelets.Middleware options)

    [<Extension>]
    static member UseWebSharperSitelets(this: IApplicationBuilder, env: IHostingEnvironment, sitelet: Sitelet<'T>) =
        this.UseWebSharperSitelets(WebSharperOptions.Create(env, sitelet))

    [<Extension>]
    static member UseWebSharperSitelets(this: IApplicationBuilder, env: IHostingEnvironment, binDir: string, sitelet: Sitelet<'T>) =
        this.UseWebSharperSitelets(WebSharperOptions.Create(env, binDir, sitelet))

    // All

    [<Extension>]
    static member UseWebSharper(this: IApplicationBuilder, options: WebSharperOptions) =
        this.UseWebSharperRemoting(options)
            .UseWebSharperSitelets(options)

    [<Extension>]
    static member UseWebSharper(this: IApplicationBuilder, env: IHostingEnvironment) =
        this.UseWebSharper(WebSharperOptions.Create(env))

    [<Extension>]
    static member UseWebSharper(this: IApplicationBuilder, env: IHostingEnvironment, binDir: string) =
        this.UseWebSharper(WebSharperOptions.Create(env, binDir))

    [<Extension>]
    static member UseWebSharper(this: IApplicationBuilder, env: IHostingEnvironment, sitelet: Sitelet<'T>) =
        this.UseWebSharper(WebSharperOptions.Create(env, sitelet))

    [<Extension>]
    static member UseWebSharper(this: IApplicationBuilder, env: IHostingEnvironment, binDir: string, sitelet: Sitelet<'T>) =
        this.UseWebSharper(WebSharperOptions.Create(env, binDir, sitelet))
