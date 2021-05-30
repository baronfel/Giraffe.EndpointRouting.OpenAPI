namespace Giraffe.EndpointRouting.OpenAPI

module Composition =
  open Giraffe

  /// the payload we get back from aspnetcore is just objs, so call that out here.
  /// we'll have to unsafe cast out to consume.
  type Metadata = obj
  /// we get a bunch of these on each Endpoint, so we'll just note that ehre.
  type MetadataList = obj list

  /// Represents a handler that contains associated endpoint metadata.
  /// When processed, the metadata is added to the generated Endpoint
  /// This is mostly here because it's helpful to have a type to bind things to,
  /// and because we erase this during Endpoint-generation anyway.
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
