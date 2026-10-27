# Understanding analysis coverage

A finding reports a documentation issue. Coverage describes how much of the
requested analysis could be completed. They are independent: an incomplete run
can have zero findings, diagnostic findings, or ordinary findings from other
fully analyzed parts of the input.

## Reading the result

| Field | Values | Meaning |
|---|---|---|
| Execution | `Succeeded`, `Failed`, `NotRun` | The analysis operation ran in a controlled way, failed technically, or was not run. A controlled unsupported analysis is not a process crash. |
| Completion | `Complete` | No missing coverage was observed for the configured checks, selected scope and mode. |
| Completion | `Incomplete` | Required coverage was missing, or execution failed. Missing findings are not a complete negative result. |
| Completion | `Unsupported` | An input capability or semantic profile is explicitly unsupported. This is not a complete analysis result. |
| Completion | `NotAssessed` | No analysis evidence is available. This is also the safe interpretation of an older report without coverage. |
| Scope / Mode | Selected input scope and exception mode | The boundary of the statement; not every project, dependency or runtime behavior in the repository. |
| ReasonCodes | Stable short codes | Machine-readable reasons, not additional findings. |

If 99 targets are complete and one is incomplete, the combined run is incomplete.
A relevant missing supporting-project contribution also prevents Complete.
Reason codes are deduplicated and sorted; aggregation does not depend on target
order or finding counts. Incomplete takes precedence over Unsupported when both
occur; technical failure takes precedence on the execution axis.

Complete is relative to existing rule scope. File filters, project selection,
documentation guards and mode-specific type filters still apply. Exception-flow
checks investigate documented members as before; members without XML
documentation do not acquire a new body analysis. A missing-documentation rule
can report them separately. Complete is not a proof that the program cannot throw,
that all C# behavior is supported, or that every possible runtime target is known.

## Inputs and four exception modes

| Input / mode | Coverage boundary |
|---|---|
| `.cs` or a directory | Syntax/XML checks run. Semantic checks require a project or solution; overall semantic coverage is Unsupported with `SemanticInputRequired`. |
| Project / solution, Direct | Only directly observed escaping throws in the selected member bodies. Calls are not a promise to analyze transitive effects. |
| ProjectTransitive | Existing reporting-project scope and its modeled calls. Unknown targets, missing source/binding or unsupported contexts prevent Complete. |
| SolutionTransitive | Loaded source-project reference closure of the selected reporting projects. Supporting projects contribute evidence without automatically becoming reporting projects. |
| ProjectTransitiveDeclaredExceptions | Existing direct checks plus the reporting-scope exception-type relevance filter for transitive checks. When no relevant declared exception types exist, the existing summary guard remains in effect. |

The default mode is SolutionTransitive. A solution normally selects the project
matching its name, or `--project`; `--full` selects all applicable projects.
Generated/test-file filters remain in effect. Unsupported syntax or binding
errors can conservatively affect coverage even when some documentation checks
still produce useful findings.

Transitive analysis uses a finite exact profile: it can preserve concrete generic
arguments when the reachable contexts are provably finite and all required
operations and targets are covered. Recursion that repeatedly wraps a type in a
larger type, missing bodies, or unavailable operation information can be outside
that profile. The analyzer then declines a complete claim instead of silently
using a partial result. Technical documentation calls this `A9-FiniteOrbit-v1`.
`OutsideProfile` records this boundary; it does not assert that every rejected
program is individually impossible to analyze.

External XML can describe possible exceptions without proving their executable
flow. Existing uncertainty/certification outcomes decide coverage. DOC631 remains
a documentation-specific diagnostic, not the global coverage status: it can be
absent if no applicable exception tag exists.

## Console examples

A fully analyzed selected scope with no issues prints, for example:

```text
Analysis coverage: complete for the selected scope and mode.
Analysis execution: Succeeded. Scope: Workspace. Mode: Direct.
Check succeeded: no documentation issues found.
```

A controlled run with an unresolved target and no findings prints, for example:

```text
Analysis coverage: incomplete; do not interpret missing findings as a complete negative result.
Analysis execution: Succeeded. Scope: Workspace. Mode: SolutionTransitive.
Coverage reasons: OutsideProfile, TargetCoverageIncomplete.
No documentation findings reported. This is not a complete negative analysis result.
```

Actual reasons depend on the input. Findings from other parts of the run remain
visible; the coverage line is separate from the finding summary. Fix mode also
shows coverage and does not certify a semantic analysis of plain files.

## JSON

Use the normal output option, for example:

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
  --check .\Demo.csproj --exception-analysis-mode solution-transitive `
  --format json --output .\artifacts\demo.json
```

Existing Metrics and Findings remain unchanged. The additive section can look
like this alongside an empty Findings array:

```json
{
  "Analysis": {
    "Execution": "Succeeded",
    "Completion": "Incomplete",
    "Scope": "Workspace",
    "Mode": "SolutionTransitive",
    "ReasonCodes": ["OutsideProfile", "TargetCoverageIncomplete"]
  },
  "Findings": []
}
```

This abbreviated example omits the other normal metadata and metrics. No internal
graph, compiler symbol or large certificate is copied into this section. Older
reports without Analysis are not retrospectively Complete. Exception-mode
comparison reports retain coverage for each measured run in `AnalysisRuns`;
value-mode comparisons retain the mode run's `Analysis` separately from counts.

## SARIF

Use `--format sarif --output .\artifacts\demo.sarif`. The same coverage appears
in the existing SARIF run property bag:

```json
{
  "properties": {
    "analysis": {
      "execution": "Succeeded",
      "completion": "Incomplete",
      "scope": "Workspace",
      "mode": "SolutionTransitive",
      "reasonCodes": ["OutsideProfile", "TargetCoverageIncomplete"]
    }
  },
  "results": []
}
```

This is a fragment of `runs[0]`, not a complete SARIF file. Coverage is a run
property, not an invented rule or result. A viewer may hide custom properties;
automation should inspect the property bag explicitly.

## Failures and exit codes

| Exit | Existing meaning |
|---:|---|
| 0 | A normal run reported no findings. Inspect Analysis before claiming completeness. |
| 1 | A normal run reported findings. Coverage remains independent. |
| 2 | Invalid CLI usage/configuration; analysis may not have started. |
| 3 | Requested project was not found. |
| 4 | Bounded external-target operation was rejected or failed. |

No new exit code is introduced. A controlled external rejection can have
Unsupported/Incomplete coverage without a process crash; the existing external
exit is still 4. Technical exceptions retain their existing failure/exit behavior.

External execution failures and rejections continue to produce **no normal
JSON/SARIF finding report**. Their existing error message is accompanied by a
separate coverage/execution explanation on stderr. Invalid CLI usage likewise
does not create a report. If an old output file already exists after a failed
invocation, it is not evidence of that invocation; always check the exit/error
channel. A fatal runtime/process termination can prevent any final output.

Successful bounded external jobs use scope `ExternalSelectedRoot`: their
selected-root exception policy is not a full project documentation check.
Historical/current selection, worker validation and isolation are unchanged.
No fallback is introduced. The existing single-root, finite-profile,
two/three-project-chain and nongeneric-final-type limits remain. A validated
no-documentation guard can finish that selected-root policy without running a
summary; Complete does not claim that an unexecuted summary ran.

## Reason codes

| Code | Meaning |
|---|---|
| OutsideProfile / UnsupportedContext | The existing exact analysis profile does not admit this query. |
| TargetCoverageIncomplete / ExceptionFlowUnresolved | Required callable/operation information or exception flow is unresolved. |
| SourceBindingIncomplete | Existing compiler errors prevent a complete semantic claim. |
| MetadataCoverageIncomplete / TypeBindingIncomplete | Required metadata or exact type binding is unavailable. |
| ScopeCoverageIncomplete / ProjectCoverageIncomplete | A necessary scope/project contribution is missing. |
| WorkspaceLoadIncomplete / CompilationUnavailable / DocumentPathUnavailable / SyntaxTreeUnavailable | Part of the requested workspace could not be processed. |
| ResultProjectionIncomplete / SummaryCertificationUnavailable | No complete certified output is available. |
| ResultCoverageIncomplete | The existing worker response explicitly reports an incomplete analysis or independent Direct result. |
| SemanticInputRequired | Use a project or solution for semantic checks. |
| CoverageNotAssessed | An aggregated component supplied no coverage evidence. |
| WorkerExecutionFailed / WorkerValidationFailed | An external execution or its validation failed; no successful evidence is admitted. |
| ExternalInputUnsupported / ExternalInputOrExecutionFailed | External inputs were rejected or could not be read/executed. |
| FindingEvidenceValidationFailed / AnalysisExecutionFailed | Existing admission/execution could not complete. |

Reason codes describe coverage; they do not alter rule severity, suppress existing
diagnostic findings, or grant permission to adopt an unvalidated analysis result.
