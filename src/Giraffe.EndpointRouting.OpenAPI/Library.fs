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
type SwaggerHttpHandler = SwaggerHttpHandler of metadata: MetadataList * handler: HttpHandler

/// represents a endpoint with additional swagger metadata (eg what we want to return out of `GET` combinators, etc)
type SwaggerEndpoint = SwaggerEndpoint of metadata: MetadataList * endpoint: Routers.Endpoint

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
    call (Unchecked.defaultof<Composer>, l, Unchecked.defaultof<SwaggerHttpHandler>, r)


[<Extension>]
type EndpointRouteBuilderExtensions() =
    [<Extension>]
    static member MapGiraffeEndpoints
        (builder  : IEndpointRouteBuilder,
        endpoints : SwaggerEndpoint list) =

        let mappedEndpoints =
            endpoints
            |> List.map(fun (SwaggerEndpoint(metadata, endpoint)) -> (endpoint, metadata) ||> List.fold (fun e m -> addMetadata m e))

        builder.MapGiraffeEndpoints(mappedEndpoints)
