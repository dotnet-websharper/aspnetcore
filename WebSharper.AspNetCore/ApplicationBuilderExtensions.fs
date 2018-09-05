namespace WebSharper.AspNetCore

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open WebSharper.Sitelets

[<Extension>]
type ApplicationBuilderExtensions =

    // Remoting                        

    [<Extension>]
    static member UseWebSharperRemoting(this: IApplicationBuilder, options: WebSharperOptions) =
        this.Use(Remoting.Middleware options)

    [<Extension>]
    static member UseWebSharperRemoting
        (
            this: IApplicationBuilder,
            env: IHostingEnvironment,
            [<Optional>] config: IConfiguration,
            [<Optional>] logger: ILogger,
            [<Optional>] binDir: string
        ) =
        this.UseWebSharperRemoting(WebSharperOptions.Create(env, None, Option.ofObj config, Option.ofObj logger, Option.ofObj binDir))

    // Sitelets

    [<Extension>]
    static member UseWebSharperSitelets(this: IApplicationBuilder, options: WebSharperOptions) =
        this.Use(Sitelets.Middleware options)

    [<Extension>]
    static member UseWebSharperSitelets
        (
            this: IApplicationBuilder,
            env: IHostingEnvironment,
            [<Optional>] sitelet: Sitelet<'T>,
            [<Optional>] config: IConfiguration,
            [<Optional>] logger: ILogger,
            [<Optional>] binDir: string
        ) =
        this.UseWebSharperSitelets(WebSharperOptions.Create(env, sitelet, config, logger, binDir))

    // All

    [<Extension>]
    static member UseWebSharper(this: IApplicationBuilder, options: WebSharperOptions) =
        if options.UseRemoting then this.UseWebSharperRemoting(options) |> ignore
        if options.UseSitelets then this.UseWebSharperSitelets(options) |> ignore
        this

    [<Extension>]
    static member UseWebSharper
        (
            this: IApplicationBuilder,
            env: IHostingEnvironment,
            [<Optional>] sitelet: Sitelet<'T>,
            [<Optional>] config: IConfiguration,
            [<Optional>] logger: ILogger,
            [<Optional>] binDir: string
        ) =
        this.UseWebSharper(WebSharperOptions.Create(env, sitelet, config, logger, binDir))

    [<Extension>]
    static member UseWebSharper
        (
            this: IApplicationBuilder,
            env: IHostingEnvironment,
            build: System.Action<WebSharperBuilder>
        ) =
        let builder = WebSharperBuilder(env)
        build.Invoke(builder)
        this.UseWebSharper(builder.Build())
