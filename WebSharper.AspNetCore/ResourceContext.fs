module internal WebSharper.AspNetCore.ResourceContext

module P = WebSharper.PathConventions
module Res = WebSharper.Core.Resources
module M = WebSharper.Core.Metadata

let Build appPath isDebug (meta: M.Info) =
    let pu = P.PathUtility.VirtualPaths(appPath)
    {
        DebuggingEnabled = isDebug
        DefaultToHttp = false
        GetSetting = fun (name: string) ->
            None // TODO
            // match ConfigurationManager.AppSettings.[name] with
            // | null -> None
            // | x -> Some x
        GetAssemblyRendering = fun name ->
            let aid = P.AssemblyId.Create(name)
            let url = if isDebug then pu.JavaScriptPath(aid) else pu.MinifiedJavaScriptPath(aid)
            let version = 
                let fileName = if isDebug then pu.JavaScriptFileName(aid) else pu.MinifiedJavaScriptFileName(aid)
                match meta.ResourceHashes.TryGetValue(fileName) with
                | true, h -> "?h=" + string h
                | _ -> ""
            Res.RenderLink (url + version)
        GetWebResourceRendering = fun ty resource ->
            let id = P.AssemblyId.Create(ty)
            let kind =
                if resource.EndsWith(".js") || resource.EndsWith(".ts")
                    then P.ResourceKind.Script
                    else P.ResourceKind.Content
            let r = P.EmbeddedResource.Create(kind, id, resource)
            let url = pu.EmbeddedPath r
            let version = 
                match meta.ResourceHashes.TryGetValue(pu.EmbeddedResourceKey r) with
                | true, h -> "?h=" + string h
                | _ -> ""
            Res.RenderLink (url + version)
        RenderingCache = System.Collections.Concurrent.ConcurrentDictionary()
        ResourceDependencyCache = System.Collections.Concurrent.ConcurrentDictionary()
    } : Res.Context

