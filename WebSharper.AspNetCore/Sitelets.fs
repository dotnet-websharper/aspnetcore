module WebSharper.AspNetCore.Sitelets

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open WebSharper.Sitelets

let private writeResponse (resp: Task<Http.Response>) (out: HttpResponse) =
    resp.ContinueWith(fun (t: Task<Http.Response>) ->
        let resp = t.Result
        out.StatusCode <- resp.Status.Code
        for name, hs in resp.Headers |> Seq.groupBy (fun h -> h.Name) do
            let values =
                [| for h in hs -> h.Value |]
                |> Microsoft.Extensions.Primitives.StringValues
            out.Headers.Append(name, values)
        resp.WriteBody(out.Body)
    )

let Middleware (options: WebSharperOptions) =
    let sitelet =
        match options.Sitelet with
        | Some s -> Some s
        | None -> Loading.DiscoverSitelet options.Assemblies
    match sitelet with
    | None ->
        Func<_,_,_>(fun (_: HttpContext) (next: Func<Task>) -> next.Invoke())
    | Some sitelet ->
        Func<_,_,_>(fun (httpCtx: HttpContext) (next: Func<Task>) ->
            let ctx = Context.GetOrMake httpCtx options
            match sitelet.Router.Route ctx.Request with
            | Some endpoint ->
                let content = sitelet.Controller.Handle endpoint
                let response = Content.ToResponse content ctx |> Async.StartAsTask
                writeResponse response httpCtx.Response
            | None -> next.Invoke()
        )
