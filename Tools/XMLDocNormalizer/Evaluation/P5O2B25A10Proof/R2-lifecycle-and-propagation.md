# R2 lifecycle trace before the propagation fix

This trace was recorded before changing the production validator. Historical
R2 failure evidence and the R5/K17/P12 closure remain unchanged.

| Stage | Concrete identity carrier / call |
|---|---|
| Actual Worker runtime | `WorkerRuntimeIdentity.ReadAndValidate()` reads loaded assembly images into `WorkerIdentity` / `WorkerAssemblyIdentity[]`; exact native hashes/versions and isolation are checked |
| Worker request execution | `Program.Execute` passes that actual identity to `HistoricalAnalysis.Analyze` |
| Outside-profile failure | `HistoricalAnalysis.Failure` returns existing `WorkerResponse(Identity: identity, Success: false, Failure: ...)`; no Result/Provenance/Finding payload |
| Serialize / deserialize | `WorkerProtocol.JsonOptions`; Main `HistoricalWorkerResponseValidation.Validate` deserializes `WorkerResponse`, rejects duplicate/incomplete/noncanonical envelopes and wrong exit/protocol/operation |
| Identity validation | Failure branch calls unchanged `ValidIdentity(response.Identity, input)` when non-null; full expected Worker/Analyzer shape, protocol, reference profile and exact Roslyn image/version/MVID/hash checks |
| Exact loss | The following `return Fail(...)` invokes the local factory `new(null, null, failure, stderr, processId)`: its second constructor argument discards the validated `WorkerResponse.Identity` |
| Existing destination | `HistoricalWorkerCallResult.Identity` already accepts `WorkerIdentity?`; no new type/field/wire DTO is needed |
| Client / consumer | `HistoricalWorkerClient.AnalyzeAsync` returns that same call via `Observe`; owned-process diagnostics are separately attached. R2 reads `historical.Identity!.LoadedRoslyn` at line227 and currently throws |

The minimal authorized correction is to construct that structured-failure call
with the same deserialized `response.Identity` only AFTER all existing failure
envelope/exit/identity checks have passed. No identity synthesis, request-derived
replacement or reconstruction. Null identities remain null and fail closed;
invalid identities still return the existing rejection without identity.

No `ValidIdentity`, `ValidAssembly`, B21 attestation, request/response binding,
selection, Worker wire production or Current analyzer code is to be changed.
This is a Historical transport-local projection, not a shared Current result
projection. Success remains false: Result/Provenance/receipts remain absent.

Validation results will be recorded in a separate R2 closure record after the
first two unchanged R2 executions and subsequent targeted/full gate attempts.
