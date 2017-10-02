namespace WebSharper.AspNetCore

open System
open System.IO
open System.Collections.Generic
open System.Collections.Concurrent
open System.Reflection
open Microsoft.AspNetCore.Hosting
open WebSharper.Sitelets
module Res = WebSharper.Core.Resources
module Shared = WebSharper.Web.Shared

[<Sealed>]
type WebSharperOptions
    (
        meta: WebSharper.Core.Metadata.Info,
        deps: WebSharper.Core.DependencyGraph.Graph,
        json: WebSharper.Core.Json.Provider,
        contentRoot: string,
        webRoot: string,
        isDebug: bool,
        assemblies: Assembly[],
        sitelet: option<Sitelet<obj>>
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

    static let loadReferencedAssemblies (alreadyLoaded: Assembly[]) =
        let loaded = Dictionary()
        let rec load (asm: Assembly) =
            for asmName in asm.GetReferencedAssemblies() do
                let name = asmName.Name
                if not (loaded.ContainsKey name) then
                    try loaded.[name] <- Assembly.Load(asmName)
                    with _ -> eprintfn "Failed to load %s referenced by %s" name (asm.GetName().Name)
        for asm in alreadyLoaded do
            loaded.[asm.GetName().Name] <- asm
        Array.ofSeq loaded.Values

    let resourceContextCache = ConcurrentDictionary<string, Res.Context>()
    let getOrAddResourceContext appPath =
        resourceContextCache.GetOrAdd(appPath, fun appPath ->
            ResourceContext.Build appPath isDebug meta
        )

    member val AuthenticationScheme = "WebSharper" with get, set

    member this.Metadata = meta

    member this.Dependencies = deps

    member this.Json = json

    member this.IsDebug = isDebug

    member this.WebRootPath = webRoot

    member this.ContentRootPath = contentRoot

    member this.Assemblies = assemblies

    member this.GetOrAddResourceContext appPath = getOrAddResourceContext appPath

    member this.Sitelet = sitelet

    new(env: IHostingEnvironment, meta, deps, json, assemblies, sitelet) =
        WebSharperOptions(meta, deps, json, env.ContentRootPath, env.WebRootPath, env.IsDevelopment(), assemblies, sitelet)

    new(env, binDir, sitelet) =
        let assemblies =
            discoverAssemblies binDir
            |> loadReferencedAssemblies
        WebSharperOptions(env, Shared.Metadata, Shared.Dependencies, Shared.Json, assemblies, sitelet)

    static member Create(env, binDir, sitelet) =
        WebSharperOptions(env, binDir, Some (Sitelet.Upcast sitelet))

    static member Create(env: IHostingEnvironment, sitelet) =
        let binDir =
            Reflection.Assembly.GetExecutingAssembly().Location
            |> Path.GetDirectoryName
        WebSharperOptions(env, binDir, Some (Sitelet.Upcast sitelet))

    static member Create(env, binDir) =
        WebSharperOptions(env, binDir, None)

    static member Create(env) =
        let binDir = ""
        WebSharperOptions(env, binDir, None)
