/// The keys that WebSharper sets in the HttpContext.
namespace WebSharper.AspNetCore

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
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
            [<Optional>] binDir: string,
            [<Optional; DefaultParameterValue "/">] appPath: string
        ) =
        this.UseWebSharperRemoting(WebSharperOptions.Create(env, unbox null, config, binDir, appPath))

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
            [<Optional>] binDir: string,
            [<Optional; DefaultParameterValue "/">] appPath: string
        ) =
        this.UseWebSharperSitelets(WebSharperOptions.Create(env, sitelet, config, binDir, appPath))

    // All

    [<Extension>]
    static member UseWebSharper(this: IApplicationBuilder, options: WebSharperOptions) =
        this.UseWebSharperRemoting(options)
            .UseWebSharperSitelets(options)

    [<Extension>]
    static member UseWebSharper
        (
            this: IApplicationBuilder,
            env: IHostingEnvironment,
            [<Optional>] sitelet: Sitelet<'T>,
            [<Optional>] config: IConfiguration,
            [<Optional>] binDir: string,
            [<Optional; DefaultParameterValue "/">] appPath: string
        ) =
        this.UseWebSharper(WebSharperOptions.Create(env, sitelet, config, binDir, appPath))
