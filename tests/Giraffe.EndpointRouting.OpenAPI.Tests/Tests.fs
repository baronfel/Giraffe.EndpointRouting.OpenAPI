namespace Giraffe.EndpointRouting.OpenAPI.Tests

open System
open Expecto
open Giraffe
open Microsoft.AspNetCore.Routing
open Giraffe.EndpointRouting
open Giraffe.EndpointRouting.OpenAPI
open Giraffe.EndpointRouting.OpenAPI.Combinators

module CompositionTests =

    let dummyRoute = route "/a/path/segment" ((text "hello") >=> (operationId "name"))

    [<Tests>]
    let tests =
        testList
            "composition"
            [ test "can compose swagger endpoint and normal httphandler" {
                match dummyRoute with
                | Routers.SimpleEndpoint (_, _, _, [metadata]) ->
                    // example swagger handler that'd add metadata to a chain
                    Expect.equal (metadata :?> Metadata.OperationIdMetadata).Id "name" "should have the name"
                | other -> failtestf "should have been a simple endpoint"
              }
              test "can still use giraffe handlers bare" {
                  match route "" (text "farts") with
                  | Routers.SimpleEndpoint (_, _, _, metadata) -> Expect.equal metadata [] "should have no metadata"
                  | other -> failtestf "should have been a simple endpoint"
              }
              test "can applybefore with normal giraffe" {
                  match route "" (text "farts")
                        |> applyBefore (requiresAuthentication (text "nope")) with
                  | Routers.SimpleEndpoint (_, _, _, metadata) -> Expect.equal metadata [] "should have no metadata"
                  | other -> failtestf "should have been a simple endpoint"
              }
              test "can applybefore with fancy endpoints" {
                  match route "" (text "farts") |> applyBefore (operationId "name") with
                  | Routers.SimpleEndpoint (_, _, _, [metadata]) ->
                      // example swagger handler that'd add metadata to a chain
                      Expect.equal (metadata :?> Metadata.OperationIdMetadata).Id "name" "should have one metadata"
                  | other -> failtestf "should have been a simple endpoint"
              }

              test "can applyafter with normal giraffe" {
                  match route "" (text "farts")
                        |> applyAfter (requiresAuthentication (text "nope")) with
                  | Routers.SimpleEndpoint (_, _, _, metadata) -> Expect.equal metadata [] "should have no metadata"
                  | other -> failtestf "should have been a simple endpoint"
              }
              test "can applyafter with fancy endpoints" {
                  match route "" (text "farts") |> applyAfter (operationId "name") with
                  | Routers.SimpleEndpoint (_, _, _, [metadata]) ->
                      // example swagger handler that'd add metadata to a chain
                      Expect.equal (metadata :?> Metadata.OperationIdMetadata).Id "name" "should have one metadata"
                  | other -> failtestf "should have been a simple endpoint"
              }
              test "endpoint registration" {
                  let builder =
                      let sources = ResizeArray()

                      { new IEndpointRouteBuilder with
                          override this.CreateApplicationBuilder() : Microsoft.AspNetCore.Builder.IApplicationBuilder =
                              failwith "Not Implemented"

                          override this.DataSources : Collections.Generic.ICollection<EndpointDataSource> = sources :> _
                          override this.ServiceProvider : IServiceProvider = failwith "Not Implemented" }

                  builder.MapGiraffeEndpoints([ GET [ dummyRoute ] ])
                  let source = builder.DataSources |> Seq.head
                  let endpoint = source.Endpoints |> Seq.head
                  Expect.hasLength endpoint.Metadata 2 "should have http method metadata and our name"
                  let name = (endpoint.Metadata.[1] :?> Metadata.OperationIdMetadata).Id
                  Expect.equal name "name" "should have our metadata"
              }

              ]
