using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WebSharper;

namespace WebSharper.AspNetCore.Tests.CSharp
{
    public static class Remoting
    {
        [Remote]
        public static Task<string> DoSomething(string input)
        {
            return Task.FromResult(new String(input.ToCharArray().Reverse().ToArray()));
        }
    }
}