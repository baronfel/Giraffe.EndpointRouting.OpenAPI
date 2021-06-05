namespace Giraffe.EndpointRouting.OpenAPI

open Giraffe
open Giraffe.EndpointRouting
open Giraffe.EndpointRouting.OpenAPI.Combinators
open Giraffe.EndpointRouting.OpenAPI.Composition
open Giraffe.EndpointRouting.OpenAPI.Metadata

[<AutoOpen>]
module PublicApi =

  /// <summary>
  /// Composes Giraffe HttpHandlers and/or Giraffe.EndpointRouting.OpenAPIHttpHandlers together
  /// </summary>
  /// <param name="l">a Giraffe HttpHandler and/or Giraffe.EndpointRouting.OpenAPIHttpHandler</param>
  /// <param name="r">a Giraffe HttpHandler and/or Giraffe.EndpointRouting.OpenAPIHttpHandler</param>
  /// <returns>The composed handler</returns>
  let inline (>=>) (l: ^l) (r: ^r) =
    let inline call (_mthd: 'M, input: 'I, _output: 'R, f) =
      ((^M or ^I or ^R): (static member Compose : _ * _ -> _) input, f)

    call (Unchecked.defaultof<Composer>, l, Unchecked.defaultof< ^R>, r)

  /// <summary>
  /// Defines a route at a given path with a given HttpHandler/OpenAPIHttpHandler function
  /// </summary>
  /// <param name="path">The route path to match on</param>
  /// <param name="handler">The HttpHandler/OpenAPIHttpHandler to execute on a route match</param>
  /// <returns>A Giraffe.EndpointRouting.Routers.Endpoint representing the final route</returns>
  let inline route path (handler: ^h) : Endpoint =
    let inline call (_mthd: 'M, path: string, dummy: 'D, handler: 'R) =
      ((^M or ^R or ^D): (static member Route : string * ^R -> ^D) path, handler)

    call (Unchecked.defaultof<Router>, path, Unchecked.defaultof<Endpoint>, handler)


  /// <summary>
  /// Prepends the given handler to the specified endpoint, running it before any already-configured handler pipelines
  /// </summary>
  /// <param name="handler">The HttpHandler/OpenAPIHttpHandler to run before the endpoint's pipeline</param>
  /// <param name="endpoint">The endpoint to which to attach the new handler</param>
  /// <returns>The composed Giraffe.EndpointRouting.Routers.Endpoint</returns>
  let inline applyBefore (handler: ^h) endpoint : Endpoint =
    let inline call (_mthd: 'M, handler: 'H, dummy: 'D, endpoint: 'R) =
      ((^M or ^R or ^D or ^H): (static member ApplyBefore : ^H * ^R -> ^R) handler, endpoint)

    call (Unchecked.defaultof<Router>, handler, Unchecked.defaultof<Endpoint>, endpoint)


  /// <summary>
  /// Appends the given handler to the specified endpoint, running it after any already-configured handler pipelines
  /// </summary>
  /// <param name="handler">The HttpHandler/OpenAPIHttpHandler to run after the endpoint's pipeline</param>
  /// <param name="endpoint">The endpoint to which to attach the new handler</param>
  /// <returns>The composed Giraffe.EndpointRouting.Routers.Endpoint</returns>
  let inline applyAfter (handler: ^h) endpoint =
    let inline call (_mthd: 'M, handler: 'H, dummy: 'D, endpoint: 'R) =
      ((^M or ^R or ^D or ^H): (static member ApplyAfter : ^H * ^R -> ^R) handler, endpoint)

    call (Unchecked.defaultof<Router>, handler, Unchecked.defaultof<Endpoint>, endpoint)

  /// <summary>
  /// Assigns an operation id to this handler
  /// </summary>
  /// <param name="id">the id to assign</param>
  /// <returns>The named HttpHandler</returns>
  let operationId id =
    OpenAPIHttpHandler([ box (OperationIdMetadata(id)) ], (fun next ctx -> next ctx))

  /// <summary>
  /// Attempts to deserialize the json body of the incoming request into the specified type before passing that to the given handler
  /// </summary>
  /// <param name="f">Function to run on the deserialized request</param>
  /// <typeparam name="'t">The type into which to deserialize the request json</typeparam>
  /// <returns>The composed HttpHandler</returns>
  let bindJson<'t> (f: 't -> HttpHandler) =
    OpenAPIHttpHandler([ OperationParameter(typeof<'t>, "body") ], Giraffe.Core.bindJson f)

  /// <summary>
  /// Tags this HttpHandler with the given security policy, forcing that policy to be satisfied before delegating to the rest of the pipeline
  /// </summary>
  /// <param name="name">The name of the policy to which to delegate</param>
  /// <param name="authFailedHandler">A handler to run if the policy is not satisfied</param>
  /// <returns></returns>
  let authorizeByPolicyName name authFailedHandler =
    OpenAPIHttpHandler([ box (OperationSecurity(name)) ], Giraffe.Auth.authorizeByPolicyName name authFailedHandler)
