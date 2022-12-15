//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.FSharp.Core;
using WebSharper;
using WebSharper.UI;
using WebSharper.UI.Templating;
using SDoc = WebSharper.UI.Doc;
using DomElement = WebSharper.JavaScript.Dom.Element;
using DomEvent = WebSharper.JavaScript.Dom.Event;
using static WebSharper.UI.Templating.Runtime.Server;
using static WebSharper.JavaScript.Pervasives;
namespace WebSharper.AspNetCore.Tests.ANC50.CSharp.Template
{
    [JavaScript]
    public class Main
    {
        string key = System.Guid.NewGuid().ToString();
        List<TemplateHole> holes = new List<TemplateHole>();
        Instance instance;
        public Main Title(string x) { holes.Add(TemplateHole.NewText("title", x)); return this; }
        public Main Title(View<string> x) { holes.Add(TemplateHole.NewTextView("title", x)); return this; }
        public Main MenuBar(Doc x) { holes.Add(TemplateHole.NewElt("menubar", x)); return this; }
        public Main MenuBar(IEnumerable<Doc> x) { holes.Add(TemplateHole.NewElt("menubar", SDoc.Concat(x))); return this; }
        public Main MenuBar(params Doc[] x) { holes.Add(TemplateHole.NewElt("menubar", SDoc.Concat(x))); return this; }
        public Main MenuBar(string x) { holes.Add(TemplateHole.NewText("menubar", x)); return this; }
        public Main MenuBar(View<string> x) { holes.Add(TemplateHole.NewTextView("menubar", x)); return this; }
        public Main Body(Doc x) { holes.Add(TemplateHole.NewElt("body", x)); return this; }
        public Main Body(IEnumerable<Doc> x) { holes.Add(TemplateHole.NewElt("body", SDoc.Concat(x))); return this; }
        public Main Body(params Doc[] x) { holes.Add(TemplateHole.NewElt("body", SDoc.Concat(x))); return this; }
        public Main Body(string x) { holes.Add(TemplateHole.NewText("body", x)); return this; }
        public Main Body(View<string> x) { holes.Add(TemplateHole.NewTextView("body", x)); return this; }
        public Main scripts(Doc x) { holes.Add(TemplateHole.NewElt("scripts", x)); return this; }
        public Main scripts(IEnumerable<Doc> x) { holes.Add(TemplateHole.NewElt("scripts", SDoc.Concat(x))); return this; }
        public Main scripts(params Doc[] x) { holes.Add(TemplateHole.NewElt("scripts", SDoc.Concat(x))); return this; }
        public Main scripts(string x) { holes.Add(TemplateHole.NewText("scripts", x)); return this; }
        public Main scripts(View<string> x) { holes.Add(TemplateHole.NewTextView("scripts", x)); return this; }
        public struct Vars
        {
        }
        public struct Anchors
        {
        }
        public class Instance : TemplateInstance
        {
            public Instance(CompletedHoles v, Doc d) : base(v, d) { }
            public Vars Vars => As<Vars>(this);
            public Anchors Anchors => As<Anchors>(this);
        }
        public Instance Create() {
            var (completed, initializer) = Handler.CompleteHoles(key, holes, new Tuple<string, ValTy, FSharpOption<object>>[] {  });
            var doc = Runtime.GetOrLoadTemplate("main", null, FSharpOption<string>.Some("Main.html"), "<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"utf-8\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>${Title}</title>\r\n    <link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.4/css/bootstrap.min.css\">\r\n    <style>\r\n        /* Sticky footer styles */\r\n        html {\r\n            position: relative;\r\n            min-height: 100%;\r\n        }\r\n\r\n        body {\r\n            /* Margin bottom by footer height */\r\n            margin-bottom: 60px;\r\n        }\r\n\r\n        .footer {\r\n            position: absolute;\r\n            bottom: 0;\r\n            width: 100%;\r\n            /* Set the fixed height of the footer here */\r\n            height: 60px;\r\n            background-color: #f5f5f5;\r\n        }\r\n\r\n        .container .text-muted {\r\n            margin: 20px 0;\r\n        }\r\n    </style>\r\n</head>\r\n<body>\r\n    <!-- Static navbar -->\r\n    <nav class=\"navbar navbar-default navbar-static-top\">\r\n        <div class=\"container\">\r\n            <div class=\"navbar-header\">\r\n                <button type=\"button\" class=\"navbar-toggle collapsed\" data-toggle=\"collapse\" data-target=\"#navbar\" aria-expanded=\"false\" aria-controls=\"navbar\">\r\n                    <span class=\"sr-only\">Toggle navigation</span>\r\n                    <span class=\"icon-bar\"></span>\r\n                    <span class=\"icon-bar\"></span>\r\n                </button>\r\n                <a class=\"navbar-brand\" href=\"#\">Your App</a>\r\n            </div>\r\n            <div id=\"navbar\" class=\"navbar-collapse collapse\">\r\n                <ul class=\"nav navbar-nav\" ws-hole=\"MenuBar\"></ul>\r\n                <ul class=\"nav navbar-nav navbar-right\">\r\n                    <li><a href=\"http://websharper.com\">websharper.com</a></li>\r\n                </ul>\r\n            </div><!--/.nav-collapse -->\r\n        </div>\r\n    </nav>\r\n    <div class=\"container\">\r\n        <div ws-replace=\"Body\">\r\n        </div>\r\n    </div>\r\n    <footer class=\"footer\">\r\n        <div class=\"container\">\r\n            <p class=\"text-muted\">\r\n                For an enhanced template that provides automatic GitHub deployment to Azure, fork from <a href=\"https://github.com/intellifactory/ClientServer.Azure\">GitHub</a>, or\r\n                read more <a href=\"http://websharper.com/blog-entry/4368/deploying-websharper-apps-to-azure-via-github\">here</a>.\r\n            </p>\r\n        </div>\r\n    </footer>\r\n    <script ws-replace=\"scripts\"></script>\r\n</body>\r\n</html>", null, completed, FSharpOption<string>.Some("main"), ServerLoad.WhenChanged, new Tuple<string, FSharpOption<string>, string>[] {  }, initializer, true, false, false);
            instance = new Instance(initializer, doc);
            return instance;
        }
        public Doc Doc() => Create().Doc;
        [Inline] public Elt Elt() => (Elt)Doc();
    }
}