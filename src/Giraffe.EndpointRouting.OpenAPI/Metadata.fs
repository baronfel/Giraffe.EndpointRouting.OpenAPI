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

    let tryGetOperationId (e: Microsoft.AspNetCore.Http.Endpoint) operationType =
      match e.Metadata.GetMetadata<OperationIdMetadata>() |> Option.ofObj with
      | Some op -> op.Id
      | None ->
        let route = (e :?> RouteEndpoint)
        let pattern = route.RoutePattern.RawText
        $"{pattern}_%A{operationType}"


    let generateOperation (e: Microsoft.AspNetCore.Http.Endpoint) (method: OperationType) =
      let op = OpenApiOperation()
      let operationId = tryGetOperationId e method

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
    let createPathItem (endpoint: Microsoft.AspNetCore.Http.Endpoint) : OpenApiPathItem =
      let pathItem = OpenApiPathItem()

      let methods =
        endpoint.Metadata.GetOrderedMetadata<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()
        |> Option.ofObj
        |> Option.map (fun l -> l :> seq<_>)
        |> Option.defaultValue Seq.empty
        |> Seq.collect (fun m -> m.HttpMethods)
        |> Seq.distinct
        |> Seq.toList
        |> function
          | [] ->
            OperationType.GetValues()
            |> Array.iter (fun method ->
              let operation = generateOperation endpoint method
              pathItem.AddOperation(method, operation)
            )
          | methods ->
            let operationTypes =
              methods |> List.toArray |> Array.map methodToOperationType

            operationTypes
            |> Array.iter (fun method ->
              let operation = generateOperation endpoint method
              pathItem.AddOperation(method, operation)
            )

      pathItem

    let private processEndpoints (endpoints: Microsoft.AspNetCore.Http.Endpoint seq) : OpenApiPaths =
      let paths = new OpenApiPaths()

      for endpoint in endpoints do
        let re = endpoint :?> RouteEndpoint
        let path = createPathItem endpoint
        // TODO: cannot add same route with different method here,
        //       need to lookup existing path and merge path items
        paths.Add(re.RoutePattern.RawText, path)

      paths

    type OpenApiDocumentBuilder(options: OpenApiDocumentOptions, endpoints: Microsoft.AspNetCore.Routing.EndpointDataSource) =
      member _.CreateDocument() : Microsoft.OpenApi.Models.OpenApiDocument =
        let endpoints = endpoints.Endpoints
        let paths = processEndpoints endpoints

        let document =
          new Microsoft.OpenApi.Models.OpenApiDocument()

        document.Paths <- paths
        document
