namespace Giraffe.EndpointRouting.OpenAPI

module Combinators =
  open Giraffe
  open Giraffe.EndpointRouting
  open Giraffe.EndpointRouting.OpenAPI.Composition

  // helper to fold a set of metadata onto an endpoint
  let applyMetadatas metadata endpoint =
    (endpoint, metadata)
    ||> List.fold (fun e m -> addMetadata m e)

  type Router =
    static member inline Route(path, SwaggerHttpHandler (metadata, handler)) : Endpoint =
      Giraffe.EndpointRouting.Routers.route path handler
      |> applyMetadatas metadata

    static member inline Route(path, handler) : Endpoint =
      Giraffe.EndpointRouting.Routers.route path handler

    static member ApplyBefore(SwaggerHttpHandler (metadata, handler), endpoint) : Endpoint =
      Giraffe.EndpointRouting.Routers.applyBefore handler endpoint
      |> applyMetadatas metadata

    static member ApplyBefore(handler, endpoint) : Endpoint =
      Giraffe.EndpointRouting.Routers.applyBefore handler endpoint

    static member ApplyAfter(SwaggerHttpHandler (metadata, handler), endpoint) : Endpoint =
      Giraffe.EndpointRouting.Routers.applyAfter handler endpoint
      |> applyMetadatas metadata

    static member ApplyAfter(handler, endpoint) : Endpoint =
      Giraffe.EndpointRouting.Routers.applyAfter handler endpoint
