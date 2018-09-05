module WebSharper.AspNetCore.Tests.Website

open Microsoft.Extensions.Logging
open WebSharper
open WebSharper.AspNetCore
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Templating

type IndexTemplate = Template<"Main.html", clientLoad = ClientLoad.FromDocument>

module Rpc =

    [<Rpc>]
    let GetLogin() =
        WebSharper.Web.Remoting.GetContext().UserSession.GetLoggedInUser()

    [<Rpc>]
    let Login(name) =
        WebSharper.Web.Remoting.GetContext().UserSession.LoginUser(name)

    [<Rpc>]
    let Logout() =
        WebSharper.Web.Remoting.GetContext().UserSession.Logout()

type EndPoint =
    | [<EndPoint "/">] Home
    | [<EndPoint "/about">] About
    | [<EndPoint "POST /post">] Post
    | [<EndPoint "POST /formdata"; FormData "x">] FormData of x: string 

[<JavaScript>]
[<Require(typeof<Resources.BaseResource>, "//maxcdn.bootstrapcdn.com/bootstrap/3.3.5/css/bootstrap.min.css")>]
module Client =
    open WebSharper.UI.Client

    type Task = { Name: string; Done: Var<bool> }

    let Tasks =
        ListModel.Create (fun task -> task.Name)
            [ { Name = "Have breakfast"; Done = Var.Create true }
              { Name = "Have lunch"; Done = Var.Create false } ]

    let NewTaskName = Var.Create ""

    let Login = Var.Create ""

    let Main (aboutPageLink: string) =
        IndexTemplate.Body()
            .ListContainer(
                ListModel.View Tasks |> Doc.BindSeqCached (fun task ->
                    IndexTemplate.ListItem()
                        .Task(task.Name)
                        .Clear(fun _ -> Tasks.RemoveByKey task.Name)
                        .Done(task.Done)
                        .ShowDone(Attr.DynamicClassPred "checked" task.Done.View)
                        .Doc()
                ))
            .NewTaskName(NewTaskName)
            .Add(fun _ ->
                Tasks.Add { Name = NewTaskName.Value; Done = Var.Create false }
                Var.Set NewTaskName "")
            .ClearCompleted(fun _ -> Tasks.RemoveBy (fun task -> task.Done.Value))
            .Login(Login)
            .DoLogin(fun _ -> Rpc.Login Login.Value |> Async.Start)
            .GetLogin(fun _ ->
                async {
                    let! u = Rpc.GetLogin()
                    match u with
                    | None -> "Not logged in."
                    | Some u -> "Logged in as: " + u
                    |> JS.Alert
                }
                |> Async.Start
            )
            .Logout(fun _ -> Rpc.Logout() |> Async.Start)
            .AboutPageLink(aboutPageLink)
            .Doc()

open WebSharper.UI.Server

let Main =
    Application.MultiPage(fun (ctx: Context<_>) (ep: EndPoint) ->
        let readBody() =
            let i = ctx.Request.Body 
            if not (isNull i) then 
                // We need to copy the stream because else StreamReader would close it.
                use m =
                    if i.CanSeek then
                        new System.IO.MemoryStream(int i.Length)
                    else
                        new System.IO.MemoryStream()
                i.CopyTo m
                if i.CanSeek then
                    i.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore
                m.Seek(0L, System.IO.SeekOrigin.Begin) |> ignore
                use reader = new System.IO.StreamReader(m)
                reader.ReadToEnd()
            else "Request body not found"
        ctx.Logger().LogInformation("Serving {0}", ep)
        match ep with
        | Home ->
            let aboutPageLink = ctx.Link About
            IndexTemplate()
                .Main(client <@ Client.Main aboutPageLink @>)
                .Doc()
            |> Content.Page
        | About ->
            Content.Text "This is a test project for WebSharper.AspNetCore"
        | FormData i ->
            Content.Text i
        | Post ->
            Content.Text ctx.Request.BodyText
    )
