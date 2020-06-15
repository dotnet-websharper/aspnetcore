using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.FSharp.Core;
using WebSharper;
using WebSharper.UI;
using WebSharper.UI.Client;
using static WebSharper.UI.Client.Html;

namespace WebSharper.AspNetCore.Tests.CSharp
{
    [JavaScript]
    public static class Client
    {
        static public IControlBody ClientMain()
        {
            var rvInput = Var.Create("");
            var submit = Submitter.CreateOption(rvInput.View);
            var vReversed =
                submit.View.MapAsync(input =>
                {
                    if (input == null)
                        return Task.FromResult("");
                    return Remoting.DoSomething(input.Value);
                });
            return div(
                input(rvInput),
                button("Send", submit.Trigger),
                hr(),
                h4(
                    attr.@class("text-muted"),
                    "The server responded:",
                    div(
                        attr.@class("jumbotron"),
                        h1(vReversed)
                    )
                )
            );
        }
    }
}