namespace WebSharper.AspNetCore

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open WebSharper.Sitelets

/// Define the sitelet to serve by WebSharper.
[<AllowNullLiteral>]
type ISiteletService =
    abstract Sitelet : Sitelet<obj>

/// Define the sitelet to serve by WebSharper.
[<AbstractClass>]
type SiteletService<'T when 'T : equality>() =
    abstract Sitelet : Sitelet<'T>

    interface ISiteletService with
        member this.Sitelet = Sitelet.Box this.Sitelet

type DefaultSiteletService<'T when 'T : equality>(sitelet: Sitelet<'T>) =
    inherit SiteletService<'T>()

    override this.Sitelet = sitelet

/// Define a remoting handler to serve by WebSharper.
type IRemotingService =
    abstract Register : unit -> unit

type RemotingService<'THandler, 'TInstance>(handler: 'TInstance) =
    interface IRemotingService with
        member this.Register() =
            WebSharper.Core.Remoting.AddHandler typeof<'THandler> handler

[<Extension>]
type ServiceExtensions =

    /// Add a sitelet service to be loaded on startup with UseWebSharper.
    [<Extension>]
    static member AddSitelet<'TImplementation
            when 'TImplementation :> ISiteletService
            and 'TImplementation : not struct>
            (this: IServiceCollection) =
        this.AddSingleton<ISiteletService, 'TImplementation>()

    /// Add a sitelet to be loaded on startup with UseWebSharper.
    [<Extension>]
    static member AddSitelet<'T when 'T : equality>
            (this: IServiceCollection, sitelet: Sitelet<'T>) =
        this.AddSingleton<ISiteletService>(DefaultSiteletService sitelet)

    /// Add a remoting handler to be loaded on startup with UseWebSharper.
    /// The client can invoke it using WebSharper.JavaScript.Pervasives.Remote<THandler>.
    [<Extension>]
    static member AddWebSharperRemoting<'THandler when 'THandler : not struct>
            (this: IServiceCollection) =
        this.AddSingleton<'THandler, 'THandler>()
            .AddSingleton<IRemotingService, RemotingService<'THandler, 'THandler>>()

    /// Add a remoting handler to be loaded on startup with UseWebSharper.
    /// The client can invoke it using WebSharper.JavaScript.Pervasives.Remote<THandler>.
    [<Extension>]
    static member AddWebSharperRemoting<'THandler, 'TInstance when 'TInstance : not struct>
            (this: IServiceCollection) =
        this.AddSingleton(ServiceDescriptor(typeof<'THandler>, typeof<'TInstance>))
            .AddSingleton<IRemotingService, RemotingService<'THandler, 'TInstance>>()

    /// Add a remoting handler to be loaded on startup with UseWebSharper.
    /// The client can invoke it using WebSharper.JavaScript.Pervasives.Remote<THandler>.
    [<Extension>]
    static member AddWebSharperRemoting<'THandler when 'THandler : not struct>
            (this: IServiceCollection, handler: 'THandler) =
        this.AddSingleton<'THandler>(handler)
            .AddSingleton<IRemotingService>(RemotingService handler)
