// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2020 IntelliFactory
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
module WebSharper.AspNetCore.Tests.WebSocketClient

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.AspNetCore.WebSocket
open WebSharper.AspNetCore.WebSocket.Client

module Server = WebSocketServer

[<JavaScript>]
let WebSocketTest (endpoint : WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage>) =
    let container = Elt.pre [] []
    let writen fmt =
        Printf.ksprintf (fun s ->
            JS.Document.CreateTextNode(s + "\n")
            |> container.Dom.AppendChild
            |> ignore
        ) fmt
    async {
        do
            writen "Checking regression #4..."
            JQuery.JQuery.Ajax(
                JQuery.AjaxSettings(
                    Url = "/ws.txt",
                    Method = JQuery.RequestType.GET,
                    Success = (fun x _ _ -> writen "%s" (x :?> _)),
                    Error = (fun _ _ e -> writen "KO: %s." e)
                )
            ) |> ignore

        let! server =
            ConnectStateful endpoint <| fun server -> async {
                return 0, fun state msg -> async {
                    match msg with
                    | Message data ->
                        match data with
                        | Server.Response1 x -> writen "Response1 %s (state: %i)" x state
                        | Server.Response2 x -> writen "Response2 %i (state: %i)" x state
                        return (state + 1)
                    | Close ->
                        writen "WebSocket connection closed."
                        return state
                    | Open ->
                        writen "WebSocket connection open."
                        return state
                    | Error ->
                        writen "WebSocket connection error!"
                        return state
                }
            }
        
        let lotsOfHellos = "HELLO" |> Array.create 1000
        let lotsOf123s = 123 |> Array.create 1000

        while true do
            do! Async.Sleep 1000
            server.Post (Server.Request1 [| "HELLO" |])
            do! Async.Sleep 1000
            server.Post (Server.Request2 lotsOf123s)
    }
    |> Async.Start

    container

open WebSharper.AspNetCore.WebSocket

let MyEndPoint (url: string) : WebSharper.AspNetCore.WebSocket.WebSocketEndpoint<Server.S2CMessage, Server.C2SMessage> = 
    WebSocketEndpoint.Create(url, "/ws", JsonEncoding.Readable)