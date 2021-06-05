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
      .UseReDoc(fun c ->
        c.SpecUrl <- "/swagger.json"
      )
      .UseEndpoints(fun endpoints ->
        endpoints.MapGiraffeEndpoints(Routes.App.routes)
        endpoints.MapGiraffeOpenAPI() |> ignore)
    |> ignore
