module WebSharper.AspNetCore.Context

open System
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

type private AspNetCoreRequest(req: HttpRequest) =
    inherit Http.Request()

    let method = Http.Method.OfString req.Method
    let uri = RequestUri req
    let mutable post = null
    let mutable get = null
    let mutable cookies = null

    override this.Method = method
    override this.Uri = uri                                     
    override this.Headers =
        seq {
            for KeyValue(k, v) in req.Headers do
                for x in v do
                    yield Http.Header.Custom k x
        }
    override this.Body = req.Body
    override this.Post = 
        if isNull post then
            post <-
                if req.HasFormContentType then 
                    { new Http.ParameterCollection with
                        member this.Item(name:string) =
                            match req.Form.TryGetValue name with
                            | true, v -> Some (string v)
                            | _ -> None
                        member this.ToList() =
                            [
                                for KeyValue(k, v) in req.Form do
                                    yield (k, string v)
                            ]    
                    }
                else
                    Http.EmptyParameters
        post
    override this.Get = 
        if isNull get then
            get <-
                { new Http.ParameterCollection with
                    member this.Item(name:string) =
                        match req.Query.TryGetValue name with
                        | true, v -> Some (string v)
                        | _ -> None
                    member this.ToList() =
                        [
                            for KeyValue(k, v) in req.Query do
                                yield (k, string v)
                        ]    
                }
        get
    override this.ServerVariables = Http.EmptyParameters
    override this.Files =
        seq {
            for f in req.Form.Files do
                yield { new Http.IPostedFile with
                    member this.Key = f.Name
                    member this.ContentLength = int f.Length
                    member this.ContentType = f.ContentType
                    member this.FileName = f.FileName
                    member this.InputStream = f.OpenReadStream()
                    member this.SaveAs(n) =
                        use fileStream = System.IO.File.Create(n)
                        use s = f.OpenReadStream()
                        s.CopyTo(fileStream)
                }
        }
    override this.Cookies =
        if isNull cookies then
            cookies <-
                { new Http.ParameterCollection with
                    member this.Item(name:string) =
                        match req.Cookies.TryGetValue name with
                        | true, v -> Some (string v)
                        | _ -> None
                    member this.ToList() =
                        [
                            for KeyValue(k, v) in req.Cookies do
                                yield (k, string v)
                        ]    
                }
        cookies

let private buildRequest (req: HttpRequest) =
    AspNetCoreRequest req :> Http.Request

type private UserSession(httpCtx: HttpContext, options: WebSharperOptions) =
    let scheme = options.AuthenticationScheme

    let loginUser (username: string) (expiry: option<TimeSpan>) =
        let identity = System.Security.Principal.GenericIdentity(username)
        let principal = System.Security.Principal.GenericPrincipal(identity, [||])
        let props = AuthenticationProperties()
        props.IsPersistent <- expiry.IsSome
        props.ExpiresUtc <-
            match expiry with
            | None -> Nullable()
            | Some t -> Nullable(DateTimeOffset.UtcNow.Add(t))
        httpCtx.SignInAsync(scheme, principal, props)
        |> Async.AwaitTask

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

        member this.LoginUser(username, ?persistent: bool) =
            if defaultArg persistent false
                then Some (TimeSpan.FromDays(1000.*365.))
                else None
            |> loginUser username 

        member this.LoginUser(username, expiry: TimeSpan) =
            loginUser username (Some expiry)

        member this.IsAvailable = true

let Make (httpCtx: HttpContext) (options: WebSharperOptions) =
    let link =
        match options.Sitelet with
        | None ->
            fun _ -> failwith "Failed to create link: no Sitelet set up."
        | Some s ->
            fun x ->
                match s.Router.Link x with
                | None -> failwithf "Failed to link to %O" (box x)
                | Some loc when loc.IsAbsoluteUri -> string loc
                | Some loc -> options.ApplicationPath ++ string loc
    let req = buildRequest httpCtx.Request
    new Context<'T>(
        ApplicationPath = options.ApplicationPath,
        Environment = dict ["HttpContext", box httpCtx],
        Link = link,
        Json = options.Json,
        Metadata = options.Metadata,
        Dependencies = options.Dependencies,
        ResourceContext = options.ResourceContext,
        Request = req,
        RootFolder = options.ContentRootPath,
        UserSession = UserSession(httpCtx, options)
    )

let MakeSimple (httpCtx: HttpContext) (options: WebSharperOptions) =
    let uri = RequestUri httpCtx.Request
    { new WebSharper.Web.Context() with
        member this.ApplicationPath = options.ApplicationPath
        // TODO use httpCtx.Items? but it's <obj, obj>, not <string, obj>
        member this.Environment = Dictionary() :> _
        member this.Json = options.Json
        member this.Metadata = options.Metadata
        member this.Dependencies = options.Dependencies
        member this.ResourceContext = options.ResourceContext
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
