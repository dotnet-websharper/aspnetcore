#r "paket: groupref build //"
#load "paket-files/wsbuild/github.com/dotnet-websharper/build-script/WebSharper.Fake.fsx"
#r "System.Xml.Linq"

// Only reference these packages from editors/non fake-cli tools
#if !FAKE
    // To have proper language service in the editor
    #r "netstandard"
    // To help FAKE related IntelliSense in editor
    #load "./.fake/build.fsx/intellisense_lazy.fsx"
#endif

open WebSharper.Fake

LazyVersionFrom "WebSharper" |> WSTargets.Default
|> MakeTargets
|> RunTargets
