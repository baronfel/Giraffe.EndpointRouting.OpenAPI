namespace OpenAPI.Sample.Routes

open Giraffe
open Giraffe.ViewEngine
open Giraffe.EndpointRouting
open Giraffe.EndpointRouting.OpenAPI
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Routing

module Diagnostics =
  let private renderEndpoints : HttpHandler =
    fun next ctx ->
      task {
        let source =
          ctx.GetService<EndpointDataSource>()

        let endpoints = source.Endpoints

        let headerRow =
          [ "Order"
            "Pattern"
            "Methods"
            "Metadata" ]
          |> List.map (fun h -> th [] [ str h ])

        let renderRow (endpoint: Microsoft.AspNetCore.Http.Endpoint) =
          let re = endpoint :?> RouteEndpoint
          let metadata = endpoint.Metadata

          let methods =
            metadata.GetOrderedMetadata<HttpMethodMetadata>()
            |> Seq.collect (fun r -> r.HttpMethods)
            |> Seq.distinct
            |> String.concat ","

          let metadataEntries =
            ul [] [
              for metadata in metadata -> li [] [ str (string metadata) ]
            ]

          tr [] [
            td [] [ str (string re.Order) ]
            td [] [ str re.RoutePattern.RawText ]
            td [] [ str methods ]
            td [] [ metadataEntries ]
          ]

        let render rows =
          let table =
            table [] [
              thead [] headerRow
              tbody [] (rows |> Seq.map renderRow |> Seq.toList)
            ]

          htmlView table

        return! render endpoints next ctx
      }


  let routes : Endpoint list =
    [ GET [ route "/diag" renderEndpoints ] ]

module App =
  let routes : Endpoint list =
    [ GET [ route "/hello" (operationId "sayHello" >=> text "Hello, world") ]
      POST [ route
               "/yellHello"
               (operationId "yellHello"
                >=> (bindJson (fun (payload: {| name: string |}) -> text payload.name))) ]
      yield! Diagnostics.routes ]
