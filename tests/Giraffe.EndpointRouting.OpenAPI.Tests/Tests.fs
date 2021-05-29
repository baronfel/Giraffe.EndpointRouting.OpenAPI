namespace Giraffe.EndpointRouting.OpenAPI.Tests

open System
open Expecto
open Giraffe
open Giraffe.EndpointRouting
open Giraffe.EndpointRouting.OpenAPI

module CompositionTests =
    [<Tests>]
    let tests =
        testList "composition" [
            testCase "can compose swagger endpoint and normal httphandler" <| fun _ ->
                // example swagger handler that'd add metadata to a chain
                let withName =
                    SwaggerHttpHandler([box "name"], fun next ctx -> next ctx )
                let (SwaggerHttpHandler(metadata, handler)) = routeCi "" >=> text "farts" >=> withName
                Expect.equal metadata [box "name"] "should have the name"
        ]
