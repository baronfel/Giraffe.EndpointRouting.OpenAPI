namespace Giraffe.EndpointRouting.OpenAPI

open Giraffe
open Giraffe.EndpointRouting
open Giraffe.EndpointRouting.OpenAPI.Combinators
open Giraffe.EndpointRouting.OpenAPI.Composition
open Giraffe.EndpointRouting.OpenAPI.Metadata

[<AutoOpen>]
module PublicApi =

  let inline (>=>) (l: ^l) (r: ^r) =
    let inline call (_mthd: 'M, input: 'I, _output: 'R, f) =
      ((^M or ^I or ^R): (static member Compose : _ * _ -> _) input, f)

    call (Unchecked.defaultof<Composer>, l, Unchecked.defaultof< ^R>, r)

  let inline route path (handler: ^h) : Endpoint =
    let inline call (_mthd: 'M, path: string, dummy: 'D, handler: 'R) =
      ((^M or ^R or ^D): (static member Route : string * ^R -> ^D) path, handler)

    call (Unchecked.defaultof<Router>, path, Unchecked.defaultof<Endpoint>, handler)

  // let inline applyBefore (handler: ^h) endpoint: Endpoint =
  //     let inline call (_mthd: 'M, handler: 'h, dummy: 'D, endpoint: 'R) = ((^M or ^R or ^d) : (static member ApplyBefore: ^h * ^R -> ^R) handler, endpoint)
  //     call (Unchecked.defaultof<Router>, handler, Unchecked.defaultof<Endpoint>, endpoint)

  // let inline applyAfter (handler: ^h) endpoint =
  //     let inline call (_mthd: 'M, handler: 'h, dummy: 'D, endpoint: 'R) = ((^M or ^R or ^d) : (static member ApplyAfter: ^h * ^R -> ^R) handler, endpoint)
  //     call (Unchecked.defaultof<Router>, handler, Unchecked.defaultof<Endpoint>, endpoint)

  /// assigns an operation id to this handler
  let operationId id =
    SwaggerHttpHandler([ box (OperationIdMetadata(id)) ], (fun next ctx -> next ctx))

  /// attempts to deserialize the body of the request
  let bindJson<'t> (f: 't -> HttpHandler) =
    SwaggerHttpHandler([ OperationParameter(typeof<'t>, "body") ], Giraffe.Core.bindJson f)

  /// tags this httphandler with the given security policy
  let authorizeByPolicyName name authFailedHandler =
    SwaggerHttpHandler([ box (OperationSecurity(name)) ], Giraffe.Auth.authorizeByPolicyName name authFailedHandler)
