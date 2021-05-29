#if INTERACTIVE
#r "nuget: Giraffe"
#I "/usr/local/share/dotnet/packs/Microsoft.AspNetCore.App.Ref/5.0.0/ref/net5.0/"
#r "Microsoft.AspNetCore.Html.Abstractions.dll"
#r "Microsoft.AspNetCore.Routing.dll"
#endif

#if !INTERACTIVE
module Giraffe.EndpointRouting.OpenAPI
#endif

open Giraffe
open Giraffe.EndpointRouting
open Microsoft.AspNetCore.Routing
open System.Runtime.CompilerServices

type Metadata = obj
type MetadataList = obj list

/// Represents a handler that contains associated endpoint metadata.
/// When processed, the metadata is added to the generated Endpoint
type SwaggerHttpHandler =
    | SwaggerHttpHandler of metadata: MetadataList * handler: HttpHandler

// helper type to make it seamless to weave in swagger-enabled endpoints into your existing pipelines
type Composer =
    static member inline Compose (SwaggerHttpHandler(lmetadata, lhandler), (SwaggerHttpHandler(rmetadata, rhandler))): SwaggerHttpHandler =
        SwaggerHttpHandler(lmetadata @ rmetadata, Giraffe.Core.compose lhandler rhandler)
    static member inline Compose (SwaggerHttpHandler(lmetadata, lhandler), rhandler: HttpHandler): SwaggerHttpHandler =
        SwaggerHttpHandler(lmetadata, Giraffe.Core.compose lhandler rhandler)
    static member inline Compose (lhandler: HttpHandler, SwaggerHttpHandler(rmetadata, rhandler)): SwaggerHttpHandler =
        SwaggerHttpHandler(rmetadata, Giraffe.Core.compose lhandler rhandler)
    static member inline Compose (lhandler: HttpHandler, rhandler: HttpHandler): SwaggerHttpHandler =
        SwaggerHttpHandler([], Giraffe.Core.compose lhandler rhandler)

let inline (>=>) (l: ^l) (r: ^r) =
    let inline call (_mthd: 'M, input: 'I, _output: 'R, f) = ((^M or ^I or ^R) : (static member Compose : _*_ -> _) input, f)
    call (Unchecked.defaultof<Composer>, l, Unchecked.defaultof< ^R >, r)

[<AutoOpen>]
module Combinators =
    let applyMetadatas metadata endpoint =
        (endpoint, metadata)
        ||> List.fold (fun e m -> addMetadata m e)

    type Router =
        static member inline Route (path, SwaggerHttpHandler(metadata, handler)): Endpoint =
            Giraffe.EndpointRouting.Routers.route path handler
            |> applyMetadatas metadata
        static member inline Route (path, handler): Endpoint =
            Giraffe.EndpointRouting.Routers.route path handler

        static member ApplyBefore (SwaggerHttpHandler(metadata, handler), endpoint): Endpoint =
            Giraffe.EndpointRouting.Routers.applyBefore handler endpoint
            |> applyMetadatas metadata
        static member ApplyBefore (handler, endpoint): Endpoint =
            Giraffe.EndpointRouting.Routers.applyBefore handler endpoint

        static member ApplyAfter (SwaggerHttpHandler(metadata, handler), endpoint): Endpoint =
            Giraffe.EndpointRouting.Routers.applyAfter handler endpoint
            |> applyMetadatas metadata
        static member ApplyAfter (handler, endpoint): Endpoint =
            Giraffe.EndpointRouting.Routers.applyAfter handler endpoint

    let inline route path (handler: ^h): Endpoint =
        let inline call (_mthd: 'M, path: string, dummy: 'D, handler: 'R) = ((^M or ^R or ^D) : (static member Route: string * ^R -> ^D) path, handler)
        call (Unchecked.defaultof<Router>, path, Unchecked.defaultof<Endpoint>, handler)

    let inline applyBefore (handler: ^h) endpoint: Endpoint =
        let inline call (_mthd: 'M, handler: 'h, dummy: 'D, endpoint: 'R) = ((^M or ^R or ^d) : (static member ApplyBefore: ^h * ^R -> ^R) handler, endpoint)
        call (Unchecked.defaultof<Router>, handler, Unchecked.defaultof<Endpoint>, endpoint)

    let inline applyAfter (handler: ^h) endpoint =
        let inline call (_mthd: 'M, handler: 'h, dummy: 'D, endpoint: 'R) = ((^M or ^R or ^d) : (static member ApplyAfter: ^h * ^R -> ^R) handler, endpoint)
        call (Unchecked.defaultof<Router>, handler, Unchecked.defaultof<Endpoint>, endpoint)
