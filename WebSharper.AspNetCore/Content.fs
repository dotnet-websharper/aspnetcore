// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2018 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}
module WebSharper.AspNetCore.Content

#nowarn "44"

open System
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open WebSharper.Sitelets

let Authorize (services: IServiceProvider) (onFail: Context<'EndPoint> -> Async<Content<'EndPoint>>) (authData: seq<IAuthorizeData>) =
    let authPolicyProvider = services.GetService<IAuthorizationPolicyProvider>()
    let auth = services.GetService<IAuthorizationService>()
    let tAuthPolicy =
        if isNull authPolicyProvider then
            failwithf "An endpoint is configured for authorization, \
                but the application has no authorization policy. \
                Add .AddAuthorization() to your ASP.NET Core services."
        else
            AuthorizationPolicy.CombineAsync(authPolicyProvider, authData)
    fun (content: string -> Async<Content<'EndPoint>>) -> async {
        return CustomContentAsync <| fun ctx -> async {
            let httpCtx = ctx.Environment.["WebSharper.AspNetCore.HttpContext"] :?> HttpContext
            let! authPolicy = tAuthPolicy |> Async.AwaitTask
            let! authResult = auth.AuthorizeAsync(httpCtx.User, authPolicy) |> Async.AwaitTask
            if authResult.Succeeded then
                let! user = ctx.UserSession.GetLoggedInUser()
                let! content = content user.Value
                return! Content.ToResponse content ctx
            else
                let! content = onFail ctx
                return! Content.ToResponse content ctx
        }
    }
