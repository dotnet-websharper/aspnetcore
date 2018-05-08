namespace WebSharper.AspNetCore

open System
open System.IO
open System.Collections.Generic
open System.Collections.Concurrent
open System.Reflection
open System.Runtime.InteropServices
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open WebSharper.Sitelets
module Res = WebSharper.Core.Resources
module Shared = WebSharper.Web.Shared

[<Sealed>]
type WebSharperOptions
    internal 
    (
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

    static let autoBinDir() =
        Reflection.Assembly.GetExecutingAssembly().Location
        |> Path.GetDirectoryName
      
    member val AuthenticationScheme = "WebSharper" with get, set

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
            env: IHostingEnvironment,
            [<Optional>] sitelet: Sitelet<'T>,
            [<Optional>] config: IConfiguration,
            [<Optional>] binDir: string
        ) =
        let siteletOpt =
            if obj.ReferenceEquals(sitelet, null)
            then None
            else Some (Sitelet.Box sitelet)
        WebSharperOptions.Create(env, siteletOpt, Option.ofObj config, Option.ofObj binDir)

    static member internal Create
        (
            env: IHostingEnvironment,
            sitelet: option<Sitelet<obj>>,
            config: option<IConfiguration>,
            binDir: option<string>
        ) =
        let binDir =
            match binDir with
            | None -> autoBinDir()
            | Some d -> d
        // Note: must load assemblies and set Context.* before calling Shared.*
        let assemblies =
            discoverAssemblies binDir
            |> loadReferencedAssemblies
        Context.IsDebug <- env.IsDevelopment
        config |> Option.iter (fun config ->
            Context.GetSetting <- fun key -> Option.ofObj config.[key]
        )
        WebSharperOptions(env.ContentRootPath, env.WebRootPath, env.IsDevelopment(), assemblies, sitelet)
