namespace WebSharper.AspNetCore.Tests

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open WebSharper.AspNetCore

type Startup(loggerFactory: ILoggerFactory, config: IConfiguration) =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddAuthentication("WebSharper")
            .AddCookie("WebSharper", fun options -> ())
        |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =
        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore

        app.UseAuthentication()
            .UseWebSharper(env, fun builder ->
                builder.Sitelet(Website.Main)
                       .Config(config.GetSection("websharper"))
                       .Logger(loggerFactory)
                |> ignore)
            .UseStaticFiles()
            .Run(fun context ->
                context.Response.StatusCode <- 404
                context.Response.WriteAsync("Fell through :("))
