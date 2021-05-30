namespace OpenAPI.Sample

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting
open Giraffe.EndpointRouting.OpenAPI
open Microsoft.AspNetCore.Server.Kestrel.Core
open FSharp.Control.Tasks
open Microsoft.AspNetCore.Routing
open Giraffe.ViewEngine

module Diagnostics =
  let private renderEndpoints : HttpHandler =
    fun next ctx ->
      task {
        let source =
          ctx.GetService<Microsoft.AspNetCore.Routing.EndpointDataSource>()

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

type Startup() =

  // This method gets called by the runtime. Use this method to add services to the container.
  // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
  member _.ConfigureServices(services: IServiceCollection) =
    services.AddGiraffeOpenAPI() |> ignore

    services.Configure<KestrelServerOptions>(fun (opts: KestrelServerOptions) -> opts.AllowSynchronousIO <- true)
    |> ignore

    ()

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
    if env.IsDevelopment() then
      app.UseDeveloperExceptionPage() |> ignore

    app
      .UseRouting()
      .UseEndpoints(fun endpoints ->
        endpoints.MapGiraffeEndpoints(App.routes)
        endpoints.MapGiraffeOpenAPI() |> ignore)
    |> ignore
