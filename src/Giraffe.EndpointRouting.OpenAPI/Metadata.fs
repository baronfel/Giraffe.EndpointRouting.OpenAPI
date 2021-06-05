namespace Giraffe.EndpointRouting.OpenAPI

module Metadata =

  open System
  open Microsoft.AspNetCore.Routing
  open Microsoft.OpenApi.Models

  [<AllowNullLiteral>]
  type OperationIdMetadata(id: string) =
    member _.Id = id

  [<AllowNullLiteral>]
  type OperationParameter(t: Type, location: string) =
    member _.Type = t
    member _.Location = location

  [<AllowNullLiteral>]
  type OperationSecurity(name: string) =
    member _.Scheme = name

  module Builder =

    /// holds any top-level OpenApi customizations that we can't get from the endpoint
    /// metadata alone
    type OpenApiDocumentOptions() =
      class
      end

    let getRoutePattern (e: Microsoft.AspNetCore.Http.Endpoint) =
      let route = (e :?> RouteEndpoint)
      route.RoutePattern.RawText

    let generateOperationId (e: Microsoft.AspNetCore.Http.Endpoint) =
      match e.Metadata.GetMetadata<OperationIdMetadata>() |> Option.ofObj with
      | Some op -> op.Id
      | None -> getRoutePattern e

    let generateOperation (e: Microsoft.AspNetCore.Http.Endpoint) =
      let op = OpenApiOperation()
      let operationId = generateOperationId e

      e.Metadata.GetOrderedMetadata<OperationParameter>()
      |> Option.ofObj
      |> Option.toList
      |> Seq.collect id
      |> Seq.distinct
      |> Seq.iteri (fun index (parameter: OperationParameter) ->
        let openApiParameter = OpenApiParameter()
        openApiParameter.Name <- string index

        openApiParameter.Required <- parameter.Type.GetGenericTypeDefinition() <> typedefof<_ option>
        // openApiParameter.Schema <- calculateSchema parameter.Type
        op.Parameters.Add(openApiParameter)
      )

      op.OperationId <- operationId
      op

    let methodToOperationType =
      function
      | "GET" -> OperationType.Get
      | "POST" -> OperationType.Post
      | e -> failwithf $"unknown method %s{e}"

    /// creates an OpenAPI document from a collection of application endpoints by parsing endpoint
    /// metadata
    let addEndpointToPath (route, pathItem: OpenApiPathItem) (endpoint: Microsoft.AspNetCore.Http.Endpoint) : OpenApiPathItem =

      // generate the method for only the supported method types
      let operation = generateOperation endpoint

      let httpMethods =
        endpoint.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()
        |> Option.ofObj
        |> Option.map (fun l -> l :> seq<_>)
        |> Option.defaultValue Seq.empty
        |> Seq.collect (fun m -> m.HttpMethods)
        |> Seq.distinct
        |> Seq.toArray
        |> function
          | [||] -> OperationType.GetValues<OperationType>()
          | ms -> ms |> Array.map methodToOperationType

      httpMethods
      |> Array.iter (fun method ->
        match pathItem.Operations.TryGetValue method with
        | true, _existing ->
          System.Diagnostics.Trace.TraceWarning($"Method {method} of path {route} attempted to add a duplicate endpoint. The new endpoint was skipped.")
        | false, _ -> pathItem.AddOperation(method, operation)
      )

      pathItem

    let private processEndpoints (endpoints: Microsoft.AspNetCore.Http.Endpoint seq) : OpenApiPaths =
      let paths = new OpenApiPaths()

      (paths, endpoints)
      ||> Seq.fold (fun paths endpoint ->
        let pathId = getRoutePattern endpoint

        match paths.TryGetValue pathId with
        | true, path -> addEndpointToPath (pathId, path) endpoint |> ignore<OpenApiPathItem>
        | false, _ ->
          let path = new OpenApiPathItem()
          paths.Add(pathId, path)
          addEndpointToPath (pathId, path) endpoint |> ignore<OpenApiPathItem>

        paths
      )

    type OpenApiDocumentBuilder(options: OpenApiDocumentOptions, endpoints: Microsoft.AspNetCore.Routing.EndpointDataSource) =
      member _.CreateDocument() : OpenApiDocument =
        let endpoints = endpoints.Endpoints
        let paths = processEndpoints endpoints

        let document = new OpenApiDocument()

        document.Paths <- paths
        document
