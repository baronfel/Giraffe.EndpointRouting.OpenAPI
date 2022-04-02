namespace Giraffe.EndpointRouting.OpenAPI

[<AutoOpen>]
module Extensions =

  open Microsoft.AspNetCore.Routing
  open Microsoft.AspNetCore.Builder
  open Microsoft.AspNetCore.Http
  open Microsoft.AspNetCore.Hosting
  open Microsoft.Extensions.DependencyInjection
  open Microsoft.Extensions.DependencyInjection.Extensions
  open System.Runtime.CompilerServices
  open System.Runtime.InteropServices
  open FSharp.Control.Tasks
  open Giraffe.EndpointRouting.OpenAPI.Metadata.Builder
  open Giraffe

  [<Extension>]
  type ServiceCollectionExtensions() =
    [<Extension>]
    /// registers services required to support OpenAPI generation for Giraffe applications
    static member AddGiraffeOpenAPI(services: IServiceCollection) =
      services.TryAddSingleton(OpenApiDocumentOptions())
      services.AddSingleton<OpenApiDocumentBuilder>()

  [<Extension>]
  type EndpointRouteBuilderExtensions() =
    [<Extension>]
    /// Registers middleware unique to Giraffe.EndpointRouting.OpenAPI for reading and
    /// converting Endpoint metadata into an OpenAPI description
    static member MapGiraffeOpenAPI
      (
        endpoints: IEndpointRouteBuilder,
        [<Optional; DefaultParameterValue("/swagger.json")>] route: string
      ) =
      endpoints.MapGet(
        route,
        RequestDelegate(fun ctx ->
          unitTask {
            printfn "Hello world"
            let builder = ctx.GetService<OpenApiDocumentBuilder>()
            ctx.SetContentType("application/json")

            use response = ctx.Response.Body
            use textWriter = new System.IO.StreamWriter(response)

            let writer =
              Microsoft.OpenApi.Writers.OpenApiJsonWriter(textWriter :> System.IO.TextWriter)
            builder.CreateDocument().SerializeAsV3(writer)
          }
        )
      )
