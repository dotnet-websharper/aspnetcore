namespace WebSharper.AspNetCore

open System
open System.IO
open System.Collections.Generic
open System.Collections.Concurrent
open System.Reflection
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open WebSharper.Sitelets
module Res = WebSharper.Core.Resources
module Shared = WebSharper.Web.Shared

[<Sealed>]
type WebSharperOptions
    internal 
    (
        services: IServiceProvider,
        contentRoot: string,
        webRoot: string,
        isDebug: bool,
        assemblies: Assembly[],
        sitelet: option<Sitelet<obj>>,
        useSitelets: bool,
        useRemoting: bool
    ) =

    static let tryLoad(name: AssemblyName) =
        try
            match Assembly.Load(name) with
            | null -> None
            | a -> Some a
        with _ -> None

    static let loadFileInfo(p: string) =
        let fn = Path.GetFullPath p
        let name = AssemblyName.GetAssemblyName(fn)
        match tryLoad(name) with
        | None -> Assembly.LoadFrom(fn)
        | Some a -> a

    static let discoverAssemblies (path: string) =
        let ls pat = Directory.GetFiles(path, pat)
        let files = Array.append (ls "*.dll") (ls "*.exe")
        files |> Array.choose (fun p ->
            try Some (loadFileInfo(p))
            with e -> None)

    static let loadReferencedAssemblies (logger: ILogger) (alreadyLoaded: Assembly[]) =
        let loaded = Dictionary()
        let rec load (asm: Assembly) =
            for asmName in asm.GetReferencedAssemblies() do
                let name = asmName.Name
                if not (loaded.ContainsKey name) then
                    try loaded.[name] <- Assembly.Load(asmName)
                    with _ ->
                        logger.LogWarning("Failed to load {0} referenced by {1}", name, asm.GetName().Name)
        for asm in alreadyLoaded do
            loaded.[asm.GetName().Name] <- asm
        Array.ofSeq loaded.Values

    static let autoBinDir() =
        Reflection.Assembly.GetExecutingAssembly().Location
        |> Path.GetDirectoryName
      
    member val AuthenticationScheme = "WebSharper" with get, set

    member this.Services = services

    member this.UseSitelets = useSitelets

    member this.UseRemoting = useRemoting

    member this.Metadata = Shared.Metadata

    member this.Dependencies = Shared.Dependencies

    member this.Json = Shared.Json

    member this.IsDebug = isDebug

    member this.WebRootPath = webRoot

    member this.ContentRootPath = contentRoot

    member this.Assemblies = assemblies

    member this.Sitelet = sitelet

    static member Create
        (
            services: IServiceProvider,
            [<Optional>] sitelet: Sitelet<'T>,
            [<Optional>] config: IConfiguration,
            [<Optional>] logger: ILogger,
            [<Optional>] binDir: string,
            [<Optional; DefaultParameterValue true>] useSitelets: bool,
            [<Optional; DefaultParameterValue true>] useRemoting: bool
        ) =
        let siteletOpt =
            if obj.ReferenceEquals(sitelet, null)
            then None
            else Some (Sitelet.Box sitelet)
        WebSharperOptions.Create(services, siteletOpt, Option.ofObj config, Option.ofObj logger, Option.ofObj binDir, useSitelets, useRemoting)

    static member internal Create
        (
            services: IServiceProvider,
            sitelet: option<Sitelet<obj>>,
            config: option<IConfiguration>,
            logger: option<ILogger>,
            binDir: option<string>,
            useSitelets: bool,
            useRemoting: bool
        ) =
        let binDir =
            match binDir with
            | None -> autoBinDir()
            | Some d -> d
        let logger =
            match logger with
            | Some l -> l
            | None -> services.GetRequiredService<ILoggerFactory>().CreateLogger<WebSharperOptions>() :> _
        // Note: must load assemblies and set Context.* before calling Shared.*
        let assemblies =
            discoverAssemblies binDir
            |> loadReferencedAssemblies logger
        let env = services.GetRequiredService<IHostingEnvironment>()
        Context.IsDebug <- env.IsDevelopment
        let config =
            match config with
            | Some c -> c
            | None -> services.GetRequiredService<IConfiguration>().GetSection("websharper") :> _
        Context.GetSetting <- fun key -> Option.ofObj config.[key]
        let sitelet =
            if useSitelets then
                sitelet |> Option.orElseWith (fun () ->
                    match services.GetRequiredService<ISiteletService>() with
                    | null -> None
                    | service -> Some service.Sitelet
                )
            else None
        WebSharperOptions(services, env.ContentRootPath, env.WebRootPath, env.IsDevelopment(), assemblies, sitelet, useSitelets, useRemoting)

type WebSharperBuilder(services: IServiceProvider) =
    let mutable _sitelet = None
    let mutable _config = None
    let mutable _logger = None
    let mutable _binDir = None
    let mutable _authScheme = None
    let mutable _useSitelets = true
    let mutable _useRemoting = true

    member this.Sitelet<'T when 'T : equality>(sitelet: Sitelet<'T>) =
        _sitelet <- Some (Sitelet.Box sitelet)
        this

    member this.Config(config: IConfiguration) =
        _config <- Some config
        this

    member this.Logger(logger: ILogger) =
        _logger <- Some logger
        this

    member this.Logger(loggerFactory: ILoggerFactory) =
        _logger <- Some (loggerFactory.CreateLogger<WebSharperOptions>() :> ILogger)
        this

    member this.BinDir(binDir: string) =
        _binDir <- Some binDir
        this

    member this.AuthenticationScheme(scheme: string) =
        _authScheme <- Some scheme
        this

    member this.UseSitelets([<Optional; DefaultParameterValue true>] useSitelets: bool) =
        _useSitelets <- useSitelets
        this

    member this.UseRemoting([<Optional; DefaultParameterValue true>] useRemoting: bool) =
        _useRemoting <- useRemoting
        this

    member this.Build() =
        let o = WebSharperOptions.Create(services, _sitelet, _config, _logger, _binDir, _useSitelets, _useRemoting)
        _authScheme |> Option.iter (fun s -> o.AuthenticationScheme <- s)
        o
