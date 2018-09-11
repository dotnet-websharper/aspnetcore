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
type ISiteletService<'T when 'T : equality>() =
    abstract Sitelet : Sitelet<'T>

    interface ISiteletService with
        member this.Sitelet = Sitelet.Box this.Sitelet

type DefaultSiteletService<'T when 'T : equality>(sitelet: Sitelet<'T>) =
    inherit ISiteletService<'T>()

    override this.Sitelet = sitelet

/// Define a remoting provider to serve by WebSharper.
type IRemotingService =
    abstract Register : unit -> unit

type RemotingService<'TProvider, 'TInstance>(provider: 'TInstance) =
    interface IRemotingService with
        member this.Register() =
            WebSharper.Core.Remoting.AddHandler typeof<'TProvider> provider

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

    /// Add a remoting provider to be loaded on startup with UseWebSharper.
    /// The client can invoke it using WebSharper.JavaScript.Pervasives.Remote<TProvider>.
    [<Extension>]
    static member AddWebSharperRemoting<'TProvider when 'TProvider : not struct>
            (this: IServiceCollection) =
        this.AddSingleton<'TProvider, 'TProvider>()
            .AddSingleton<IRemotingService, RemotingService<'TProvider, 'TProvider>>()

    /// Add a remoting provider to be loaded on startup with UseWebSharper.
    /// The client can invoke it using WebSharper.JavaScript.Pervasives.Remote<TProvider>.
    [<Extension>]
    static member AddWebSharperRemoting<'TProvider, 'TInstance when 'TInstance : not struct>
            (this: IServiceCollection) =
        this.AddSingleton(ServiceDescriptor(typeof<'TProvider>, typeof<'TInstance>))
            .AddSingleton<IRemotingService, RemotingService<'TProvider, 'TInstance>>()

    /// Add a remoting provider to be loaded on startup with UseWebSharper.
    /// The client can invoke it using WebSharper.JavaScript.Pervasives.Remote<TProvider>.
    [<Extension>]
    static member AddWebSharperRemoting<'TProvider when 'TProvider : not struct>
            (this: IServiceCollection, provider: 'TProvider) =
        this.AddSingleton<'TProvider>(provider)
            .AddSingleton<IRemotingService>(RemotingService provider)
