namespace WebSharper.AspNetCore.Tests

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open WebSharper.AspNetCore

type Startup() =

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddAuthentication("WebSharper")
            .AddCookie("WebSharper", fun options ->
                // options.AccessDeniedPath <- PathString "/Account/Forbidden/"
                // options.LoginPath <- PathString "/Account/Unauthorized/"
                ()
            )
        |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =
        if env.IsDevelopment() then app.UseDeveloperExceptionPage() |> ignore

        app.UseAuthentication()
            .UseWebSharper(env, Website.Main)
            .UseStaticFiles()
            .Run(fun context ->
                context.Response.StatusCode <- 404
                context.Response.WriteAsync("Fell through :("))
