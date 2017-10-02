module WebSharper.AspNetCore.Tests.Website

open WebSharper
open WebSharper.JavaScript
open WebSharper.Sitelets
open WebSharper.UI.Next
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Templating

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

[<JavaScript>]
[<Require(typeof<Resources.BaseResource>, "//maxcdn.bootstrapcdn.com/bootstrap/3.3.5/css/bootstrap.min.css")>]
module Client =
    open WebSharper.UI.Next.Client

    type Task = { Name: string; Done: Var<bool> }

    let Tasks =
        ListModel.Create (fun task -> task.Name)
            [ { Name = "Have breakfast"; Done = Var.Create true }
              { Name = "Have lunch"; Done = Var.Create false } ]

    let NewTaskName = Var.Create ""

    let Login = Var.Create ""

    let Main() =
        IndexTemplate.Body()
            .ListContainer(
                ListModel.View Tasks |> Doc.BindSeqCached (fun task ->
                    IndexTemplate.ListItem()
                        .Task(task.Name)
                        .Clear(fun () -> Tasks.RemoveByKey task.Name)
                        .Done(task.Done)
                        .ShowDone(Attr.DynamicClass "checked" task.Done.View id)
                        .Doc()
                ))
            .NewTaskName(NewTaskName)
            .Add(fun () ->
                Tasks.Add { Name = NewTaskName.Value; Done = Var.Create false }
                Var.Set NewTaskName "")
            .ClearCompleted(fun () -> Tasks.RemoveBy (fun task -> task.Done.Value))
            .Login(Login)
            .DoLogin(fun () -> Rpc.Login Login.Value |> Async.Start)
            .GetLogin(fun () ->
                async {
                    let! u = Rpc.GetLogin()
                    match u with
                    | None -> "Not logged in."
                    | Some u -> "Logged in as: " + u
                    |> JS.Alert
                }
                |> Async.Start
            )
            .Logout(fun () -> Rpc.Logout() |> Async.Start)
            .Doc()

open WebSharper.UI.Next.Server

let Main =
    Application.SinglePage(fun ctx ->
        IndexTemplate()
            .Main(client <@ Client.Main() @>)
            .Doc()
        |> Content.Page
    )
