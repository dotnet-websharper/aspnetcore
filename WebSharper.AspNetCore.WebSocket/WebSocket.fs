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
namespace WebSharper.AspNetCore.WebSocket

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Threading.Tasks
open WebSharper
open System.Threading
open Microsoft.AspNetCore.Builder

[<RequireQualifiedAccess>]
type JsonEncoding =
    | Typed
    | Readable

type private Context = WebSharper.Web.Context

module private Async =

    let StartAsUnitTask (a : Async<unit>): Task =
        a |> Async.StartAsTask :> Task

    [<JavaScript>]
    let FoldAgent initState f =
        MailboxProcessor.Start(fun inbox ->
            let rec loop state : Async<unit> = async {
                let! msg = inbox.Receive()
                let! newState = f state msg
                return! loop newState
            }
            loop initState
        )

module private Helpers =
    let getScheme scheme =
            if scheme = "https" || scheme = "wss" then "wss"
            else "ws"

type WebSocketEndpoint<'S2C, 'C2S> [<JavaScript>] (uri : string) =

    // the uri of the websocket server
    [<JavaScript>]
    member this.URI = uri

    // the encoding of messages
    [<JavaScript>]
    member val JsonEncoding = JsonEncoding.Typed with get, set

    static member Create (baseUrl : string, route : string, [<Optional>] encoding: JsonEncoding) =
        let uri = System.Uri(System.Uri(baseUrl), route)
        let scheme = Helpers.getScheme uri.Scheme
        let wsuri = sprintf "%s://%s%s" scheme uri.Authority uri.AbsolutePath
        WebSocketEndpoint<'S2C, 'C2S>(
            wsuri,
            JsonEncoding = (if isNull (box encoding) then JsonEncoding.Typed else encoding)
        )

module MessageCoder =
    module J = WebSharper.Core.Json

    let ToJString (jP: J.Provider) (msg: 'T) =
        let enc = jP.GetEncoder<'T>()
        enc.Encode msg
        |> jP.Pack
        |> J.Stringify

    let FromJString (jP: J.Provider) str : 'T =
        let dec = jP.GetDecoder<'T>()
        J.Parse str
        |> dec.Decode

module Client =
    open WebSharper.JavaScript

    [<JavaScript>]
    type Message<'S2C> =
        | Message of 'S2C
        | Error
        | Open
        | Close

    [<JavaScript>]
    type WebSocketServer<'S2C, 'C2S>(conn: WebSocket, encode: 'C2S -> string) =
        member this.Connection = conn
        member this.Post (msg: 'C2S) = msg |> encode |> conn.Send

    type Agent<'S2C, 'C2S> = WebSocketServer<'S2C, 'C2S> -> Async<Message<'S2C> -> unit>

    type StatefulAgent<'S2C, 'C2S, 'State> = WebSocketServer<'S2C, 'C2S> -> Async<'State * ('State -> Message<'S2C> -> Async<'State>)>

    [<JavaScript>]
    let cacheSocket (socket: WebSocket) decode =
        let cache = Queue()
        let isOpen = ref false
        socket.Onopen <- fun () -> cache.Enqueue Message.Open; isOpen := true
        socket.Onclose <- fun () -> cache.Enqueue Message.Close
        socket.Onmessage <- fun msg -> cache.Enqueue (Message.Message (decode msg))
        socket.Onerror <- fun () -> cache.Enqueue Message.Error
        fun post ->
            Seq.iter post cache
            !isOpen

    [<JavaScript>]
    let getEncoding (encode: 'C2S -> string) (decode: string -> 'S2C) (jsonEncoding: JsonEncoding) =
        let encode, decode =
            match jsonEncoding with
            | JsonEncoding.Typed -> 
                // TODO do module imports
                Json.Stringify, fun x -> Json.Activate (Json.Parse x) [||]
            | _ -> encode, decode
        let decode (msg: MessageEvent) = decode (As<string> msg.Data)
        encode, decode

    [<JavaScript>]
    type WithEncoding =

        static member FromWebSocketStateful (encode: 'C2S -> string) (decode: string -> 'S2C) socket (agent : StatefulAgent<'S2C, 'C2S, 'State>) jsonEncoding =
            let encode, decode = getEncoding encode decode jsonEncoding
            let flush = cacheSocket socket decode
            let server = WebSocketServer(socket, encode)
            async {
                let! initState, func = agent server
                let agent = Async.FoldAgent initState func
                return! Async.FromContinuations <| fun (ok, ko, _) ->
                    let isOpen = flush agent.Post
                    socket.Onopen <- fun () ->
                        agent.Post Message.Open
                        ok server
                    socket.Onclose <- fun () ->
                        agent.Post Message.Close
                    socket.Onmessage <- fun msg ->
                        agent.Post (Message.Message (decode msg))
                    socket.Onerror <- fun () ->
                        agent.Post Message.Error
                        // TODO: test if this is right. Might be called multiple times
                        //       or after ok was already called.
                        ko <| System.Exception("Could not connect to the server.")
                    if isOpen then ok server
            }

        static member FromWebSocket (encode: 'C2S -> string) (decode: string -> 'S2C) socket (agent : Agent<'S2C, 'C2S>) jsonEncoding =
            let encode, decode = getEncoding encode decode jsonEncoding
            let flush = cacheSocket socket decode
            let server = WebSocketServer(socket, encode)

            async {
                let! proc = agent server
                return! Async.FromContinuations <| fun (ok, ko, _) ->
                    let isOpen = flush proc
                    socket.Onopen <- fun () ->
                        proc Message.Open
                        ok server
                    socket.Onclose <- fun () ->
                        proc Message.Close
                    socket.Onmessage <- fun msg ->
                        proc (Message.Message (decode msg))
                    socket.Onerror <- fun () ->
                        proc Message.Error
                        // TODO: test if this is right. Might be called multiple times
                        //       or after ok was already called.
                        ko <| System.Exception("Could not connect to the server.")
                    if isOpen then ok server
            }

        [<MethodImpl(MethodImplOptions.NoInlining)>]
        static member ConnectStateful encode decode (endpoint : WebSocketEndpoint<'S2C, 'C2S>) (agent : StatefulAgent<'S2C, 'C2S, 'State>) =
            let socket = new WebSocket(endpoint.URI)
            WithEncoding.FromWebSocketStateful encode decode socket agent endpoint.JsonEncoding

        [<MethodImpl(MethodImplOptions.NoInlining)>]
        static member Connect encode decode (endpoint : WebSocketEndpoint<'S2C, 'C2S>) (agent : Agent<'S2C, 'C2S>) =
            let socket = new WebSocket(endpoint.URI)
            WithEncoding.FromWebSocket encode decode socket agent endpoint.JsonEncoding

    [<Inline>]
    let FromWebSocket<'S2C, 'C2S> (socket: WebSocket) (agent: Agent<'S2C, 'C2S>) jsonEncoding =
        WithEncoding.FromWebSocket Json.Serialize Json.Deserialize socket agent jsonEncoding

    [<Inline>]
    let FromWebSocketStateful<'S2C, 'C2S, 'State> (socket: WebSocket) (agent: StatefulAgent<'S2C, 'C2S, 'State>) jsonEncoding =
        WithEncoding.FromWebSocketStateful Json.Serialize Json.Deserialize socket agent jsonEncoding

    [<Inline>]
    let Connect<'S2C, 'C2S> (endpoint: WebSocketEndpoint<'S2C, 'C2S>) (agent: Agent<'S2C, 'C2S>) =
        WithEncoding.Connect Json.Serialize Json.Deserialize endpoint agent

    [<Inline>]
    let ConnectStateful<'S2C, 'C2S, 'State> (endpoint: WebSocketEndpoint<'S2C, 'C2S>) (agent: StatefulAgent<'S2C, 'C2S, 'State>) =
        WithEncoding.ConnectStateful Json.Serialize Json.Deserialize endpoint agent

module Server =
    type Message<'C2S> =
        | Message of 'C2S
        | Error of exn
        | Close

    open System.Net.WebSockets
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Primitives

    [<AbstractClass>]
    type WebSocketConnection(maxMessageSize) =
        let maxMessageSize = defaultArg maxMessageSize (1024*64)
        let mCancellToken = new CancellationTokenSource() 
        let mutable mWebSocket: Net.WebSockets.WebSocket = null
        let mutable buffer: byte[] = null
        let mutable httpContext: HttpContext = null

        member x.MaxMessageSize = maxMessageSize

        member x.Context = httpContext

        member x.Close(status: WebSocketCloseStatus, reason: string) =
            mWebSocket.CloseAsync(status, reason, mCancellToken.Token)

        member x.Abort() =
            mCancellToken.Cancel()

        member x.SendBinary(buffer: ArraySegment<byte>, endOfMessage) =
            mWebSocket.SendAsync(buffer, WebSocketMessageType.Binary, endOfMessage, mCancellToken.Token)

        member x.SendBinary(buffer: byte[], endOfMessage) =
            x.SendBinary(ArraySegment(buffer), endOfMessage)

        member x.SendText(buffer: ArraySegment<byte>, endOfMessage) =
            mWebSocket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage, mCancellToken.Token)

        member x.SendText(buffer: byte[], endOfMessage) =
            x.SendText(ArraySegment(buffer), endOfMessage)

        member x.Send(buffer: ArraySegment<byte>, endOfMessage, messageType) =
            mWebSocket.SendAsync(buffer, messageType, endOfMessage, mCancellToken.Token)
            
        //abstract AuthenticateRequest: request:HttpRequest -> bool  
        //default x.AuthenticateRequest _ = true

        //abstract AuthenticateRequestAsync: request:HttpRequest -> Task<bool>
        //default x.AuthenticateRequestAsync _ = Task.FromResult true

        abstract OnOpen: unit -> unit
        default x.OnOpen() = ()
        
        abstract OnOpenAsync: unit -> Task
        default x.OnOpenAsync() = Task.FromResult<unit>() :> Task

        abstract OnMessageReceived: message:ArraySegment<byte> * messageType:WebSocketMessageType -> Task
        default x.OnMessageReceived(_, _) = Task.FromResult<unit>() :> Task

        abstract OnClose: closeStatus:Nullable<WebSocketCloseStatus> * closeStatusDescription:string -> unit
        default x.OnClose(_, _) = ()

        abstract OnCloseAsync: closeStatus:Nullable<WebSocketCloseStatus> * closeStatusDescription:string -> Task
        default x.OnCloseAsync(_, _) = Task.FromResult<unit>() :> Task

        abstract OnReceiveError: error:exn -> unit
        default x.OnReceiveError(_) = ()

        member internal x.AcceptSocketAsync(httpCtx: HttpContext) =
            let rec receive() =
                async {
                    let mutable cont = true
                    try
                        let! received = mWebSocket.ReceiveAsync(ArraySegment(buffer), mCancellToken.Token) |> Async.AwaitTask
                    
                        if received.Count > 0 then
                            if not received.EndOfMessage then
                                raise (System.IO.InternalBufferOverflowException("WebSocket message size has exceeded maxMessageSize=" + string maxMessageSize))
                            do! x.OnMessageReceived(ArraySegment(buffer, 0, received.Count), received.MessageType) |> Async.AwaitTask

                        if received.MessageType = WebSocketMessageType.Close then
                            cont <- false    
                    with
                    | :? TaskCanceledException 
                    | :? ObjectDisposedException ->
                        cont <- false
                    // If this exception is due to the underlying TCP connection going away, treat as a normal close
                    // rather than a fatal exception.
                    | :? COMException as ce 
                        when ce.ErrorCode = 0x800703e3 || ce.ErrorCode = 0x800704cd || ce.ErrorCode = 0x80070026 ->
                        ()
                    | err ->
                        if not mCancellToken.IsCancellationRequested  then 
                            x.OnReceiveError(err)
                        cont <- false
                    if cont then
                        do! receive()
                }
            
            async {
                //httpCtx.Response.Headers.Add("X-Content-Type-Options", StringValues.op_Implicit "nosniff")
                let! webSocket = httpCtx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                mWebSocket <- webSocket
                httpContext <- httpCtx
                //if x.AuthenticateRequest(httpCtx.Request) then
                //    let! authorized = x.AuthenticateRequestAsync(httpCtx.Request) |> Async.AwaitTask    
                //    if authorized then
                //        let! webSocket = httpCtx.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
                //        mWebSocket <- webSocket
                //    elif httpCtx.Request.

                x.OnOpen()
                do! x.OnOpenAsync() |> Async.AwaitTask

                buffer <- Array.zeroCreate maxMessageSize
                do! receive()  

                try
                    do! mWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", mCancellToken.Token) |> Async.AwaitTask
                with _ -> ()

                if not mCancellToken.IsCancellationRequested then
                    mCancellToken.Cancel()

                x.OnClose(mWebSocket.CloseStatus, mWebSocket.CloseStatusDescription)
                do! x.OnCloseAsync(mWebSocket.CloseStatus, mWebSocket.CloseStatusDescription) |> Async.AwaitTask

            }

    type WebSocketClient<'S2C, 'C2S>(conn: WebSocketConnection, ctx, jP) =
        let onMessage = Event<'C2S>()
        let onClose = Event<unit>()
        let onError = Event<exn>()

        member this.JsonProvider = jP
        member this.Connection = conn
        member this.Context : Context = ctx
        member this.PostAsync (value: 'S2C) =
            let msg = MessageCoder.ToJString jP value
            let bytes = System.Text.Encoding.UTF8.GetBytes(msg)
            conn.SendText(bytes, true) |> Async.AwaitTask
        member this.Post (value: 'S2C) = this.PostAsync value |> Async.Start
        member this.OnMessage = onMessage.Publish
        member this.OnClose = onClose.Publish
        member this.OnError = onError.Publish

        member internal this.Close() = onClose.Trigger()
        member internal this.Message msg = onMessage.Trigger(msg)
        member internal this.Error e = onError.Trigger(e)

    type Agent<'S2C, 'C2S> = WebSocketClient<'S2C, 'C2S> -> Async<Message<'C2S> -> unit>

    type StatefulAgent<'S2C, 'C2S, 'State> = WebSocketClient<'S2C, 'C2S> -> Async<'State * ('State -> Message<'C2S> -> Async<'State>)>

    [<RequireQualifiedAccess>]
    type CustomMessage<'C2S, 'Custom> =
        | Message of 'C2S
        | Custom of 'Custom
        | Error of exn
        | Close

    type CustomWebSocketAgent<'S2C, 'C2S, 'Custom>(client: WebSocketClient<'S2C, 'C2S>) =
        let onCustom = Event<'Custom>()
        member this.Client = client
        member this.PostCustom (value: 'Custom) = onCustom.Trigger value
        member this.OnCustom = onCustom.Publish

    type CustomAgent<'S2C, 'C2S, 'Custom, 'State> = CustomWebSocketAgent<'S2C, 'C2S, 'Custom> -> Async<'State * ('State -> CustomMessage<'C2S, 'Custom> -> Async<'State>)>

type private WebSharperWebSocketConnection<'S2C, 'C2S>(maxMessageSize, ctx, jP, agent: Server.Agent<'S2C, 'C2S>) =
    inherit Server.WebSocketConnection(maxMessageSize)
    let mutable post : option<Server.Message<'C2S> -> unit> = None
    //val private processor : WebSocketProcessor<'S2C, 'C2S>

    override x.OnClose(status, desc) =
        post |> Option.iter (fun p -> p Server.Close)

    //override x.AuthenticateRequest(req) =
    //    x.processor.AuthenticateRequest |> Option.forall (fun o -> o x.Context.Environment)    

    //override x.AuthenticateRequestAsync(req) =
    //    x.AuthenticateRequest(req)
    //    |> System.Threading.Tasks.Task.FromResult 

    override x.OnOpenAsync() =
        let cl = Server.WebSocketClient(x, ctx, jP)
        async {
            let! a = agent cl
            post <- Some a
        }
        |> Async.StartAsTask :> _


    override x.OnMessageReceived(message, typ) =
        async {
            let json = System.Text.Encoding.UTF8.GetString(message.Array)
            let m = MessageCoder.FromJString jP json
            post.Value(Server.Message m)
        }
        |> Async.StartAsTask :> _

    override x.OnReceiveError(ex) =
        post.Value(Server.Error ex)

open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Builder
open WebSharper.AspNetCore

module private Middleware =
    let Create<'S2C, 'C2S> 
        (
            route: string,
            agent: Server.Agent<'S2C, 'C2S>, 
            wsOptions: WebSharperOptions, 
            jsonEncoding: JsonEncoding,
            maxMessageSize : option<int> 
            //onAuth: Func<HttpRequest, bool>,
            //onAuthAsync: Func<HttpRequest, bool>
        ) 
        : Func<HttpContext, Func<Task>, Task> =
        let json =
            match jsonEncoding with
            | JsonEncoding.Typed -> wsOptions.Json
            | JsonEncoding.Readable -> WebSharper.Web.Shared.PlainJson
        Func<_,_,_>(fun (httpCtx: HttpContext) (next: Func<Task>) -> 
            let ctx = Context.GetOrMakeSimple httpCtx wsOptions
            let ep = (if route.StartsWith "/" then "" else "/") + route 
            if httpCtx.Request.Path.HasValue && httpCtx.Request.Path.Value = ep && httpCtx.WebSockets.IsWebSocketRequest then
                let conn = WebSharperWebSocketConnection(maxMessageSize, ctx, json, agent)
                conn.AcceptSocketAsync(httpCtx) |> Async.StartAsUnitTask
            else
                next.Invoke()
        )

    let AdaptStatefulAgent (agent: Server.StatefulAgent<'S2C, 'C2S, 'State>) : Server.Agent<'S2C, 'C2S> =
        let agent client = async {
            let! initState, receive = agent client
            let receive state msg =
                async {
                    try return! receive state msg
                    with exn ->
                        try return! receive state (Server.Error exn)
                        with exn -> return state
                }
            let agent = Async.FoldAgent initState receive
            return agent.Post
        }   
        agent

    let AdaptCustomAgent (agent: Server.CustomAgent<'S2C, 'C2S, 'Custom, 'State>) : Server.Agent<'S2C, 'C2S> =
        let agent client = async {
            let client = Server.CustomWebSocketAgent(client)
            let! initState, receive = agent client
            let receive state msg =
                async {
                    try return! receive state msg
                    with exn ->
                        try return! receive state (Server.CustomMessage.Error exn)
                        with exn -> return state
                }
            let agent = Async.FoldAgent initState receive
            client.OnCustom.Add (Server.CustomMessage.Custom >> agent.Post)
            return function
            | Server.Close -> agent.Post Server.CustomMessage.Close
            | Server.Error e -> agent.Post (Server.CustomMessage.Error e)
            | Server.Message m -> agent.Post (Server.CustomMessage.Message m)
        }
        agent

type WebSharperWebSocketBuilder() =
    let mutable _maxMessageSize = None
    let mutable _jsonEncoding = JsonEncoding.Typed
    let mutable onBuild = ignore
    //let mutable _onAuth = Func<HttpRequest, bool>(fun _ -> true)
    //let mutable _onAuthAsync = Func<HttpRequest, Task<bool>>(fun _ -> Task.FromResult(true))

    member this.MaxMessageSize(maxMessageSize: int) =
        _maxMessageSize <- Some maxMessageSize
        this

    member this.JsonEncoding(jsonEncoding: JsonEncoding) =
        _jsonEncoding <- jsonEncoding
        this

    member this.Use(agent: Server.Agent<'S2C, 'C2S>) =
        onBuild <-
            fun (wsBuilder: WebSharperBuilder, route) -> 
                wsBuilder.Use(fun appBuilder wsOptions ->
                    appBuilder.Use(Middleware.Create<'S2C, 'C2S>(route, agent, wsOptions, _jsonEncoding, _maxMessageSize))
                    |> ignore
                )
        this

    member this.Use(agent: Server.StatefulAgent<'S2C, 'C2S, 'State>) =
        this.Use(Middleware.AdaptStatefulAgent agent)

    member this.Use(agent: Server.CustomAgent<'S2C, 'C2S, 'Custom, 'State>) =
        this.Use(Middleware.AdaptCustomAgent agent)

    //member this.AuthenticateRequest(onAuth) =
    //    _onAuth <- onAuth
    //    this

    //member this.AuthenticateRequest(onAuthAsync) =
    //    _onAuthAsync <- onAuthAsync
    //    this

    member internal this.Build(wsBuilder: WebSharperBuilder, route) =
        onBuild (wsBuilder, route)

[<Extension; Sealed>]
type Extensions =

    [<Extension>]
    static member UseWebSocket
        (
            this: WebSharperBuilder, 
            route: string, 
            [<Optional>] build: System.Action<WebSharperWebSocketBuilder>
        ) =
        let builder = WebSharperWebSocketBuilder()
        if not (isNull build) then build.Invoke(builder)
        builder.Build(this, route)

    //[<Extension>]
    //static member UseWebSocket
    //    (
    //        this: WebSharperBuilder, 
    //        route: string, 
    //        agent: Server.StatefulAgent<'S2C, 'C2S, 'State>, 
    //        [<Optional>] build: System.Action<WebSharperWebSocketBuilder>
    //    ) =
    //    this.UseWebSocket(route, Middleware.AdaptStatefulAgent agent, build)

    //[<Extension>]
    //static member private UseWebSocket
    //    (
    //        this: WebSharperBuilder, 
    //        route: string, 
    //        agent: Server.CustomAgent<'S2C, 'C2S, 'Custom, 'State>, 
    //        [<Optional>] build: System.Action<WebSharperWebSocketBuilder>
    //    ) =
    //    this.UseWebSocket(route, Middleware.AdaptCustomAgent agent, build)
