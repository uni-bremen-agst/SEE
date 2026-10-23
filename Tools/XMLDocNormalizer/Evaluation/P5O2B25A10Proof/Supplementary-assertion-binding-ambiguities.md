# Supplementary Worker assertion-binding ambiguities

Resolved by the subsequent read-only Phase-0 audit: see
[Supplementary-binding-closure.md](Supplementary-binding-closure.md) and
`Supplementary-resolved-bindings-r2.json`. The original 85/429/3,202 gap list
below is preserved as input history, not the current unresolved status. No
change permission is introduced; R8–R10 remain pending, no R11 is needed.

These are source-inventory binding gaps, NOT proved semantic conflicts and NOT test-change permissions. Original 202 methods / 203 cases / 228 calls are reconstructed. No production clearance or definitive full-preflight closure is claimed. This exact list belongs to the same consolidated batch; do not turn it into serial implementation STOPs.

## `ExceptionFlowAnalysisDispatchTests.MissingCurrentOrPayloadData_RetainsRouterInvalidInput`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`

Case `ExceptionFlowAnalysisDispatchTests.MissingCurrentOrPayloadData_RetainsRouterInvalidInput#0`, parameters: 0

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:352`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:353`: `Assert.True(result.Decision!.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:355`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:357`: `Assert.Null(result.Routing.HistoricalAnalysis)`

- `Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactoryTests.cs:612`: `Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics))`

Case `ExceptionFlowAnalysisDispatchTests.MissingCurrentOrPayloadData_RetainsRouterInvalidInput#1`, parameters: 1

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:352`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:353`: `Assert.True(result.Decision!.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:355`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:357`: `Assert.Null(result.Routing.HistoricalAnalysis)`

- `Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactoryTests.cs:612`: `Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics))`

Case `ExceptionFlowAnalysisDispatchTests.MissingCurrentOrPayloadData_RetainsRouterInvalidInput#2`, parameters: 2

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:352`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:353`: `Assert.True(result.Decision!.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:355`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:357`: `Assert.Null(result.Routing.HistoricalAnalysis)`

- `Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactoryTests.cs:612`: `Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics))`

Case `ExceptionFlowAnalysisDispatchTests.MissingCurrentOrPayloadData_RetainsRouterInvalidInput#3`, parameters: 3

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:352`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:353`: `Assert.True(result.Decision!.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:355`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:357`: `Assert.Null(result.Routing.HistoricalAnalysis)`

- `Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalCompilationProvenanceDescriptorFactoryTests.cs:612`: `Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics))`

## `ExceptionFlowAnalysisDispatchTests.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`

Case `ExceptionFlowAnalysisDispatchTests.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess#0`, parameters: "public static class Fixture { public static void Root() { Missing(); } }", "CompilationFailure"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:194`: `Assert.Equal(expected, result.Routing!.HistoricalFailure!.WorkerFailure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess#1`, parameters: "public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:194`: `Assert.Equal(expected, result.Routing!.HistoricalFailure!.WorkerFailure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

## `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`

Case `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess#0`, parameters: "crash", "WorkerCrash"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess#1`, parameters: "malformed", "MalformedResponse"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess#2`, parameters: "mismatch", "ProtocolMismatch"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess#3`, parameters: "nonzero", "NonZeroExit"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess#4`, parameters: "flood", "OutputLimit"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess#5`, parameters: "stderr-flood", "OutputLimit"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.ExistingAdversarialPeerFailures_NoFallbackAndStoppedProcess#6`, parameters: "utf8", "TransportFailure"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

## `ExceptionFlowAnalysisDispatchTests.TimeoutOrCancellation_PropagatesAndTerminatesOwnedProcess`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`

Case `ExceptionFlowAnalysisDispatchTests.TimeoutOrCancellation_PropagatesAndTerminatesOwnedProcess#0`, parameters: false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

Case `ExceptionFlowAnalysisDispatchTests.TimeoutOrCancellation_PropagatesAndTerminatesOwnedProcess#1`, parameters: true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

## `ExceptionFlowAnalysisDispatchTests.UnsupportedWorkerProfile_FailsBeforeProcessStart`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`

Case `ExceptionFlowAnalysisDispatchTests.UnsupportedWorkerProfile_FailsBeforeProcessStart#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:361`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:363`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:364`: `Assert.Null(result.Routing!.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:366`: `Assert.Same(result.Routing.HistoricalAnalysis!.Failure, result.Routing.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:367`: `Assert.Equal(code, result.Routing.HistoricalFailure!.Code)`

## `ExceptionFlowAnalysisDispatchTests.CurrentException_RemainsOriginalRouterFailure`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`

Case `ExceptionFlowAnalysisDispatchTests.CurrentException_RemainsOriginalRouterFailure#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:352`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:353`: `Assert.True(result.Decision!.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:355`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs:357`: `Assert.Null(result.Routing.HistoricalAnalysis)`

## `ExceptionFlowAnalysisRouterTests.NullRequest_IsNotAnImplicitCurrentSelection`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs`

Case `ExceptionFlowAnalysisRouterTests.NullRequest_IsNotAnImplicitCurrentSelection#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

## `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#0`, parameters: "current-missing"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#1`, parameters: "historical-missing"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#2`, parameters: "current-wrong-input"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#3`, parameters: "historical-wrong-input"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#4`, parameters: "both-current"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#5`, parameters: "both-historical"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#6`, parameters: "null-member"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

Case `ExceptionFlowAnalysisRouterTests.AmbiguousOrInconsistentInputs_AreRejectedBeforeEitherRoute#7`, parameters: "null-session"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:255`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:256`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:258`: `Assert.Null(result.HistoricalAnalysis)`

## `ExceptionFlowAnalysisRouterTests.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs`

Case `ExceptionFlowAnalysisRouterTests.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract#0`, parameters: "public static class Fixture { public static void Root() { Missing(); } }", "CompilationFailure"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:166`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:167`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:170`: `Assert.Same(result.HistoricalAnalysis!.Failure, result.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:171`: `Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, result.HistoricalFailure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:172`: `Assert.Equal(code, result.HistoricalFailure.WorkerFailure!.Code.ToString())`

Case `ExceptionFlowAnalysisRouterTests.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract#1`, parameters: "public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:166`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:167`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:170`: `Assert.Same(result.HistoricalAnalysis!.Failure, result.HistoricalFailure)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:171`: `Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, result.HistoricalFailure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:172`: `Assert.Equal(code, result.HistoricalFailure.WorkerFailure!.Code.ToString())`

## `ExceptionFlowAnalysisRouterTests.UnsupportedHistoricalProfile_RemainsB4UnsupportedInput`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs`

Case `ExceptionFlowAnalysisRouterTests.UnsupportedHistoricalProfile_RemainsB4UnsupportedInput#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:182`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs:184`: `Assert.Null(result.Result)`

## `ExceptionFlowAnalyzerSelectionPolicyTests.SelectedCurrent_ExecutesExistingRouterWithNoWorkerRequirement`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs`

Case `ExceptionFlowAnalyzerSelectionPolicyTests.SelectedCurrent_ExecutesExistingRouterWithNoWorkerRequirement#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:160`: `Assert.Equal(RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)), routed.Result)`

## `ExceptionFlowAnalyzerSelectionPolicyTests.SelectedHistorical_ExecutesGenuineB5RouteWithParityAndIsolation`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs`

Case `ExceptionFlowAnalyzerSelectionPolicyTests.SelectedHistorical_ExecutesGenuineB5RouteWithParityAndIsolation#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:180`: `Assert.Equal(current.Result, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:181`: `Assert.Same(historical.HistoricalAnalysis!.Result, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:183`: `Assert.Equal("NullReferenceException", Assert.Single(historical.Result!.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:190`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker" || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7")`

## `ExceptionFlowAnalyzerSelectionPolicyTests.SelectionFailure_RemainsNonExecutableAtTheUnchangedRouter`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs`

Case `ExceptionFlowAnalyzerSelectionPolicyTests.SelectionFailure_RemainsNonExecutableAtTheUnchangedRouter#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:220`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:222`: `Assert.Null(result.HistoricalAnalysis)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalyzerSelectionPolicyTests.cs:223`: `Assert.Null(result.Result)`

## `ExceptionFlowBoundedGenericContextTests.GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs`

Case `ExceptionFlowBoundedGenericContextTests.GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness#0`, parameters: false, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:177`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:179`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:180`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:183`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowBoundedGenericContextTests.GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness#1`, parameters: false, true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:177`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:179`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:180`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:183`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowBoundedGenericContextTests.GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness#2`, parameters: true, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:177`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:179`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:180`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:183`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowBoundedGenericContextTests.GenuineWorker_SharesFiniteCycleCacheBoundary_NotCorrectness#3`, parameters: true, true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:177`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:179`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:180`: `Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs:183`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

## `ExceptionFlowCanonicalExceptionIdentityTests.StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs`

Case `ExceptionFlowCanonicalExceptionIdentityTests.StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness#0`, parameters: "closed-source", "Helper();", "public static void Helper() { throw new Shared.Outer<int>.Leaf(); }"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:144`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:145`: `Assert.Single(current.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:152`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowCanonicalExceptionIdentityTests.StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness#1`, parameters: "constructed-callee", "GenericThrower<int>.Throw();", ""

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:144`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:145`: `Assert.Single(current.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:152`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowCanonicalExceptionIdentityTests.StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness#2`, parameters: "two-constructed-callees", "GenericThrower<int>.Throw(); GenericThrower<string>.Throw();", ""

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:144`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:145`: `Assert.Single(current.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowCanonicalExceptionIdentityTests.cs:152`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

## `ExceptionFlowConstructedInvocationContextTests.GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs`

Case `ExceptionFlowConstructedInvocationContextTests.GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness#0`, parameters: false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs:202`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs:203`: `Assert.Single(current.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs:208`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowConstructedInvocationContextTests.GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness#1`, parameters: true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs:202`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs:203`: `Assert.Single(current.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowConstructedInvocationContextTests.cs:208`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

## `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#0`, parameters: false, "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#1`, parameters: false, "external"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#2`, parameters: false, "local"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#3`, parameters: false, "exact"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#4`, parameters: false, "base"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#5`, parameters: false, "invalid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#6`, parameters: false, "alias"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#7`, parameters: false, "inheritdoc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#8`, parameters: false, "no-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#9`, parameters: false, "method"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#10`, parameters: false, "combined"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#11`, parameters: false, "empty"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#12`, parameters: false, "direct-only"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#13`, parameters: true, "local"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#14`, parameters: true, "chain"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#15`, parameters: true, "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#16`, parameters: true, "external"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#17`, parameters: true, "exact"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#18`, parameters: true, "base"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#19`, parameters: true, "invalid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#20`, parameters: true, "alias"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#21`, parameters: true, "inheritdoc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#22`, parameters: true, "no-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#23`, parameters: true, "unused"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#24`, parameters: true, "method"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#25`, parameters: true, "combined"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat#26`, parameters: true, "empty"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:51`: `Assert.Equal(Set(expected.GetProperty("findings")), Set(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:52`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:54`: `Assert.Equal(summary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:55`: `Assert.Equal(summary, expected.GetProperty("graphCount").GetInt32() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:58`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(direct!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:59`: `Assert.Equal(Json(direct.DirectResult), Json(direct.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:63`: `Assert.Equal(native.GetProperty(summary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:74`: `Assert.Empty(facts.FlowResult.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:81`: `Assert.Equal(Json(facts.FlowResult), Json(repeatedFacts.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:88`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), actual.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:89`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), actual.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:90`: `Assert.Equal(actual.GetProperty("summaries").GetRawText(), actual.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:91`: `Assert.Equal(Json(facts.FlowResult), actual.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:92`: `Assert.True(actual.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:94`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:95`: `Assert.Empty(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalBoundedFindingTests.MissingTamperedReplayAndWrongModeEvidence_CannotMintFindingReceipt`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs`

Case `ExceptionFlowHistoricalBoundedFindingTests.MissingTamperedReplayAndWrongModeEvidence_CannotMintFindingReceipt#0`, parameters: false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalBoundedFindingTests.MissingTamperedReplayAndWrongModeEvidence_CannotMintFindingReceipt#1`, parameters: true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs:367`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalDeclaredBoundaryTests.ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow#0`, parameters: "no-local-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:75`: `Assert.NotEqual(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:76`: `Assert.Equal(project.Result, solution.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:77`: `Assert.Equal(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(solution.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:80`: `Assert.Equal(direct.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:81`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:82`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:86`: `Assert.Equal(project.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:87`: `Assert.Equal(project.GraphCount, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:88`: `Assert.DoesNotContain(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:92`: `Assert.Empty(project.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:93`: `Assert.Contains(direct.Findings, finding => finding.Smell.ID == "DOC630")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:97`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC610" && finding.Message.Contains("System.ArgumentException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:98`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:99`: `Assert.Equal(3, declared.Result.Entries.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:103`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC632" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:104`: `Assert.Contains(project.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-a/Helper.cs")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:106`: `Assert.All(project.Result.Entries, entry => Assert.Equal(CanonicalExceptionFlowEvidenceKind.Proven, entry.EvidenceKind))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:107`: `Assert.Empty(project.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:113`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:116`: `Assert.Null(raw.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:131`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:136`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:137`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:139`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:140`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:269`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:270`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(Findings()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:315`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow#1`, parameters: "xml-only"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:75`: `Assert.NotEqual(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:76`: `Assert.Equal(project.Result, solution.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:77`: `Assert.Equal(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(solution.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:80`: `Assert.Equal(direct.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:81`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:82`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:86`: `Assert.Equal(project.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:87`: `Assert.Equal(project.GraphCount, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:88`: `Assert.DoesNotContain(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:92`: `Assert.Empty(project.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:93`: `Assert.Contains(direct.Findings, finding => finding.Smell.ID == "DOC630")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:97`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC610" && finding.Message.Contains("System.ArgumentException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:98`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:99`: `Assert.Equal(3, declared.Result.Entries.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:103`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC632" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:104`: `Assert.Contains(project.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-a/Helper.cs")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:106`: `Assert.All(project.Result.Entries, entry => Assert.Equal(CanonicalExceptionFlowEvidenceKind.Proven, entry.EvidenceKind))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:107`: `Assert.Empty(project.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:113`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:116`: `Assert.Null(raw.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:131`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:136`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:137`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:139`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:140`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:269`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:270`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(Findings()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:315`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow#2`, parameters: "unused-local-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:75`: `Assert.NotEqual(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:76`: `Assert.Equal(project.Result, solution.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:77`: `Assert.Equal(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(solution.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:80`: `Assert.Equal(direct.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:81`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:82`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:86`: `Assert.Equal(project.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:87`: `Assert.Equal(project.GraphCount, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:88`: `Assert.DoesNotContain(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:92`: `Assert.Empty(project.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:93`: `Assert.Contains(direct.Findings, finding => finding.Smell.ID == "DOC630")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:97`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC610" && finding.Message.Contains("System.ArgumentException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:98`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:99`: `Assert.Equal(3, declared.Result.Entries.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:103`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC632" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:104`: `Assert.Contains(project.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-a/Helper.cs")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:106`: `Assert.All(project.Result.Entries, entry => Assert.Equal(CanonicalExceptionFlowEvidenceKind.Proven, entry.EvidenceKind))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:107`: `Assert.Empty(project.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:113`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:116`: `Assert.Null(raw.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:131`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:136`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:137`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:139`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:140`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:269`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:270`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(Findings()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:315`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow#3`, parameters: "mixed-direct-transitive"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:75`: `Assert.NotEqual(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:76`: `Assert.Equal(project.Result, solution.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:77`: `Assert.Equal(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(solution.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:80`: `Assert.Equal(direct.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:81`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:82`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:86`: `Assert.Equal(project.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:87`: `Assert.Equal(project.GraphCount, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:88`: `Assert.DoesNotContain(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:92`: `Assert.Empty(project.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:93`: `Assert.Contains(direct.Findings, finding => finding.Smell.ID == "DOC630")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:97`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC610" && finding.Message.Contains("System.ArgumentException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:98`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:99`: `Assert.Equal(3, declared.Result.Entries.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:103`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC632" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:104`: `Assert.Contains(project.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-a/Helper.cs")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:106`: `Assert.All(project.Result.Entries, entry => Assert.Equal(CanonicalExceptionFlowEvidenceKind.Proven, entry.EvidenceKind))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:107`: `Assert.Empty(project.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:113`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:116`: `Assert.Null(raw.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:131`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:136`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:137`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:139`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:140`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:269`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:270`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(Findings()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:315`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow#4`, parameters: "local-chain"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:75`: `Assert.NotEqual(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:76`: `Assert.Equal(project.Result, solution.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:77`: `Assert.Equal(JsonSerializer.Serialize(project.Findings), JsonSerializer.Serialize(solution.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:80`: `Assert.Equal(direct.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:81`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:82`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:86`: `Assert.Equal(project.Result, declared.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:87`: `Assert.Equal(project.GraphCount, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:88`: `Assert.DoesNotContain(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("System.InvalidOperationException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:92`: `Assert.Empty(project.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:93`: `Assert.Contains(direct.Findings, finding => finding.Smell.ID == "DOC630")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:97`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC610" && finding.Message.Contains("System.ArgumentException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:98`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC611" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:99`: `Assert.Equal(3, declared.Result.Entries.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:103`: `Assert.Contains(declared.Findings, finding => finding.Smell.ID == "DOC632" && finding.Message.Contains("LocalException", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:104`: `Assert.Contains(project.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-a/Helper.cs")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:106`: `Assert.All(project.Result.Entries, entry => Assert.Equal(CanonicalExceptionFlowEvidenceKind.Proven, entry.EvidenceKind))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:107`: `Assert.Empty(project.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:113`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:116`: `Assert.Null(raw.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:131`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:136`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:137`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:139`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:140`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:269`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:270`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(Findings()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:315`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#0`, parameters: "missing-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#1`, parameters: "default-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#2`, parameters: "unknown-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#3`, parameters: "declared-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#4`, parameters: "declared-missing-scope"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#5`, parameters: "declared-missing-source"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#6`, parameters: "declared-foreign-owner"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#7`, parameters: "declared-missing-parse"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#8`, parameters: "declared-invalid-parse"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.UnsupportedDeclaredOrIncompleteExplicitContext_FailsBeforeAnyEngine#9`, parameters: "declared-conflicting-scope"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:217`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalDeclaredBoundaryTests.ExistingSupportedWorkerErrors_PropagateWithoutCompilerDowngradeOrModeFallback`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs`

Case `ExceptionFlowHistoricalDeclaredBoundaryTests.ExistingSupportedWorkerErrors_PropagateWithoutCompilerDowngradeOrModeFallback#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:234`: `Assert.Null(unavailable.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs:315`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation#0`, parameters: "local"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:51`: `Assert.Equal(native.Summary, call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:52`: `Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:59`: `Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:60`: `Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:61`: `Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:75`: `Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:78`: `Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:79`: `Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:80`: `Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:88`: `Assert.True(decision.GetProperty("hasSummarySource").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:93`: `Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:94`: `Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:97`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:98`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:100`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation#1`, parameters: "external"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:51`: `Assert.Equal(native.Summary, call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:52`: `Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:59`: `Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:60`: `Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:61`: `Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:75`: `Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:78`: `Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:79`: `Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:80`: `Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:88`: `Assert.True(decision.GetProperty("hasSummarySource").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:93`: `Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:94`: `Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:97`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:98`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:100`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation#2`, parameters: "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:51`: `Assert.Equal(native.Summary, call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:52`: `Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:59`: `Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:60`: `Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:61`: `Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:75`: `Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:78`: `Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:79`: `Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:80`: `Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:88`: `Assert.True(decision.GetProperty("hasSummarySource").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:93`: `Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:94`: `Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:97`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:98`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:100`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation#3`, parameters: "combined"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:51`: `Assert.Equal(native.Summary, call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:52`: `Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:59`: `Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:60`: `Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:61`: `Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:75`: `Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:78`: `Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:79`: `Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:80`: `Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:88`: `Assert.True(decision.GetProperty("hasSummarySource").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:93`: `Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:94`: `Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:97`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:98`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:100`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation#4`, parameters: "unused"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:51`: `Assert.Equal(native.Summary, call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:52`: `Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:59`: `Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:60`: `Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:61`: `Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:75`: `Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:78`: `Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:79`: `Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:80`: `Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:88`: `Assert.True(decision.GetProperty("hasSummarySource").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:93`: `Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:94`: `Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:97`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:98`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:100`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation#5`, parameters: "partial"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:51`: `Assert.Equal(native.Summary, call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:52`: `Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:59`: `Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:60`: `Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:61`: `Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:75`: `Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:78`: `Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:79`: `Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:80`: `Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:88`: `Assert.True(decision.GetProperty("hasSummarySource").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:93`: `Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:94`: `Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:97`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:98`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:100`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation#6`, parameters: "nested"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:51`: `Assert.Equal(native.Summary, call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:52`: `Assert.Equal(native.Direct, call.DeclaredPolicy!.DirectResult)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:59`: `Assert.Equal(prepared.Context.Scope!.OrderedSourcePaths, call.DeclaredPolicy.ReportingSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:60`: `Assert.Equal(2, Assert.Single(actualDefinitions).DeclaringSourcePaths!.Length)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:61`: `Assert.NotEqual(JsonSerializer.Serialize(native.ProjectFindings), JsonSerializer.Serialize(native.DeclaredFindings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:75`: `Assert.Equal(decision.Decision.Relevant, native.DeclaredFindings.Any(finding => finding.GetProperty("smell").GetProperty("id").GetString() == id && finding.GetProperty("message").GetString()!.Contains(expected.DisplayName, StringComparison.Ordinal)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:78`: `Assert.Equal(native.HasDeclaredTypes, decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:79`: `Assert.Equal(native.Direct.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasDirectSource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:80`: `Assert.Equal(native.Summary.Entries.Any(entry => entry.ExceptionType.Equals(expected.Type)), decision.Decision.HasSummarySource)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:88`: `Assert.True(decision.GetProperty("hasSummarySource").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:93`: `Assert.DoesNotContain(native.Summary.Entries, entry => entry.ExceptionType.Equals(actualDefinitions[0].Type))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:94`: `Assert.Contains(native.DeclaredFindings, item => item.GetProperty("smell").GetProperty("id").GetString() == "DOC632")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:97`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:98`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:100`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#0`, parameters: "missing-policy"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#1`, parameters: "missing-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#2`, parameters: "assembly"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#3`, parameters: "module"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#4`, parameters: "missing-source"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#5`, parameters: "foreign-source"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#6`, parameters: "missing-manifest"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#7`, parameters: "missing-count"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#8`, parameters: "unused-omission-recounted"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#9`, parameters: "duplicate-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#10`, parameters: "missing-direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#11`, parameters: "foreign-direct-path"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#12`, parameters: "unknown-evidence-kind"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#13`, parameters: "missing-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#14`, parameters: "default-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.MissingConflictingOrForgedPolicyResponse_FailsClosed#15`, parameters: "conflicting-profile"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:181`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:182`: `Assert.Equal(DeclaredFindingPolicyFailureCode.UnvalidatedEvidence, DeclaredExceptionFindingPolicy.Evaluate(rejected, new(WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions, native.Expected[0].Type, DeclaredFindingSourceKind.Summary)).Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredPolicyTests.QueryAdmissionAndLegacyEvidence_AreExplicitFailClosed_NoDefaultOrRelevantGuess`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.QueryAdmissionAndLegacyEvidence_AreExplicitFailClosed_NoDefaultOrRelevantGuess#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredPolicyTests.IncompleteRequestOriginalReceiptsAndUnsupportedShapes_DoNotActivateOrDowngrade`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs`

Case `ExceptionFlowHistoricalDeclaredPolicyTests.IncompleteRequestOriginalReceiptsAndUnsupportedShapes_DoNotActivateOrDowngrade#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:233`: `Assert.Null(mismatch.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#0`, parameters: "local"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#1`, parameters: "external"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#2`, parameters: "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#3`, parameters: "combined"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#4`, parameters: "unused"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#5`, parameters: "partial"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#6`, parameters: "nested"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#7`, parameters: "exact"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#8`, parameters: "base"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#9`, parameters: "inheritdoc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#10`, parameters: "invalid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#11`, parameters: "no-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#12`, parameters: "alias"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#13`, parameters: "method"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#14`, parameters: "duplicate"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#15`, parameters: "empty-tag"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#16`, parameters: "chain"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat#17`, parameters: "guard-direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:45`: `Assert.True(first.Succeeded, first.Failure + ": " + first.Direct.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message + ": " + first.Summary?.Dispatch?.Routing?.HistoricalAnalysis?.Failure?.Message)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:49`: `Assert.Equal(FindingSet(declared.GetProperty("findings")), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:50`: `Assert.Equal(Json(first.Findings), Json(repeat.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:52`: `Assert.Equal(expectedSummary, first.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:53`: `Assert.Equal(expectedSummary, declared.GetProperty("graphCount").GetInt32() != 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:56`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(directFacts!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:57`: `Assert.Equal(Json(directFacts.DirectResult), Json(direct.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:60`: `Assert.True(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out var facts))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:62`: `Assert.Equal(native.GetProperty(expectedSummary ? "summary" : "direct").GetRawText(), Json(facts!.FlowResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:68`: `Assert.Equal(Json(facts.FlowResult), Json((repeat.Summary ?? repeat.Direct).Dispatch!.Routing!.HistoricalAnalysis!.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:77`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(selected with { }, expectedSummary ? WorkerDeclaredExecution.SummaryEvidence : WorkerDeclaredExecution.DirectEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:88`: `Assert.Single(first.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:89`: `Assert.Equal(0, declared.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(projectCall.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:103`: `Assert.Equal(FindingSet(project.GetProperty("findings")), FindingSet(projectFindings.Value))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:107`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.Value.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:108`: `Assert.Equal(native.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:109`: `Assert.Equal(graph.Value.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()), graph.Value.GetProperty("repeatedSummaries").EnumerateArray().Select(item => item.GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:110`: `Assert.Equal(Json(first.Summary!.Result), graph.Value.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:111`: `Assert.True(graph.Value.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:116`: `Assert.NotEqual(FindingSet(project.GetProperty("findings")), FindingSet(declared.GetProperty("findings")))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:117`: `Assert.NotEqual(FindingSet(projectFindings!.Value), FindingSet(first.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:119`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC611")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:121`: `Assert.Contains(first.Findings, finding => finding.Smell.ID == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#0`, parameters: "missing-document"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#1`, parameters: "missing-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#2`, parameters: "missing-declarations"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#3`, parameters: "missing-ownership"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#4`, parameters: "missing-direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#5`, parameters: "missing-summary"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#6`, parameters: "conflicting-flow"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#7`, parameters: "invalid-context"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#8`, parameters: "wrong-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#9`, parameters: "missing-attestation"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#10`, parameters: "malformed-attestation"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#11`, parameters: "consumed-replay"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#12`, parameters: "request-mismatch"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#13`, parameters: "response-mismatch"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#14`, parameters: "binding-tamper"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#15`, parameters: "target-substitution"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.InvalidContractOrReplay_CannotReachGuardOrFindingPolicy#16`, parameters: "source-substitution"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:199`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch#0`, parameters: "direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:233`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:234`: `Assert.Empty(rejected.Findings)`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch#1`, parameters: "project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:233`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:234`: `Assert.Empty(rejected.Findings)`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch#2`, parameters: "solution"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:233`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:234`: `Assert.Empty(rejected.Findings)`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch#3`, parameters: "default"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:233`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:234`: `Assert.Empty(rejected.Findings)`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch#4`, parameters: "missing-document"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:233`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:234`: `Assert.Empty(rejected.Findings)`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch#5`, parameters: "missing-sources"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:233`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:234`: `Assert.Empty(rejected.Findings)`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.UnsupportedFindingScopes_AreRejectedBeforeDispatch#6`, parameters: "cross-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:233`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:234`: `Assert.Empty(rejected.Findings)`

## `ExceptionFlowHistoricalDeclaredProductionFindingTests.SelectedSummaryWorkerFailure_DoesNotPublishPartialDirectFindings`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.SelectedSummaryWorkerFailure_DoesNotPublishPartialDirectFindings#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:254`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:255`: `Assert.True(result.Direct.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:257`: `Assert.Null(result.Summary.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalDeclaredProductionFindingTests.B21ReceiptWithoutProductivePurpose_AndWrongPurpose_CannotReachGuard`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.B21ReceiptWithoutProductivePurpose_AndWrongPurpose_CannotReachGuard#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:271`: `Assert.False(XmlDocExceptionSemanticDetector.TryAdmitFindingEvidence(call, WorkerDeclaredExecution.SummaryEvidence, out _))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalDeclaredProductionFindingTests.PermanentBoundary_MainOwnsGuardAndPolicy_WorkerOnlyExecutesExplicitPurpose`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.PermanentBoundary_MainOwnsGuardAndPolicy_WorkerOnlyExecutesExplicitPurpose#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:285`: `Assert.Contains("RequiresSummary(options.ExceptionAnalysisMode", detector)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:286`: `Assert.Contains("AddExceptionFindings<INamedTypeSymbol>", detector)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:287`: `Assert.Contains("AddExceptionFindings(findings", external)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:288`: `Assert.True(external.IndexOf("if (!TryAdmitFindingEvidence", StringComparison.Ordinal) < external.IndexOf("RequiresSummary(input!", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:291`: `Assert.Contains("ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions || hasDeclaredTypes", findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:292`: `Assert.DoesNotContain("RequiresSummary", worker)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalDeclaredProductionFindingTests.ExactProductiveResponseFromWrongPhysicalWorker_IsRejectedBeforeGuard`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`

Case `ExceptionFlowHistoricalDeclaredProductionFindingTests.ExactProductiveResponseFromWrongPhysicalWorker_IsRejectedBeforeGuard#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:323`: `Assert.False(rejected.SummaryRequested)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs:398`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalDeclaredReadinessTests.IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs`

Case `ExceptionFlowHistoricalDeclaredReadinessTests.IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings#0`, parameters: "cover-exact"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:45`: `Assert.Equal(Json(call.Result), Json(changed.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:47`: `Assert.Equal(Json(changed.Result), Json(repeated.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:49`: `Assert.Equal(before.GetProperty("summary").GetRawText(), after.GetProperty("summary").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:52`: `Assert.Equal(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:54`: `Assert.NotEqual(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:55`: `Assert.Single(call.DeclaredPolicy!.DirectResult!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:73`: `Assert.Empty(after.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:192`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:206`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredReadinessTests.IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings#1`, parameters: "cover-base"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:45`: `Assert.Equal(Json(call.Result), Json(changed.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:47`: `Assert.Equal(Json(changed.Result), Json(repeated.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:49`: `Assert.Equal(before.GetProperty("summary").GetRawText(), after.GetProperty("summary").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:52`: `Assert.Equal(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:54`: `Assert.NotEqual(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:55`: `Assert.Single(call.DeclaredPolicy!.DirectResult!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:73`: `Assert.Empty(after.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:192`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:206`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredReadinessTests.IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings#2`, parameters: "inheritdoc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:45`: `Assert.Equal(Json(call.Result), Json(changed.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:47`: `Assert.Equal(Json(changed.Result), Json(repeated.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:49`: `Assert.Equal(before.GetProperty("summary").GetRawText(), after.GetProperty("summary").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:52`: `Assert.Equal(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:54`: `Assert.NotEqual(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:55`: `Assert.Single(call.DeclaredPolicy!.DirectResult!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:73`: `Assert.Empty(after.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:192`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:206`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDeclaredReadinessTests.IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings#3`, parameters: "non-exception"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:45`: `Assert.Equal(Json(call.Result), Json(changed.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:47`: `Assert.Equal(Json(changed.Result), Json(repeated.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:49`: `Assert.Equal(before.GetProperty("summary").GetRawText(), after.GetProperty("summary").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:52`: `Assert.Equal(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:54`: `Assert.NotEqual(before.GetProperty("declaredFindings").GetRawText(), after.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:55`: `Assert.Single(call.DeclaredPolicy!.DirectResult!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:73`: `Assert.Empty(after.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:192`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:206`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredReadinessTests.NoLocalDeclarationGuard_CannotBeReplacedByUnguardedProjectExecution`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs`

Case `ExceptionFlowHistoricalDeclaredReadinessTests.NoLocalDeclarationGuard_CannotBeReplacedByUnguardedProjectExecution#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:100`: `Assert.Empty(native.GetProperty("direct").GetProperty("entries").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:101`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:104`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:106`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:206`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:192`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDeclaredReadinessTests.SameProjectChain_HasFullPolicyFactsButNoProductiveDeclaredFindingRoute`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs`

Case `ExceptionFlowHistoricalDeclaredReadinessTests.SameProjectChain_HasFullPolicyFactsButNoProductiveDeclaredFindingRoute#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:133`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:134`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:136`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:192`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs:206`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#0`, parameters: "exact"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#1`, parameters: "alias"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#2`, parameters: "base"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#3`, parameters: "invalid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#4`, parameters: "method"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#5`, parameters: "duplicate"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#6`, parameters: "empty-tag"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.RealWorkerBinding_ProducesDeterministicCanonicalFacts_WithFreshSingleUseAuthority#7`, parameters: "no-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:32`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:41`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:42`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(first.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#0`, parameters: "missing-attestation"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#1`, parameters: "unregistered-attestation"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#2`, parameters: "receipt-replay"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#3`, parameters: "type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#4`, parameters: "coherent-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#5`, parameters: "member"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#6`, parameters: "source"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#7`, parameters: "ownership"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#8`, parameters: "default-kind"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#9`, parameters: "missing-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#10`, parameters: "missing-bindings"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#11`, parameters: "unknown-protocol"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#12`, parameters: "unknown-wire-attestation"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#13`, parameters: "cross-binding"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#14`, parameters: "cross-target"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#15`, parameters: "cross-source"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowHistoricalDocumentationAttestationTests.MissingMalformedChangedOrReplayedAuthority_NeverMintsDocumentationReceipt#16`, parameters: "cloned-input"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:197`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDocumentationAttestationTests.cs:228`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.HistoricalWorker" or "XMLDocNormalizer.ExceptionFlow.Historical")`

## `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#0`, parameters: "local"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#1`, parameters: "external"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#2`, parameters: "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#3`, parameters: "combined"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#4`, parameters: "unused"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#5`, parameters: "partial"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#6`, parameters: "nested"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#7`, parameters: "exact"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#8`, parameters: "base"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#9`, parameters: "inheritdoc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#10`, parameters: "invalid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#11`, parameters: "no-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#12`, parameters: "alias"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#13`, parameters: "method"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#14`, parameters: "duplicate"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalFindingContractTests.CompleteSelectedMemberContract_ActualNativeFindingsFactsGuardAndRepeat#15`, parameters: "empty-tag"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:48`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:49`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:51`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:62`: `Assert.Equal(FindingContextBuilder.ForDeclaration(member, "ExceptionFlow", filePath: payload.LogicalPath), facts.Member.Context)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:87`: `Assert.Equal(native.GetProperty("hasDeclaredTypes").GetBoolean(), decision.Decision.SummaryEnabled)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:108`: `Assert.DoesNotContain(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:109`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC610")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:110`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:111`: `Assert.Contains(native.GetProperty("declaredFindings").EnumerateArray(), item => Id(item) == "DOC660")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:112`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#0`, parameters: "missing-document"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#1`, parameters: "missing-bindings"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#2`, parameters: "omission"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#3`, parameters: "owner"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#4`, parameters: "member-span"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#5`, parameters: "method"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#6`, parameters: "cref"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#7`, parameters: "inheritdoc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#8`, parameters: "has-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#9`, parameters: "default-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#10`, parameters: "missing-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#11`, parameters: "profile"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#12`, parameters: "type-identity"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#13`, parameters: "exception-classification"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#14`, parameters: "display"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#15`, parameters: "missing-displays"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#16`, parameters: "duplicate-displays"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#17`, parameters: "unknown-kind"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#18`, parameters: "missing-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#19`, parameters: "direct-conflict"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#20`, parameters: "missing-summary"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalFindingContractTests.MissingConflictingOrForgedDocumentContract_FailsClosed#21`, parameters: "missing-direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:210`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalFindingContractTests.OriginalContextReceipt_RequestAdmissionLegacyAndUncertainGuard_AreNotBypassed`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs`

Case `ExceptionFlowHistoricalFindingContractTests.OriginalContextReceipt_RequestAdmissionLegacyAndUncertainGuard_AreNotBypassed#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:227`: `Assert.Null(clone.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:231`: `Assert.Empty(native.GetProperty("declaredFindings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs:302`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalInputEquivalenceTests.InvalidPath_IsTypedBeforeProcessStartAndNeverNormalized`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs`

Case `ExceptionFlowHistoricalInputEquivalenceTests.InvalidPath_IsTypedBeforeProcessStartAndNeverNormalized#0`, parameters: ""

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:118`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:120`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalInputEquivalenceTests.InvalidPath_IsTypedBeforeProcessStartAndNeverNormalized#1`, parameters: "   "

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:118`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:120`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalInputEquivalenceTests.InvalidPath_IsTypedBeforeProcessStartAndNeverNormalized#2`, parameters: "source\nforged.cs"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:118`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:120`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalInputEquivalenceTests.InvalidPath_IsTypedBeforeProcessStartAndNeverNormalized#3`, parameters: "source\0forged.cs"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:118`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:120`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalInputEquivalenceTests.InvalidPath_IsTypedBeforeProcessStartAndNeverNormalized#4`, parameters: "oversize"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:118`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:120`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalInputEquivalenceTests.ConflictingReturnedPath_IsRejectedInProvenanceAndEveryFlowStep`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs`

Case `ExceptionFlowHistoricalInputEquivalenceTests.ConflictingReturnedPath_IsRejectedInProvenanceAndEveryFlowStep#0`, parameters: false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:138`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalInputEquivalenceTests.ConflictingReturnedPath_IsRejectedInProvenanceAndEveryFlowStep#1`, parameters: true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:138`: `Assert.Null(rejected.Result)`

## `ExceptionFlowHistoricalInputEquivalenceTests.MissingSourceAndUnsupportedOptions_RetainTypedFailureWithoutFallback`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs`

Case `ExceptionFlowHistoricalInputEquivalenceTests.MissingSourceAndUnsupportedOptions_RetainTypedFailureWithoutFallback#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:155`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalInputEquivalenceTests.Inventory_DifferentialWitnessesSeparateParseReferencesSourcesAndNullable`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs`

Case `ExceptionFlowHistoricalInputEquivalenceTests.Inventory_DifferentialWitnessesSeparateParseReferencesSourcesAndNullable#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:165`: `Assert.Single(defined.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:166`: `Assert.Empty(noDefines.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:169`: `Assert.Equal(noDefines.Result, oldWorker.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:170`: `Assert.NotEqual(defined.Result, oldWorker.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:183`: `Assert.Equal(disabled.Result, enabled.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:191`: `Assert.Equal("ExternalException", withReference.Result.Entries.Single().ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:194`: `Assert.Null(withoutReference.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:199`: `Assert.Single(multiple.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:202`: `Assert.Null(missingSource.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalInputEquivalenceTests.cs:230`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

## `ExceptionFlowHistoricalPayloadProjectionTests.MissingRequiredInput_IsTypedAndCannotReachDispatch`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs`

Case `ExceptionFlowHistoricalPayloadProjectionTests.MissingRequiredInput_IsTypedAndCannotReachDispatch#0`, parameters: 0

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:70`: `Assert.Null(result.Dispatch)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:71`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:308`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:309`: `Assert.Equal(code, result.Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:310`: `Assert.Null(result.Value)`

Case `ExceptionFlowHistoricalPayloadProjectionTests.MissingRequiredInput_IsTypedAndCannotReachDispatch#1`, parameters: 1

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:70`: `Assert.Null(result.Dispatch)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:71`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:308`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:309`: `Assert.Equal(code, result.Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:310`: `Assert.Null(result.Value)`

Case `ExceptionFlowHistoricalPayloadProjectionTests.MissingRequiredInput_IsTypedAndCannotReachDispatch#2`, parameters: 2

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:70`: `Assert.Null(result.Dispatch)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:71`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:308`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:309`: `Assert.Equal(code, result.Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:310`: `Assert.Null(result.Value)`

Case `ExceptionFlowHistoricalPayloadProjectionTests.MissingRequiredInput_IsTypedAndCannotReachDispatch#3`, parameters: 3

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:70`: `Assert.Null(result.Dispatch)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:71`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:308`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:309`: `Assert.Equal(code, result.Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:310`: `Assert.Null(result.Value)`

## `ExceptionFlowHistoricalPayloadProjectionTests.RealHistoricalProductionFlow_AutomaticPayloadParityIsolationAndCleanup`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs`

Case `ExceptionFlowHistoricalPayloadProjectionTests.RealHistoricalProductionFlow_AutomaticPayloadParityIsolationAndCleanup#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:204`: `Assert.Equal("NullReferenceException", Assert.Single(result.Result!.Entries).ExceptionType.MetadataName)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:205`: `Assert.Empty(result.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:318`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker" || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7")`

## `ExceptionFlowHistoricalPayloadProjectionTests.WorkerUnavailableOrCancelled_RetainsFailureWithoutCurrentFallback`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs`

Case `ExceptionFlowHistoricalPayloadProjectionTests.WorkerUnavailableOrCancelled_RetainsFailureWithoutCurrentFallback#0`, parameters: false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:252`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:253`: `Assert.Equal(cancelled ? HistoricalWorkerClientFailureCode.Cancelled : HistoricalWorkerClientFailureCode.StartFailure, result.Dispatch!.Routing!.HistoricalFailure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:256`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalPayloadProjectionTests.WorkerUnavailableOrCancelled_RetainsFailureWithoutCurrentFallback#1`, parameters: true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:252`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:253`: `Assert.Equal(cancelled ? HistoricalWorkerClientFailureCode.Cancelled : HistoricalWorkerClientFailureCode.StartFailure, result.Dispatch!.Routing!.HistoricalFailure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalPayloadProjectionTests.cs:256`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalProductionAdmissionTests.CoherentBindingSubstitution_IsRejectedWithoutExecutionAttestation_AndCannotEnterProduction`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs`

Case `ExceptionFlowHistoricalProductionAdmissionTests.CoherentBindingSubstitution_IsRejectedWithoutExecutionAttestation_AndCannotEnterProduction#0`, parameters: "unresolved"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:38`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(genuine.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:39`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(genuine.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:56`: `Assert.Single(genuine.DeclaredPolicy!.DirectResult!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:72`: `Assert.Null(admitted.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.CoherentBindingSubstitution_IsRejectedWithoutExecutionAttestation_AndCannotEnterProduction#1`, parameters: "other-symbol"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:38`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(genuine.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:39`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(genuine.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:56`: `Assert.Single(genuine.DeclaredPolicy!.DirectResult!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:72`: `Assert.Null(admitted.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.CoherentBindingSubstitution_IsRejectedWithoutExecutionAttestation_AndCannotEnterProduction#2`, parameters: "redirect-named"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:38`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(genuine.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:39`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(genuine.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:56`: `Assert.Single(genuine.DeclaredPolicy!.DirectResult!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:72`: `Assert.Null(admitted.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs`

Case `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation#0`, parameters: "local"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:134`: `Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:135`: `Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:136`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:137`: `Assert.True(declared.GraphCount > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:138`: `Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:142`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:152`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:154`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:159`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:160`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:162`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation#1`, parameters: "external"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:134`: `Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:135`: `Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:136`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:137`: `Assert.True(declared.GraphCount > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:138`: `Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:142`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:152`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:154`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:159`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:160`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:162`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation#2`, parameters: "guard"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:134`: `Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:135`: `Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:136`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:137`: `Assert.True(declared.GraphCount > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:138`: `Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:142`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:152`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:154`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:159`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:160`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:162`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation#3`, parameters: "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:134`: `Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:135`: `Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:136`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:137`: `Assert.True(declared.GraphCount > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:138`: `Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:142`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:152`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:154`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:159`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:160`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:162`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation#4`, parameters: "chain"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:134`: `Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:135`: `Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:136`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:137`: `Assert.True(declared.GraphCount > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:138`: `Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:142`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:152`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:154`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:159`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:160`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:162`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation#5`, parameters: "unused"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:134`: `Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:135`: `Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:136`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:137`: `Assert.True(declared.GraphCount > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:138`: `Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:142`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:152`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:154`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:159`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:160`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:162`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation#6`, parameters: "no-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:134`: `Assert.Equal(native.GetProperty("declaredFindings").GetRawText(), Json(declared.Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:135`: `Assert.Equal(native.GetProperty("projectFindings").GetRawText(), Json(production.Single(item => item.Mode == ExceptionAnalysisMode.ProjectTransitive).Findings))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:136`: `Assert.Equal(0, declared.GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:137`: `Assert.True(declared.GraphCount > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:138`: `Assert.Equal(0, production.Single(item => item.Mode == ExceptionAnalysisMode.Direct).GraphCount)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:142`: `Assert.NotEqual(native.GetProperty("projectFindings").GetRawText(), native.GetProperty("declaredFindings").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:152`: `Assert.Null(call.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:154`: `Assert.Empty(declared.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:159`: `Assert.Equal(native.GetProperty("summary").GetRawText(), Json(call.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:160`: `Assert.Equal(native.GetProperty("direct").GetRawText(), Json(call.DeclaredPolicy!.DirectResult))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:162`: `Assert.Equal(Json(call.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:217`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs:226`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs:273`: `Assert.Equal(canonicalSummary, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalReferenceIdentityTests.RealExternalMetadata_FullCanonicalIdentitySourcesUncertaintyGraphRepeatAndIsolation`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.RealExternalMetadata_FullCanonicalIdentitySourcesUncertaintyGraphRepeatAndIsolation#0`, parameters: 1

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:38`: `Assert.Contains(native.Result.Entries, entry => entry.ExceptionType.MetadataName == "ExternalException" && entry.ExceptionType.Assembly!.VersionMajor == version)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:40`: `Assert.Empty(native.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:43`: `Assert.Null(before.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:52`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:53`: `Assert.Equal(first.Result, second.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:294`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:295`: `Assert.Equal(summaries, Summaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:297`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:389`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:390`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:391`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:392`: `Assert.Equal(native.GraphSummaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()!).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:394`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(historical, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:395`: `Assert.True(native.RepeatedFindingsEqual)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:406`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker" || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:306`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalReferenceIdentityTests.RealExternalMetadata_FullCanonicalIdentitySourcesUncertaintyGraphRepeatAndIsolation#1`, parameters: 2

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:38`: `Assert.Contains(native.Result.Entries, entry => entry.ExceptionType.MetadataName == "ExternalException" && entry.ExceptionType.Assembly!.VersionMajor == version)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:40`: `Assert.Empty(native.Result.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:43`: `Assert.Null(before.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:52`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:53`: `Assert.Equal(first.Result, second.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:294`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:295`: `Assert.Equal(summaries, Summaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:297`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:389`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:390`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:391`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:392`: `Assert.Equal(native.GraphSummaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()!).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:394`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(historical, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:395`: `Assert.True(native.RepeatedFindingsEqual)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:406`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker" || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:306`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalReferenceIdentityTests.OriginalTargetNameVersionAndModule_FullIdentityParityWithoutCompilerOptionExpansion`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.OriginalTargetNameVersionAndModule_FullIdentityParityWithoutCompilerOptionExpansion#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:94`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:95`: `Assert.Equal(first.Result, second.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:294`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:295`: `Assert.Equal(summaries, Summaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:297`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:389`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:390`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:391`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:392`: `Assert.Equal(native.GraphSummaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()!).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:394`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(historical, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:395`: `Assert.True(native.RepeatedFindingsEqual)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:406`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker" || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:306`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalReferenceIdentityTests.SameLookupNameDifferentVersions_BindsPeIdentityNotPath`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.SameLookupNameDifferentVersions_BindsPeIdentityNotPath#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:115`: `Assert.NotEqual(left.Result, right.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:116`: `Assert.Equal(Native(first).Result, left.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:120`: `Assert.Equal(left.Result, again.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:294`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:295`: `Assert.Equal(summaries, Summaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:297`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:306`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalReferenceIdentityTests.AliasesAndOrder_ArePreservedNotSortedOrDiscarded`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.AliasesAndOrder_ArePreservedNotSortedOrDiscarded#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:133`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:136`: `Assert.Equal(Native(reordered).Result, second.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:294`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:295`: `Assert.Equal(summaries, Summaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:297`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:306`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalReferenceIdentityTests.MissingExternalReference_HasSameCompilerErrorAndNoSuccessfulOrFallbackResult`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.MissingExternalReference_HasSameCompilerErrorAndNoSuccessfulOrFallbackResult#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:158`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:160`: `Assert.Contains(result.Failure.WorkerFailure.Details, error => error.StartsWith("CS0246", StringComparison.Ordinal))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:294`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:295`: `Assert.Equal(summaries, Summaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:297`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, new XmlDocOptions(), session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:306`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata#0`, parameters: "identity-version"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:188`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata#1`, parameters: "identity-key"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:188`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata#2`, parameters: "module-mvid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:188`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata#3`, parameters: "module-name"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:188`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata#4`, parameters: "target-culture"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:188`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata#5`, parameters: "target-version"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:188`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ContradictoryDescription_IsRejectedAgainstActualHistoricalMetadata#6`, parameters: "target-module"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:188`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#0`, parameters: "hash"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#1`, parameters: "duplicate"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#2`, parameters: "conflicting-version"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#3`, parameters: "module"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#4`, parameters: "embed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#5`, parameters: "signed-target"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#6`, parameters: "missing-images"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#7`, parameters: "unknown-profile"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#8`, parameters: "alias-duplicate"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#9`, parameters: "count-budget"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#10`, parameters: "byte-budget"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.UnsupportedReferenceStates_FailBeforeProcessStart#11`, parameters: "target-name"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:223`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalReferenceIdentityTests.ReturnedReferenceWorldHash_IsBoundAndUnknownDuplicateJsonFailsClosed`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ReturnedReferenceWorldHash_IsBoundAndUnknownDuplicateJsonFailsClosed#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:251`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:406`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker" || assembly.ManifestModule.ModuleVersionId.ToString() is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7")`

## `ExceptionFlowHistoricalReferenceIdentityTests.ReturnedExceptionAssemblyOrMvid_CannotContradictRequestedReferenceWorld`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ReturnedExceptionAssemblyOrMvid_CannotContradictRequestedReferenceWorld#0`, parameters: false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:276`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalReferenceIdentityTests.ReturnedExceptionAssemblyOrMvid_CannotContradictRequestedReferenceWorld#1`, parameters: true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalReferenceIdentityTests.cs:276`: `Assert.Null(rejected.Result)`

## `ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs`

Case `ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation#0`, parameters: "direct", "12", 1, true, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:43`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:46`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:47`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:48`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == HelperPath)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:54`: `Assert.Equal(payload.Context!.Scope!.OrderedSourcePaths, projection.Value!.Payload.Context!.Scope!.OrderedSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:60`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:66`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:67`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:68`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:70`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:71`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:72`: `Assert.Equal(payload.Context.Scope.OrderedSourcePaths, graph.GetProperty("ownership").EnumerateArray().Select(item => item.GetProperty("logicalPath").GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:282`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:285`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:366`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation#1`, parameters: "direct", "12", 2, true, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:43`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:46`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:47`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:48`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == HelperPath)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:54`: `Assert.Equal(payload.Context!.Scope!.OrderedSourcePaths, projection.Value!.Payload.Context!.Scope!.OrderedSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:60`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:66`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:67`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:68`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:70`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:71`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:72`: `Assert.Equal(payload.Context.Scope.OrderedSourcePaths, graph.GetProperty("ownership").EnumerateArray().Select(item => item.GetProperty("logicalPath").GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:282`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:285`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:366`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation#2`, parameters: "project-transitive", "10", 2, true, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:43`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:46`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:47`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:48`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == HelperPath)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:54`: `Assert.Equal(payload.Context!.Scope!.OrderedSourcePaths, projection.Value!.Payload.Context!.Scope!.OrderedSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:60`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:66`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:67`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:68`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:70`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:71`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:72`: `Assert.Equal(payload.Context.Scope.OrderedSourcePaths, graph.GetProperty("ownership").EnumerateArray().Select(item => item.GetProperty("logicalPath").GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:282`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:285`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:366`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation#3`, parameters: "project-transitive", "12", 2, false, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:43`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:46`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:47`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:48`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == HelperPath)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:54`: `Assert.Equal(payload.Context!.Scope!.OrderedSourcePaths, projection.Value!.Payload.Context!.Scope!.OrderedSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:60`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:66`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:67`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:68`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:70`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:71`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:72`: `Assert.Equal(payload.Context.Scope.OrderedSourcePaths, graph.GetProperty("ownership").EnumerateArray().Select(item => item.GetProperty("logicalPath").GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:282`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:285`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:366`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation#4`, parameters: "project-transitive", "12", 3, true, true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:43`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:46`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:47`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:48`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == HelperPath)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:54`: `Assert.Equal(payload.Context!.Scope!.OrderedSourcePaths, projection.Value!.Payload.Context!.Scope!.OrderedSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:60`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:66`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:67`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:68`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:70`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:71`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:72`: `Assert.Equal(payload.Context.Scope.OrderedSourcePaths, graph.GetProperty("ownership").EnumerateArray().Select(item => item.GetProperty("logicalPath").GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:282`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:285`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:366`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation#5`, parameters: "solution-transitive", "12", 2, true, true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:43`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:46`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:47`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:48`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == HelperPath)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:54`: `Assert.Equal(payload.Context!.Scope!.OrderedSourcePaths, projection.Value!.Payload.Context!.Scope!.OrderedSourcePaths)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:60`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:66`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:67`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:68`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:70`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:71`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:72`: `Assert.Equal(payload.Context.Scope.OrderedSourcePaths, graph.GetProperty("ownership").EnumerateArray().Select(item => item.GetProperty("logicalPath").GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:282`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:285`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:366`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#0`, parameters: "missing-scope"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#1`, parameters: "missing-source"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#2`, parameters: "missing-additional"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#3`, parameters: "source-identity"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#4`, parameters: "duplicate-path"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#5`, parameters: "missing-parse"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#6`, parameters: "invalid-language"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#7`, parameters: "missing-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#8`, parameters: "foreign-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#9`, parameters: "conflicting-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#10`, parameters: "root-membership"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#11`, parameters: "missing-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#12`, parameters: "unknown-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#13`, parameters: "declared-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#14`, parameters: "solution-without-projects"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#15`, parameters: "supporting-compilation"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#16`, parameters: "ordered-membership"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#17`, parameters: "source-budget"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#18`, parameters: "project-budget"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalScopeTests.MissingConflictingInvalidOrUnsupportedScope_FailsBeforeStart#19`, parameters: "root-path"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:150`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalScopeTests.SupportingSourceUnavailableLanguage_FailsInActualHistoricalCompilerWithoutDowngrade`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs`

Case `ExceptionFlowHistoricalScopeTests.SupportingSourceUnavailableLanguage_FailsInActualHistoricalCompilerWithoutDowngrade#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:160`: `Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Failure!.WorkerFailure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:161`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:366`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalScopeTests.DeclaredMode_IsGenuineDetectorPolicyNotInventedWorkerTraversal`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs`

Case `ExceptionFlowHistoricalScopeTests.DeclaredMode_IsGenuineDetectorPolicyNotInventedWorkerTraversal#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:191`: `Assert.NotEmpty(normal.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:192`: `Assert.True(JsonSerializer.SerializeToElement(normal.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:193`: `Assert.Equal(0, JsonSerializer.SerializeToElement(declared.Findings).GetArrayLength())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:196`: `Assert.Null(rejected.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:282`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:285`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:292`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalScopeTests.CrossProjectSourceClosure_IsRealCurrentInputAndExplicitB15Boundary`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs`

Case `ExceptionFlowHistoricalScopeTests.CrossProjectSourceClosure_IsRealCurrentInputAndExplicitB15Boundary#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:217`: `Assert.NotEmpty(solutionFlow.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:218`: `Assert.NotEqual(projectFlow, solutionFlow)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:219`: `Assert.Contains(solutionFlow.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == "/project-b/Helper.cs")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:220`: `Assert.Equal(solutionFlow, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(root)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:222`: `Assert.Null(refused.Result)`

## `ExceptionFlowHistoricalScopeTests.ForeignRootAndForgedSourceWorldOrPath_AreNotAccepted`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs`

Case `ExceptionFlowHistoricalScopeTests.ForeignRootAndForgedSourceWorldOrPath_AreNotAccepted#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalScopeTests.cs:250`: `Assert.Null(wrong.Result)`

## `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#0`, parameters: 2, "chain"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#1`, parameters: 3, "chain"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#2`, parameters: 2, "source-type"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#3`, parameters: 2, "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#4`, parameters: 3, "mixed"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#5`, parameters: 2, "multiple"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#6`, parameters: 3, "multiple"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#7`, parameters: 3, "supporting-sources"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#8`, parameters: 2, "empty"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#9`, parameters: 3, "empty"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#10`, parameters: 2, "exact"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#11`, parameters: 2, "base"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#12`, parameters: 2, "invalid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#13`, parameters: 2, "inheritdoc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission#14`, parameters: 2, "no-doc"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:44`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(first.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:45`: `Assert.Equal(Json(first.Result), Json(repeat.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:47`: `Assert.Equal(native.GetProperty("graphCount").GetInt32(), graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:48`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:49`: `Assert.Equal(graph.GetProperty("summaries").GetRawText(), graph.GetProperty("repeatedSummaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:50`: `Assert.Equal(Json(first.Result), graph.GetProperty("result").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:51`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:56`: `Assert.Empty(first.Result!.Uncertainties)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:57`: `Assert.All(native.GetProperty("findings").EnumerateArray(), finding => Assert.Equal(payload.LogicalPath, finding.GetProperty("filePath").GetString()))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:58`: `Assert.Empty(first.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:64`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalSolutionFindingTests.AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs`

Case `ExceptionFlowHistoricalSolutionFindingTests.AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:94`: `Assert.Equal(native.GetProperty("result").GetRawText(), Json(historical.Result))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:96`: `Assert.Equal(native.GetProperty("summaries").GetRawText(), graph.GetProperty("summaries").GetRawText())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:104`: `Assert.Single(historical.Result!.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:106`: `Assert.Contains(entry.Paths.SelectMany(path => path.Steps), step => step.FilePath == "/solution/p1/Source.cs" && step.Kind.ToString() == "ExplicitThrow")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:107`: `Assert.Single(native.GetProperty("findings").EnumerateArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:108`: `Assert.Equal("DOC611", native.GetProperty("findings")[0].GetProperty("smell").GetProperty("id").GetString())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:205`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:212`: `Assert.Empty(mismoded.Findings)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:291`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution#0`, parameters: "root-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:157`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution#1`, parameters: "source-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:157`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution#2`, parameters: "dependency-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:157`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution#3`, parameters: "reference-mvid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:157`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution#4`, parameters: "missing-edge"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:157`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution#5`, parameters: "duplicate-path"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:157`: `Assert.Null(rejected.Result)`

Case `ExceptionFlowHistoricalSolutionFindingTests.ExistingGraphValidation_RejectsProjectSourceReferenceAndRootSubstitution#6`, parameters: "root-member"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs:157`: `Assert.Null(rejected.Result)`

## `ExceptionFlowHistoricalSolutionScopeTests.Solution_OriginalProjectsFullFlowGraphFindingsOwnershipEdgesRepeatAndIsolation`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`

Case `ExceptionFlowHistoricalSolutionScopeTests.Solution_OriginalProjectsFullFlowGraphFindingsOwnershipEdgesRepeatAndIsolation#0`, parameters: 2, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:38`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:39`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == PathFor(count - 1))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:43`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:48`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:49`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:50`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:52`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:53`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:234`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:237`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:240`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionScopeTests.Solution_OriginalProjectsFullFlowGraphFindingsOwnershipEdgesRepeatAndIsolation#1`, parameters: 3, false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:38`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:39`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == PathFor(count - 1))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:43`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:48`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:49`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:50`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:52`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:53`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:234`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:237`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:240`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionScopeTests.Solution_OriginalProjectsFullFlowGraphFindingsOwnershipEdgesRepeatAndIsolation#2`, parameters: 2, true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:38`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:39`: `Assert.Contains(native.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath == PathFor(count - 1))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:43`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:48`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:49`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:50`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(item => item.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:52`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:53`: `Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(first.Result, WorkerProtocol.JsonOptions), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:234`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:237`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:240`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalSolutionScopeTests.LocalModes_DoNotWidenToSupportingProjects`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`

Case `ExceptionFlowHistoricalSolutionScopeTests.LocalModes_DoNotWidenToSupportingProjects#0`, parameters: "Direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:95`: `Assert.NotEqual(solution.Result, local.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:97`: `Assert.DoesNotContain(local.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath != PathFor(0))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:100`: `Assert.Null(refusal.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:103`: `Assert.Equal(local.Result, worker.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:234`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:237`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:240`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

Case `ExceptionFlowHistoricalSolutionScopeTests.LocalModes_DoNotWidenToSupportingProjects#1`, parameters: "ProjectTransitive"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:95`: `Assert.NotEqual(solution.Result, local.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:97`: `Assert.DoesNotContain(local.Result.Entries.SelectMany(entry => entry.Paths).SelectMany(path => path.Steps), step => step.FilePath != PathFor(0))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:100`: `Assert.Null(refusal.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:103`: `Assert.Equal(local.Result, worker.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:234`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:237`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:240`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#0`, parameters: "missing-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#1`, parameters: "unknown-target"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#2`, parameters: "duplicate-identity"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#3`, parameters: "conflicting-identity"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#4`, parameters: "conflicting-ownership"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#5`, parameters: "dangling-edge"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#6`, parameters: "missing-reference"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#7`, parameters: "wrong-mvid"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#8`, parameters: "wrong-assembly-version"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#9`, parameters: "missing-edges"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#10`, parameters: "unreachable"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#11`, parameters: "cycle"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#12`, parameters: "nested-graph"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#13`, parameters: "root-mismatch"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#14`, parameters: "unknown-project"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#15`, parameters: "missing-project-references"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#16`, parameters: "declared-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#17`, parameters: "project-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSolutionScopeTests.InvalidGraph_FailsTypedBeforeStartWithoutLocalOrCurrentFallback#18`, parameters: "project-budget"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:154`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalSolutionScopeTests.OriginalGraph_RejectsUnvalidatedCopiedIncompleteAndNestedProjects`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`

Case `ExceptionFlowHistoricalSolutionScopeTests.OriginalGraph_RejectsUnvalidatedCopiedIncompleteAndNestedProjects#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:168`: `Assert.Null(refusal.Result)`

## `ExceptionFlowHistoricalSolutionScopeTests.MissingReachableSourceProject_DoesNotReturnACompleteSmallerScope`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`

Case `ExceptionFlowHistoricalSolutionScopeTests.MissingReachableSourceProject_DoesNotReturnACompleteSmallerScope#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:180`: `Assert.NotEmpty(Native(full, ExceptionAnalysisMode.SolutionTransitive).Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:185`: `Assert.False(result.Succeeded)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:234`: `Assert.Equal(result, Analyze())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:237`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:240`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, target.GetSemanticModel(tree), semantic, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalSolutionScopeTests.SupportingProject_UnsupportedCompilerContextAndForgedWorldAreFailClosed`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`

Case `ExceptionFlowHistoricalSolutionScopeTests.SupportingProject_UnsupportedCompilerContextAndForgedWorldAreFailClosed#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:202`: `Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Failure!.WorkerFailure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs:338`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalSourceContextTests.ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs`

Case `ExceptionFlowHistoricalSourceContextTests.ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation#0`, parameters: "10", 0

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:38`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:41`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:42`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:65`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:71`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:72`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:73`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:75`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:76`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:238`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:239`: `Assert.Equal(summaries, GraphSummaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:242`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:282`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:249`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalSourceContextTests.ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation#1`, parameters: "10", 1

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:38`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:41`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:42`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:65`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:71`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:72`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:73`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:75`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:76`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:238`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:239`: `Assert.Equal(summaries, GraphSummaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:242`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:282`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:249`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalSourceContextTests.ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation#2`, parameters: "10", 2

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:38`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:41`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:42`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:65`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:71`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:72`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:73`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:75`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:76`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:238`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:239`: `Assert.Equal(summaries, GraphSummaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:242`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:282`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:249`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalSourceContextTests.ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation#3`, parameters: "12", 0

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:38`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:41`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:42`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:65`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:71`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:72`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:73`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:75`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:76`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:238`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:239`: `Assert.Equal(summaries, GraphSummaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:242`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:282`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:249`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalSourceContextTests.ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation#4`, parameters: "12", 1

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:38`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:41`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:42`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:65`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:71`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:72`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:73`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:75`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:76`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:238`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:239`: `Assert.Equal(summaries, GraphSummaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:242`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:282`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:249`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

Case `ExceptionFlowHistoricalSourceContextTests.ValidatedParseContext_ActiveProgramFullFlowGraphFindingsProvenanceRepeatAndIsolation#5`, parameters: "12", 2

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:38`: `Assert.Empty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:41`: `Assert.NotEmpty(native.Result.Entries)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:42`: `Assert.True(JsonSerializer.SerializeToElement(native.Findings).GetArrayLength() > 0)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:65`: `Assert.Equal(native.Result, first.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:71`: `Assert.Equal(native.GraphCount, graph.GetProperty("graphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:72`: `Assert.Equal(native.GraphCount, graph.GetProperty("repeatedGraphCount").GetInt32())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:73`: `Assert.Equal(native.Summaries, graph.GetProperty("summaries").EnumerateArray().Select(value => value.GetString()).ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:75`: `Assert.True(graph.GetProperty("repeatedResultEqual").GetBoolean())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:76`: `Assert.True(JsonNode.DeepEquals(JsonNode.Parse(JsonSerializer.Serialize(first.Result, WorkerProtocol.JsonOptions)), JsonNode.Parse(graph.GetProperty("result").GetRawText())))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:238`: `Assert.Equal(result, RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(session.Analyze(member)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:239`: `Assert.Equal(summaries, GraphSummaries(graph))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:242`: `Assert.Equal(JsonSerializer.Serialize(findings), JsonSerializer.Serialize(XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, input.LogicalPath!, compilation.GetSemanticModel(tree), context, options, session)))`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:282`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:249`: `Assert.True(RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(entry.Key, entry.Value, out var summary))`

## `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#0`, parameters: "missing-parse"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#1`, parameters: "missing-profile"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#2`, parameters: "missing-language"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#3`, parameters: "invalid-language"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#4`, parameters: "latest"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#5`, parameters: "missing-symbols"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#6`, parameters: "duplicate-symbols"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#7`, parameters: "invalid-symbol"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#8`, parameters: "true-symbol"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#9`, parameters: "false-symbol"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#10`, parameters: "symbol-budget"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#11`, parameters: "language-conflict"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#12`, parameters: "doc-mode"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#13`, parameters: "source-kind"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#14`, parameters: "missing-scope"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#15`, parameters: "missing-path"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#16`, parameters: "direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#17`, parameters: "project-transitive"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#18`, parameters: "project-transitive-declared-exceptions"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#19`, parameters: "compiler-conflict"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

Case `ExceptionFlowHistoricalSourceContextTests.MissingInvalidContradictoryOrUnsupportedContext_FailsBeforeStart#20`, parameters: "extra-source"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:164`: `Assert.Null(result.Result)`

## `ExceptionFlowHistoricalSourceContextTests.HistoricalUnavailableConcreteLanguage_IsTypedUnsupportedNotDefaultOrCurrent`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs`

Case `ExceptionFlowHistoricalSourceContextTests.HistoricalUnavailableConcreteLanguage_IsTypedUnsupportedNotDefaultOrCurrent#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:172`: `Assert.Equal(WorkerFailureCode.UnsupportedContext, result.Failure!.WorkerFailure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:173`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:282`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name is "XMLDocNormalizer.ExceptionFlow.Historical" or "XMLDocNormalizer.HistoricalWorker")`

## `ExceptionFlowHistoricalSourceContextTests.ForgedReturnedParseWorld_IsNotAcceptedAsAnotherActiveProgram`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs`

Case `ExceptionFlowHistoricalSourceContextTests.ForgedReturnedParseWorld_IsNotAcceptedAsAnotherActiveProgram#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:202`: `Assert.Null(rejected.Result)`

## `ExceptionFlowHistoricalSourceContextTests.ProductionScope_DoesNotSilentlySubstituteSolutionTransitive`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs`

Case `ExceptionFlowHistoricalSourceContextTests.ProductionScope_DoesNotSilentlySubstituteSolutionTransitive#0`, parameters: "Direct"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:218`: `Assert.Null(result.Dispatch)`

Case `ExceptionFlowHistoricalSourceContextTests.ProductionScope_DoesNotSilentlySubstituteSolutionTransitive#1`, parameters: "ProjectTransitive"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:218`: `Assert.Null(result.Dispatch)`

Case `ExceptionFlowHistoricalSourceContextTests.ProductionScope_DoesNotSilentlySubstituteSolutionTransitive#2`, parameters: "ProjectTransitiveDeclaredExceptions"

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSourceContextTests.cs:218`: `Assert.Null(result.Dispatch)`

## `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#0`, parameters: "Direct", false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#1`, parameters: "Direct", true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#2`, parameters: "ProjectTransitive", false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#3`, parameters: "ProjectTransitive", true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#4`, parameters: "ProjectTransitiveDeclaredExceptions", false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#5`, parameters: "ProjectTransitiveDeclaredExceptions", true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#6`, parameters: "SolutionTransitive", false

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

Case `ExceptionFlowProductionCurrentParityTests.Detector_AllModesRetainFindingsAndCallerOwnedSession#7`, parameters: "SolutionTransitive", true

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowProductionCurrentParityTests.cs:51`: `Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession) .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session))`

## `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure`

File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#0`, parameters: self

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#1`, parameters: mutual

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#2`, parameters: three

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#3`, parameters: outgoing

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#4`, parameters: incoming

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#5`, parameters: repeat

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#6`, parameters: independent

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

Case `ExceptionFlowRecursiveSummaryClosureTests.GenuineWorker_UsesIndependentlyProvenFiniteClosure#7`, parameters: acyclic

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:177`: `Assert.Equal(scenario == "independent" ? new[] { "Fault", "Other" } : ["Fault"], current.Entries.Select(entry => entry.ExceptionType.MetadataName).Order().ToArray())`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:184`: `Assert.Equal(current, historical.Result)`

- `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs:187`: `Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical")`

## `HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess`

File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs`

Case `HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess#0`, parameters: "public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:90`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:91`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:92`: `Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, result.Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:93`: `Assert.Equal(code, result.Failure.WorkerFailure!.Code.ToString())`

Case `HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess#1`, parameters: "public static class Fixture { public static void Root() { missing(); } }", "CompilationFailure"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:90`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:91`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:92`: `Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, result.Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:93`: `Assert.Equal(code, result.Failure.WorkerFailure!.Code.ToString())`

Case `HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess#2`, parameters: "public static class Fixture { public static void Other() {} }", "InvalidRequest"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:90`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:91`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:92`: `Assert.Equal(HistoricalWorkerClientFailureCode.StructuredFailure, result.Failure!.Code)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:93`: `Assert.Equal(code, result.Failure.WorkerFailure!.Code.ToString())`

## `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart`

File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#0`, parameters: "missing-source"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#1`, parameters: "mode"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#2`, parameters: "language"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#3`, parameters: "references"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#4`, parameters: "additional-sources"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#5`, parameters: "supporting"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#6`, parameters: "compiler-options"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

Case `HistoricalWorkerClientTests.UnsupportedInput_IsRejectedBeforeProcessStart#7`, parameters: "assembly-name"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:124`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:125`: `Assert.Null(result.Result)`

## `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp`

File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs`

Case `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp#0`, parameters: "crash", "WorkerCrash"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:150`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:151`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:152`: `Assert.Equal(expected, result.Failure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:153`: `Assert.Equal("test crash diagnostic", result.Diagnostics)`

Case `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp#1`, parameters: "malformed", "MalformedResponse"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:150`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:151`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:152`: `Assert.Equal(expected, result.Failure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:153`: `Assert.Equal("test crash diagnostic", result.Diagnostics)`

Case `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp#2`, parameters: "mismatch", "ProtocolMismatch"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:150`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:151`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:152`: `Assert.Equal(expected, result.Failure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:153`: `Assert.Equal("test crash diagnostic", result.Diagnostics)`

Case `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp#3`, parameters: "nonzero", "NonZeroExit"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:150`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:151`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:152`: `Assert.Equal(expected, result.Failure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:153`: `Assert.Equal("test crash diagnostic", result.Diagnostics)`

Case `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp#4`, parameters: "flood", "OutputLimit"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:150`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:151`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:152`: `Assert.Equal(expected, result.Failure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:153`: `Assert.Equal("test crash diagnostic", result.Diagnostics)`

Case `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp#5`, parameters: "stderr-flood", "OutputLimit"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:150`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:151`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:152`: `Assert.Equal(expected, result.Failure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:153`: `Assert.Equal("test crash diagnostic", result.Diagnostics)`

Case `HistoricalWorkerClientTests.AdversarialPipePeer_IsFailClosedAndCleanedUp#6`, parameters: "utf8", "TransportFailure"

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:150`: `Assert.False(result.Success)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:151`: `Assert.Null(result.Result)`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:152`: `Assert.Equal(expected, result.Failure!.Code.ToString())`

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:153`: `Assert.Equal("test crash diagnostic", result.Diagnostics)`

## `HistoricalWorkerClientTests.Deadline_KillsOwnedProcessAndCompletesPipes`

File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs`

Case `HistoricalWorkerClientTests.Deadline_KillsOwnedProcessAndCompletesPipes#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:161`: `Assert.Equal(HistoricalWorkerClientFailureCode.Timeout, result.Failure!.Code)`

## `HistoricalWorkerClientTests.Cancellation_KillsOwnedProcess`

File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs`

Case `HistoricalWorkerClientTests.Cancellation_KillsOwnedProcess#0`, parameters: 

- `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs:170`: `Assert.Equal(HistoricalWorkerClientFailureCode.Cancelled, result.Failure!.Code)`

