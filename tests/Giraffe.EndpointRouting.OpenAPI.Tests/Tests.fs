namespace Giraffe.EndpointRouting.OpenAPI.Tests

open System
open Expecto
open Giraffe
open Microsoft.AspNetCore.Routing
open Giraffe.EndpointRouting
open Giraffe.EndpointRouting.OpenAPI

module CompositionTests =
    let withName =
        SwaggerHttpHandler([box "name"], fun next ctx -> next ctx )

    let dummyRoute = route "" (text "farts" >=> withName)

    [<Tests>]
    let tests =
        testList "composition" [
            testCase "can compose swagger endpoint and normal httphandler" <| fun _ ->
                match dummyRoute with
                | Routers.SimpleEndpoint(_, _, _, metadata) ->
                    // example swagger handler that'd add metadata to a chain
                    Expect.equal metadata [box "name"] "should have the name"
                | other -> failtestf "should have been a simple endpoint"
            testCase "endpoint registration" <| fun _ ->
                let builder =
                    let sources = ResizeArray()
                    { new IEndpointRouteBuilder with
                        override this.CreateApplicationBuilder(): Microsoft.AspNetCore.Builder.IApplicationBuilder =
                            failwith "Not Implemented"
                        override this.DataSources: Collections.Generic.ICollection<EndpointDataSource> =
                            sources :> _
                        override this.ServiceProvider: IServiceProvider =
                            failwith "Not Implemented"
                        }

                builder.MapGiraffeEndpoints([ GET [ dummyRoute ] ])
                let source = builder.DataSources |> Seq.head
                let endpoint = source.Endpoints |> Seq.head
                Expect.hasLength endpoint.Metadata 2 "should have http method metadata and our name"
                let name = endpoint.Metadata.[1] :?> string
                Expect.equal name "name" "should have our metadata"

        ]
