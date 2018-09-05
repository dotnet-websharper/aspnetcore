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
        ServiceDescriptor(typeof<ISiteletService>, DefaultSiteletService(sitelet))
        |> this.Add
