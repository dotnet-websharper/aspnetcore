module WebSharper.AspNetCore.Context

open System.Collections.Generic
open System.Collections.Specialized
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open WebSharper.Sitelets
module Res = WebSharper.Core.Resources

let private stripFinalSlash (s: string) =
    if s.EndsWith "/" then s.[..s.Length-2] else s

let private (++) (a: string) (b: string) =
    let startsWithSlash (s: string) =
        s.Length > 0
        && s.[0] = '/'
    let endsWithSlash (s: string) =
        s.Length > 0
        && s.[s.Length - 1] = '/'
    match endsWithSlash a, startsWithSlash b with
    | true, true -> a + b.Substring(1)
    | false, false -> a + "/" + b
    | _ -> a + b

let RequestUri (req: HttpRequest) =
    System.UriBuilder(
        req.Scheme,
        req.Host.Host,
        req.Host.Port.GetValueOrDefault(-1),
        req.Path.ToString(),
        req.QueryString.ToString()
    ).Uri

let private buildRequest (req: HttpRequest) =
    {
        Method = Http.Method.OfString req.Method
        Uri = RequestUri req
        Headers =
            [
                for KeyValue(k, v) in req.Headers do
                    for x in v do
                        yield Http.Header.Custom k x
            ]
        Post = Http.ParameterCollection([]) // TODO
        Get =
            let nv = NameValueCollection()
            for KeyValue(k, v) in req.Query do
                for x in v do
                    nv.Add(k, x)
            Http.ParameterCollection(nv)
        ServerVariables = Http.ParameterCollection([])
        Body = req.Body
        Files = [] // TODO
    } : Http.Request

type private UserSession(httpCtx: HttpContext, options: WebSharperOptions) =
    let scheme = options.AuthenticationScheme

    interface WebSharper.Web.IUserSession with
        member this.GetLoggedInUser() =
            match httpCtx.User with
            | null -> None
            | u ->
                match u.Identity.Name with
                | null -> None
                | i -> Some i
            |> async.Return

        member this.Logout() =
            httpCtx.SignOutAsync(scheme)
            |> Async.AwaitTask

        member this.LoginUser(username, ?persistent) =
            let identity = System.Security.Principal.GenericIdentity(username)
            let principal = System.Security.Principal.GenericPrincipal(identity, [||])
            httpCtx.SignInAsync(scheme, principal)
            |> Async.AwaitTask

        member this.IsAvailable = true

let Make (httpCtx: HttpContext) (options: WebSharperOptions) =
    // TODO make customizable?
    let appPath = "/"
    let link =
        match options.Sitelet with
        | None ->
            fun _ -> failwith "Failed to create link: no Sitelet set up."
        | Some s ->
            fun x ->
                match s.Router.Link x with
                | None -> failwithf "Failed to link to %O" (box x)
                | Some loc when loc.IsAbsoluteUri -> string loc
                | Some loc -> appPath ++ string loc
    let req = buildRequest httpCtx.Request
    new Context<'T>(
        ApplicationPath = appPath,
        // TODO use httpCtx.Items? but it's <obj, obj>, not <string, obj>
        Environment = Dictionary(),
        Link = link,
        Json = options.Json,
        Metadata = options.Metadata,
        Dependencies = options.Dependencies,
        ResourceContext = options.GetOrAddResourceContext(appPath),
        Request = req,
        RootFolder = options.ContentRootPath,
        UserSession = UserSession(httpCtx, options)
    )

let MakeSimple (httpCtx: HttpContext) (options: WebSharperOptions) =
    // TODO make customizable?
    let appPath = "/"
    let uri = RequestUri httpCtx.Request
    { new WebSharper.Web.Context() with
        member this.ApplicationPath = appPath
        // TODO use httpCtx.Items? but it's <obj, obj>, not <string, obj>
        member this.Environment = Dictionary() :> _
        member this.Json = options.Json
        member this.Metadata = options.Metadata
        member this.Dependencies = options.Dependencies
        member this.ResourceContext = options.GetOrAddResourceContext(appPath)
        member this.RequestUri = uri
        member this.RootFolder = options.ContentRootPath
        member this.UserSession = UserSession(httpCtx, options) :> _
    }

let private getOrMake<'T> make (httpCtx: HttpContext) (options: WebSharperOptions) =
    match httpCtx.Items.TryGetValue(EnvKey.Context) with
    | true, x -> x :?> 'T
    | false, _ ->
        let ctx = make httpCtx options
        httpCtx.Items.[EnvKey.Context] <- ctx
        ctx

let GetOrMake httpCtx options =
    getOrMake<Context<'T>> Make httpCtx options

let GetOrMakeSimple httpCtx options =
    getOrMake<WebSharper.Web.Context> MakeSimple httpCtx options
