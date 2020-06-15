using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WebSharper;
using WebSharper.Sitelets;
using WebSharper.UI;
using static WebSharper.UI.Html;

namespace WebSharper.AspNetCore.Tests.CSharp
{
    public class Site
    {
        [EndPoint("/")]
        public class Home {
            public override bool Equals(object obj) => obj is Home;
            public override int GetHashCode() => 0;
        }

        [EndPoint("GET /about")]
        public class About {
            public override bool Equals(object obj) => obj is About;
            public override int GetHashCode() => 1;
        }

        public static Doc MenuBar(Context<object> ctx, object endpoint)
        {
            Doc link(string txt, object act) =>
                li(
                    endpoint.Equals(act) ? attr.@class("active") : null,
                    a(attr.href(ctx.Link(act)), txt)
                );
            return doc(
                li(link("Home", new Home())),
                li(link("About", new About()))  
            );
        }

        public static Task<Content> Page(Context<object> ctx, object endpoint, string title, Doc body) =>
            Content.Page(
                new Template.Main()
                    .Title(title)
                    .MenuBar(MenuBar(ctx, endpoint))
                    .Body(body)
                    .Doc()
            );

        [Website]
        public static Sitelet<object> Main =>
            new SiteletBuilder()
                .With<Home>((ctx, action) =>
                    Page(ctx, action, "Home",
                        doc(
                            h1("Say Hi to the server!"),
                            div(client(() => Client.ClientMain()))
                        )
                    )
                )
                .With<About>((ctx, action) =>
                    Page(ctx, action, "About",
                        doc(
                            h1("About"),
                            p("This is a template WebSharper client-server application.")
                        )
                    )
                )
                .Install();
    }
}