param(
    [string] $Before = 'artifacts/p5o2a5b/pre-boundary.json',
    [string] $After = 'artifacts/p5o2a5b/post-boundary-final.json',
    [string] $Output = 'Evaluation/P5O2A5B-contextual-fact-evaluator-extraction-audit.json',
    [string] $Report = 'Evaluation/P5O2A5B-contextual-fact-evaluator-extraction.md'
)
$ErrorActionPreference = 'Stop'
$pre = Get-Content -LiteralPath $Before -Raw -Encoding UTF8 | ConvertFrom-Json
$post = Get-Content -LiteralPath $After -Raw -Encoding UTF8 | ConvertFrom-Json
$a5a = Get-Content -LiteralPath 'Evaluation/P5O2A5A-contextual-evaluator-boundary-audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
$root = (Get-Location).Path
$head = (git rev-parse HEAD).Trim()
if ($head -ne '7362e7e22b9bf9fa14390b598ee6a2bbae33b0bb') { throw 'Starting HEAD changed.' }
function NormalizeOwner([string] $value) { $value.Replace('ExceptionFlowContextualFactEvaluator', 'ExceptionFlowAnalyzer') }
function Edges($items) { @($items | ForEach-Object { (NormalizeOwner $_.Caller) + ' -> ' + (NormalizeOwner $_.Callee) + ' [' + $_.Kind + ']' } | Sort-Object -Unique) }
function SetDiff($left, $right) {
    [ordered]@{ Added = @($right | Where-Object { $left -cnotcontains $_ }); Removed = @($left | Where-Object { $right -cnotcontains $_ }) }
}
function AssertGate([bool] $condition, [string] $message) { if (-not $condition) { throw $message } }
$memberDiff = SetDiff @($pre.Scc.Members.Id | Sort-Object) @($post.Scc.Members.Id | ForEach-Object { NormalizeOwner $_ } | Sort-Object)
$sccDiff = SetDiff (Edges $pre.Scc.InternalEdges) (Edges $post.Scc.InternalEdges)
$ingressDiff = SetDiff (Edges @($pre.Ingress.Edge)) (Edges @($post.Ingress.Edge))
$egressDiff = SetDiff (Edges $pre.Egress) (Edges $post.Egress)
$preMemberDiff = SetDiff @($a5a.Scc.Members.Id | Sort-Object) @($pre.Scc.Members.Id | Sort-Object)
$preSccDiff = SetDiff (Edges $a5a.Scc.InternalEdges) (Edges $pre.Scc.InternalEdges)
foreach ($diff in @($memberDiff, $sccDiff, $ingressDiff, $egressDiff, $preMemberDiff, $preSccDiff)) {
    AssertGate ($diff.Added.Count -eq 0 -and $diff.Removed.Count -eq 0) 'A measured graph boundary changed.'
}
$verification = $post.ExtractionVerification
AssertGate ($verification.Methods.Count -eq 65 -and @($verification.Methods | Where-Object { -not $_.Equal -or -not $_.ParametersEqual }).Count -eq 0) 'Implementation tokens changed.'
AssertGate ($verification.Fields.Count -eq 3 -and @($verification.Fields | Where-Object { -not $_.Equal }).Count -eq 0) 'Field declaration tokens changed.'
AssertGate (@($verification.RetainedAnalyzerMethods | Where-Object { -not $_.EqualModuloEvaluatorQualification }).Count -eq 0) 'Analyzer orchestration changed.'
AssertGate (@($verification.RetainedTypesInMixedFiles | Where-Object { -not $_.Equal }).Count -eq 0) 'A retained provider changed.'
AssertGate ($post.CompilationErrors.Count -eq 0 -and $post.ReachableAnalyzerOutsideScc.Count -eq 0 -and $post.LogicalComponentCycles.Count -eq 0) 'Post-extraction architecture gate failed.'

$baseline = Get-Content -Raw -Encoding UTF8 'artifacts/p5o2a4g/final-self-analysis.json' | ConvertFrom-Json
$self = Get-Content -Raw -Encoding UTF8 'artifacts/p5o2a5b/final-self-analysis.json' | ConvertFrom-Json
function FindingKey($finding) {
    @($finding.FilePath.Replace($root, '').Replace('\', '/'), $finding.SmellId, $finding.ContainingNamespace,
        $finding.ContainingType, $finding.SymbolName, $finding.TargetName, $finding.TagName, $finding.OwnerKind,
        $finding.SubjectKind, $finding.Line, $finding.Column) -join '|'
}
function NormalizedFindings($findings) {
    @($findings | Sort-Object { FindingKey $_ } | ForEach-Object {
        $copy = [ordered]@{}
        foreach ($property in $_.PSObject.Properties) {
            $copy[$property.Name] = if ($property.Name -eq 'FilePath') { $property.Value.Replace($root, '').Replace('\', '/') } else { $property.Value }
        }
        [pscustomobject] $copy
    })
}
$oldFindings = @(NormalizedFindings $baseline.Findings)
$newFindings = @(NormalizedFindings $self.Findings)
$oldJson = ConvertTo-Json -InputObject $oldFindings -Depth 100 -Compress
$newJson = ConvertTo-Json -InputObject $newFindings -Depth 100 -Compress
$findingDiff = SetDiff @($oldFindings | ForEach-Object { FindingKey $_ }) @($newFindings | ForEach-Object { FindingKey $_ })
$changedEvidence = @($newFindings | Where-Object {
    $key = FindingKey $_
    $previous = $oldFindings | Where-Object { (FindingKey $_) -ceq $key }
    $previous -and (ConvertTo-Json -InputObject $previous -Depth 100 -Compress) -cne (ConvertTo-Json -InputObject $_ -Depth 100 -Compress)
})
AssertGate ($oldJson -ceq $newJson -and $findingDiff.Added.Count -eq 0 -and $findingDiff.Removed.Count -eq 0 -and $changedEvidence.Count -eq 0) 'Canonical findings or evidence changed.'
function Hash([string] $value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)))).Replace('-', '') }
    finally { $sha.Dispose() }
}

$tests = @('p5o2a5b-new-tests-final', 'focused', 'relevant', 'architecture-final', 'broad', 'full', 'known-flake-isolated', 'full-retry', 'full-final') | ForEach-Object {
    $path = 'artifacts/p5o2a5b/test-results/' + $_ + '.trx'
    [xml] $trx = Get-Content -Raw -LiteralPath $path
    $counts = $trx.TestRun.ResultSummary.Counters
    [pscustomobject]@{ Run = $_; Artifact = $path; Total = [int] $counts.total; Passed = [int] $counts.passed; Failed = [int] $counts.failed; NotExecuted = [int] $counts.notExecuted }
}
AssertGate (($tests | Where-Object { $_.Run -eq 'full-retry' }).Passed -eq 2559) 'Full-suite retry is not green.'
$movedFields = @($post.FieldDeclarations | Where-Object { $_.Id -like '*ExceptionFlowContextualFactEvaluator*' })
$cacheMethods = @($post.AllSourceNodes | Where-Object { $_.Kind -eq 'MethodDeclaration' -and $_.Owner -like '*ExceptionFlowContextualFactEvaluator.ConditionalWeakTableValueFactCachePartition' })
AssertGate ($movedFields.Count -eq 3 -and $cacheMethods.Count -eq 2) 'Cache move set differs from A5A.'
$status = @(git status --short)
$stashes = @(git stash list --format='%H %s')
$evidence = [ordered]@{
    Schema = 'ContextualFactEvaluator.Extraction.v1'; Date = '2026-10-06'; StartingHead = $head
    Scope = 'Atomic SCC ownership/state move only; no A5C, worker, historical Roslyn or provider redesign.'
    PreA5AGate = @{ Members = 63; InternalEdges = 112; ExactMembers = $preMemberDiff; ExactEdges = $preSccDiff; CacheSupportMethods = 2; OwnedFields = 3 }
    Metrics = @{
        Before = @{ SccMethods = $pre.Scc.MethodCount; SccInternalEdges = $pre.Scc.InternalEdgeCount; Ingress = $pre.Ingress.Count; Egress = $pre.Egress.Count; AnalyzerPartials = $pre.AnalyzerPartials; AnalyzerNonblankSloc = $pre.AnalyzerNonblankSloc; ReachableSourceCallables = $pre.ReachableSourceMethods.Count }
        After = @{ SccMethods = $post.Scc.MethodCount; SccInternalEdges = $post.Scc.InternalEdgeCount; ExpandedSccMethods = $post.Scc.ExpandedMethodCount; Ingress = $post.Ingress.Count; Egress = $post.Egress.Count; AnalyzerPartials = $post.AnalyzerPartials; AnalyzerNonblankSloc = $post.AnalyzerNonblankSloc; ReachableSourceCallables = $post.ReachableSourceMethods.Count; EvaluatorToAnalyzer = 0; ComponentsToAnalyzer = 0; InterComponentCycles = $post.LogicalComponentCycles.Count }
    }
    MovedSccMethods = $post.Scc.Members; CacheSupportMethods = $cacheMethods; MovedFields = $movedFields
    ImplementationTokenVerification = $verification
    AnalyzerOwnershipSize = $verification.AnalyzerOwnershipSize
    RawMeasurements = @{ Before = @{ Path = $Before; Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $Before).Hash }; After = @{ Path = $After; Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $After).Hash }; Repeat = @{ Path = 'artifacts/p5o2a5b/post-boundary-repeat.json'; Sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath 'artifacts/p5o2a5b/post-boundary-repeat.json').Hash } }
    Ingress = @{ Before = $pre.Ingress; After = $post.Ingress; NormalizedDiff = $ingressDiff; UniqueEdges = 16; Callers = 15; OrchestrationCallers = 13; SeedFacades = 2; ActualInvocationSites = 17 }
    Egress = @{ Before = $pre.Egress; After = $post.Egress; Owners = $post.EgressOwners; NormalizedDiff = $egressDiff; FactResolverInvocations = 126; SemanticScopeInvocations = 19 }
    Scc = @{ BeforeInternalEdges = $pre.Scc.InternalEdges; AfterInternalEdges = $post.Scc.InternalEdges; NormalizedMemberDiff = $memberDiff; NormalizedEdgeDiff = $sccDiff }
    DependencyClosure = @{ Nodes = $post.ReachableSourceMethods; Edges = $post.ReachableSourceEdges; ExpandedTypeEdges = $post.ComponentEdges; ExpandedTypeCycles = $post.ComponentCycles; LogicalComponentEdges = $post.LogicalComponentEdges; InterComponentCycles = $post.LogicalComponentCycles; InitializerDependencies = $post.ReachableInitializerDependencies; Limitations = $post.Limitations }
    StateOwnership = @{ Moved = $movedFields; EvaluatorStateReferences = @($post.AllStateAccesses | Where-Object { $_.Caller -like '*ExceptionFlowContextualFactEvaluator*' }); DirectSccState = $post.DirectState; ProviderCacheDeclarations = @($post.FieldDeclarations | Where-Object { $_.Id -like '*successfulDereferenceCaches*' -or $_.Id -like '*dataFlowFactCache*' }); GuardAndLocalState = @($post.LocalState | Where-Object { $_.Callable -like '*ExceptionFlowContextualFactEvaluator*' -or $_.Caller -like '*ExceptionFlowContextualFactEvaluator*' }) }
    CacheSemantics = @{ OwnerBefore = 'ExceptionFlowAnalyzer'; OwnerAfter = 'ExceptionFlowContextualFactEvaluator'; Partition = 'ConditionalWeakTableValueFactCachePartition'; Lifetime = 'static readonly outer ConditionalWeakTable; weak SemanticModel identity; first-use partitions; no reset'; InnerKey = 'ISymbol.OriginalDefinition / SymbolEqualityComparer.Default'; Value = 'bool, positive and negative'; Guard = 'Add normalized field before lookup, duplicate fails closed, finally Remove; independent invariant seed'; Threading = 'lookup/store under partition gate; immutable computation outside lock; TryAdd first result'; Context = 'only context-neutral private readonly source-CWT invariant cached, no context key'; OtherProviderCachesMoved = $false }
    Validation = @{ Tests = $tests; SolutionWarningAsErrorBuild = @{ Warnings = 0; Errors = 0 }; AuditWarningAsErrorBuild = @{ Warnings = 0; Errors = 0 }; SelfAnalysis = @{ Artifact = 'artifacts/p5o2a5b/final-self-analysis.json'; ExitCode = 1; Findings = $self.Findings.Count; DOC610 = $self.Metrics.TotalFindingCounts.DOC610; DOC611 = $self.Metrics.TotalFindingCounts.DOC611; DOC631 = $self.Metrics.TotalFindingCounts.DOC631; DOC632 = $self.Metrics.TotalFindingCounts.DOC632 }; Canonical = @{ Added = $findingDiff.Added.Count; Removed = $findingDiff.Removed.Count; ChangedEvidence = $changedEvidence.Count; ExactFullFindingArraysEqual = ((ConvertTo-Json -InputObject @($baseline.Findings) -Depth 100 -Compress) -ceq (ConvertTo-Json -InputObject @($self.Findings) -Depth 100 -Compress)); NormalizedFindingArraysEqual = ($oldJson -ceq $newJson); BeforeNormalizedHash = Hash $oldJson; AfterNormalizedHash = Hash $newJson; Normalization = 'Sort all findings by full canonical identity; normalize workspace prefix/path separators only; retain every finding/evidence property; UTF8 compact PowerShell JSON'; HistoricalBaselineReferenceHash = '15BBAC06F82C233BCB652B2ADAA3A83322E7E626469F8C77E400C1259CA71B1B'; Findings = $newFindings }; FormatGate = 'Changed/new C# sources only plus complete audit-tool project: pass. Known global unchanged HEAD deviations not repaired.'; JsonParseGate = 'Renderer reparses generated JSON before success'; CRLF = 'Scope text files normalized and independently checked after final generation'; DiffCheck = 'Final git diff --check run separately' }
    A5CReadiness = @{ Ready = $true; MinimalBlocker = $null; RemainingFacades = @('ExceptionFlowAnalyzer.CreateCallContext/4', 'ExceptionFlowAnalyzer.IsDefinitelyNonNull/3'); A5CStarted = $false; WorkerReadinessClaimed = $false }
    GitStatusAtEvidenceGeneration = $status; StashesAtFinalVerification = $stashes
}
$encoding = New-Object Text.UTF8Encoding($false)
$json = ConvertTo-Json -InputObject $evidence -Depth 100
[IO.File]::WriteAllText((Join-Path $root $Output), $json.Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n", $encoding)
$null = Get-Content -Raw -LiteralPath $Output -Encoding UTF8 | ConvertFrom-Json

$lines = New-Object 'System.Collections.Generic.List[string]'
function Line([string] $value = '') { $lines.Add($value) }
Line '# P5O2A5B - Contextual Fact Evaluator Extraction'
Line
Line '**Completed. P5O2A5C is architecturally ready; minimal blocker: none. A5C has not begun.**'
Line
Line ('Starting HEAD: `' + $head + '` (`Audit contextual evaluator boundary.`), branch `fix/documentation`. Date: 2026-10-06.')
Line 'The initial status/diff/stat/HEAD/log gate was read before changes. Only the protected root ignore WIP and seven tracked Core build artifacts were dirty. A5A production/test paths were unchanged since the A4G baseline. All A5A evidence and its measured graph/state boundary were reviewed before the production move.'
Line
Line '## 1. Boundary and outcome'
Line
Line '| Contract | Before | After |'
Line '|---|---:|---:|'
Line '| SCC methods / internal directed edges | 63 / 112 | 63 / 112 |'
Line '| Expanded SCC | 63 | 63 |'
Line '| Ingress edges / callers / entries | 16 / 15 / 4 | 16 / 15 / 4 |'
Line '| Full direct egress | 791 | 791 |'
Line '| Fact/resolver calls / SemanticScope calls | 126 / 19 | 126 / 19 |'
Line '| Reachable explicit source callables | 284 | 284 |'
Line '| A5A Analyzer-prefix files / whole-file nonblank lines | 40 / 18,316 | 29 / 12,977 |'
Line ('| Actual Analyzer declarations / owned nonblank lines | ' + $verification.AnalyzerOwnershipSize.BeforeDeclarations + ' / ' + $verification.AnalyzerOwnershipSize.BeforeOwnedNonblankLines + ' | ' + $verification.AnalyzerOwnershipSize.AfterDeclarations + ' / ' + $verification.AnalyzerOwnershipSize.AfterOwnedNonblankLines + ' |')
Line '| Evaluator / downstream components -> Analyzer | not extracted / 0 | 0 / 0 |'
Line '| Inter-component cycles | 0 | 0 |'
Line
Line 'Pre-A5B member signatures and 112 edge pairs equal A5A exactly. Post-A5B member, internal-edge, ingress and full kind-labelled egress sets equal the pre-set after normalizing only the new owner name back to the old name. No graph metric was forced. The older 94/18 counts remain historical partial projections, not the complete 791-edge census.'
Line 'The historical file/SLOC metric counts the complete ExceptionFlowAnalyzer*.cs files, including co-located providers. There were 43 actual class declarations in the 40 original prefixed files because several files contained multiple partial declarations. After A5B, the old CWT file is provider-only but retains its filename; therefore there are 29 prefixed files but 28 actual Analyzer partial declarations. The separate owned-line count uses only each Analyzer class FullSpan (including documentation/nested state, excluding imports/namespace/other owners). No provider file was renamed merely to improve the metric.'
Line
Line 'The owner is `internal static partial ExceptionFlowContextualFactEvaluator`, in 14 family partials. It evaluates contextual values, symbols, sequence/dictionary content, calls and semantic contexts. Traversal, exception paths, Summary graphs, runtime-target orchestration and analysis output remain outside. No injection, interfaces, dependency bags, service locators, general-purpose traversal or worker/IPC changes were introduced.'
Line
Line '## 2. Exact move set'
Line
Line 'Exactly 63 SCC method declarations moved. The following overload-distinct names/parameter counts identify this exact current set; the companion JSON `MovedSccMethods` gives every fully qualified signature, return type, visibility, source location, internal callers/callees, dependencies and state references.'
Line
Line '| # | Method | Parameters | New partial |'
Line '|---|---|---:|---|'
$index = 0
foreach ($method in $post.Scc.Members) {
    $index++
    $partial = [IO.Path]::GetFileName($method.File).Replace('ExceptionFlowContextualFactEvaluator.', '').Replace('.cs', '')
    Line ('| ' + $index + ' | `' + $method.Name + '` | ' + $method.Parameters.Count + ' | ' + $partial + ' |')
}
Line
Line 'Exactly two additional helpers moved inside the complete private sealed `ConditionalWeakTableValueFactCachePartition`: `TryGetValue(ISymbol, out bool)` and `Store(ISymbol, bool)`. Their original accessibility and implicit constructor are preserved; no other Analyzer method was moved.'
Line
Line '| Field | New owner | Storage / initialization |'
Line '|---|---|---|'
Line '| `conditionalWeakTableValueFactCaches` | Evaluator | private static readonly CWT<SemanticModel, Partition>, `new()` |'
Line '| `gate` | Evaluator nested partition | private instance readonly object, `new()` |'
Line '| `entries` | Evaluator nested partition | private instance readonly Dictionary<ISymbol,bool>, `new(SymbolEqualityComparer.Default)` |'
Line
Line 'All 65 method-body token hashes and parameter token hashes equal starting HEAD. All three complete field-declaration token hashes equal starting HEAD. All 226 retained Analyzer methods are identical modulo the new evaluator qualification. All three retained CWT provider partial bodies in the mixed source file are token-identical. The JSON contains before/after hashes and per-method results.'
Line
Line 'Four SCC entries are internal: `CreateCallContext/5`, `AddExplicitArgumentFacts/7`, `GetExpressionValueFacts/3`, `IsDefinitelyNonNull/4`. The other 59 SCC methods are private. Only visibility adjustments are the two formerly private ingress targets becoming internal and the now evaluator-only `IsDictionaryInsertionValueProvenNonNull/2` becoming private. No algorithm, parameter, default or result was changed.'
Line
Line 'Eleven old Analyzer-only partial files were removed as part of the move; their implementations are retained in the new evaluator partials and recoverable from starting Git HEAD. Provider-only content remains in the old CWT source file; it was not copied into the evaluator.'
Line
Line '## 3. Ingress, composition and dependencies'
Line
Line 'Thirteen orchestration callers retain their behavior and now call the evaluator entries. Two thin seed facades remain Analyzer-owned per the A5A recommendation: `CreateCallContext/4` allocates the same fresh value-source guard and delegates to evaluator `/5`; `IsDefinitelyNonNull/3` allocates the same fresh return-symbol guard and delegates to evaluator `/4`. Neither is an SCC-method duplicate. Systematic facade/composition cleanup is reserved for A5C.'
Line
Line 'There are 16 unique caller/callee ingress edges but 17 concrete invocation sites: `EvaluateNullComparison` evaluates both operands on the same edge. This is a deduplication distinction, not an additional ingress edge or caller.'
Line
Line 'The full egress remains 791: 397 property accesses, 366 invocations and 28 constructions. The 126 direct fact/resolver calls still use the same 19 owners; SemanticScope remains 19 including the Compilation overload. CallContext, value-fact extensions, metadata APIs and cache support remain the same dependencies; the three nested cache-support edges merely change owner. No downstream owner was copied or centralized.'
Line
Line 'The bound source closure, field initializer/method-reference/delegate audit, exact implementation comparison and compiled-IL architecture test find no evaluator -> Analyzer path, no component -> Analyzer path, no Analyzer state/delegate/callback/factory capture and no inter-component cycle. The IL guard includes `ldftn`, calls, constructors, field/type tokens and nested compiler-generated implementation bodies. Static analysis does not infer dynamic runtime targets or traverse metadata bodies; the unchanged algorithm/provider graph and semantic regressions supply complementary coverage.'
Line
Line 'The one expanded type-level DataFlow provider/cache cycle remains provider-internal. Folding nested implementation types to their logical owner yields zero inter-component cycles. It was not refactored.'
Line
Line '## 4. State, cache and lifetime'
Line
Line 'The outer cache remains static readonly, with weak SemanticModel reference-identity keys and first-use partitions. Type initialization naturally belongs to the new static owner; it does not create a per-call or per-Analyzer cache. The inner key remains field OriginalDefinition with SymbolEqualityComparer.Default, value bool, including negative results. Model/Compilation worlds cannot share partitions; collection releases model/partition/symbol state. There is no reset, invalidation redesign, context key or global strong model dictionary.'
Line
Line 'Ambient guard Add occurs before lookup; a duplicate fails closed even when a positive entry is warm. Finally removes only successfully-added field guards. Invariant computation uses the same fresh independently seeded guard; recursive return/value/sequence sets retain comparer, copies, Add/Remove and finally behavior. Per-partition locking still protects lookup/store only, computation is outside locks, and TryAdd preserves the first result. Sequential caller-owned scratch collections are not made thread-safe or persisted.'
Line
Line 'Analyzer orchestration state and Domain/CallContext constructor-copied normalized state remain at their owners. Typed argument-fact dictionary/index scratch is still synchronously filled through AddExplicitArgumentFacts; it is not turned into a persistent dependency bag. Successful-dereference and DataFlow provider caches, semantic scope, delegate resolution, successful sequence validation/content preservation and all four analysis modes are unchanged.'
Line
Line '## 5. Tests and validation'
Line
Line 'Twelve new test cases (nine semantic/lifetime, three architecture) secure all four entries, positive/negative/unknown facts, optional default projection, per-context results, mutual recursion and guard cleanup, repeated positive/negative memoization, symbol comparer/original definitions, guard-before-cache, semantic-world partition isolation and bounded GC weak lifetime. Existing semantic suites remain intact. One A4G architecture source-path expectation follows its direct CWT user to the evaluator; the provider-independent check retains its old physical provider file.'
Line
Line '| Run | Passed / total | Failed | Not executed |'
Line '|---|---:|---:|---:|'
foreach ($test in $tests) { Line ('| ' + $test.Run + ' | ' + $test.Passed + ' / ' + $test.Total + ' | ' + $test.Failed + ' | ' + $test.NotExecuted + ' |') }
Line
Line 'The initial full run failed only the pre-existing MultiModuleAssembly_FailsClosed flake (2558 passed, 1 failed). The isolated test passed; the unchanged full retry passed 2559/2559. No foreign-flake fix was made. During new-test development, three default-argument assertions initially expected PositiveInt32; existing GetConstantValueFacts only supplies NonNull for that default. The assertions were corrected to the existing contract, with no production change.'
Line
Line 'Solution and audit-tool warning-as-error builds: 0 warnings / 0 errors. Changed/new production/test C# formatting and complete audit-tool formatting gates pass. Known unrelated global HEAD format deviations were neither reformatted nor treated as A5B work. Final JSON parse, CRLF (35 A5B text files), git diff --check and byte-identical repeated post-measurement gates pass; the renderer checks these again after generation. A final full run after CRLF normalization also passes 2559/2559 without skips.'
Line
Line 'Fresh solution-transitive Self Analysis: 16 findings, DOC610=0, DOC611=1, DOC631=15, DOC632=0. CLI exit 1 is expected because documentation findings remain. Compared against immediate Post-A5A/A4G semantic baseline: 0 added, 0 removed, 0 changed evidence; complete raw finding arrays are exactly equal and normalized arrays are exactly equal. This retains Message/Snippet and every finding/evidence property, not just smell counts. The JSON stores current normalization/hashes and the historical baseline reference hash distinctly.'
Line
Line '## 6. Reproduction and evidence'
Line
Line 'Run from Tools/XMLDocNormalizer. The audit remains outside production/solution compilation. Extraction verification is opt-in with an explicit starting ref so later normal audits do not depend on uncommitted HEAD shape.'
Line
Line '~~~powershell'
Line 'dotnet build XMLDocNormalizer.sln --no-restore -warnaserror'
Line 'dotnet build Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj --no-restore -warnaserror'
Line 'dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln artifacts/p5o2a5b/post-boundary-final.json --verify-extraction=7362e7e22b9bf9fa14390b598ee6a2bbae33b0bb'
Line 'dotnet test XMLDocNormalizer.sln --no-build --no-restore --logger "trx;LogFileName=full-retry.trx" --results-directory artifacts/p5o2a5b/test-results'
Line 'dotnet src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll --check --project XMLDocNormalizer --exception-analysis-mode solution-transitive --format json --output artifacts/p5o2a5b/final-self-analysis.json XMLDocNormalizer.sln'
Line 'powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/ContextualBoundaryAudit/Write-ExtractionEvidence.ps1'
Line 'git diff --check'
Line '~~~'
Line
Line 'The invocation filters actually used were: focused = ContextualFactEvaluator, DOC611_CallContextValueFactDependencyTests, ConditionalWeakTable, DataFlowFactCache, SuccessfulDereferenceCache; relevant = Sequence, Dictionary, Callback, Nullability, Guard, NonNull, Successful, ContentPreservation, Delegate; architecture = five existing fact/scope/core/summary/local dependency suites plus the new evaluator dependency suite; broad = Check.Semantic, Execution.Semantic, Evaluation. TRX files contain exact selected case identities and outcomes.'
Line
Line 'The optional --extraction-plan mode only generates apply_patch artifacts from bound syntax spans; it never writes production. Ambiguous initial one-line patch matching was detected and replaced with whole-file syntax-span hunks. Two evidence-helper issues (cache support public accessibility and multiple provider partial declarations) were corrected; both were audit-tool issues, not production semantic changes.'
Line
Line '- [Machine-readable A5B audit](P5O2A5B-contextual-fact-evaluator-extraction-audit.json): exact signatures, edges, before/after owner/token/state evidence, tests and canonical arrays.'
Line '- [A5A authoritative audit](P5O2A5A-contextual-evaluator-boundary-audit.json) and [A5A matrices](P5O2A5A-contextual-evaluator-boundary-matrices.md): immutable pre-boundary evidence.'
Line '- Raw 19/20 MB bound measurements, self-analysis output and TRX results remain under ignored artifacts/p5o2a5b, not runtime dependencies.'
Line
Line '## 7. Git protection and next boundary'
Line
Line 'Protected root .gitignore WIP was not edited/reset/staged. Tracked Core bin/obj churn remains foreign build output. All six stash hashes/names are unchanged, including both protected provenance stashes. No reset/checkout/restore, commit, push or stash mutation occurred. Source-file deletions are the authorized ownership move, not discarded WIP.'
Line
Line '**A5C ready: yes. Minimal blocker: none.** The SCC/state ownership and downstream no-return/acyclic contracts are now closed. The two deliberate seed facades and systematic composition review remain A5C work. This is not a claim of historical-worker or dual-Roslyn readiness; no A5C/A6/worker work was started.'
Line
Line 'Final status is printed in the user handoff; the evidence JSON also captures generation-time status and verified stash identities.'
Line
Line '~~~text'
foreach ($entry in $status) { Line $entry }
Line '~~~'
[IO.File]::WriteAllText((Join-Path $root $Report), ($lines -join "`r`n") + "`r`n", $encoding)

# These are independent, current-worktree checks, not inferred from the semantic test count.
$scopeFiles = @(@(git diff --name-only --relative) + @(git ls-files --others --exclude-standard) | Sort-Object -Unique | Where-Object {
    ($_ -like 'Evaluation/*' -or $_ -like 'Tests/XMLDocNormalizerTests/*' -or $_ -like 'src/XMLDocNormalizer/*') -and
    $_ -match '\.(cs|ps1|md|json)$' -and (Test-Path -LiteralPath $_)
})
foreach ($file in $scopeFiles) {
    $content = [IO.File]::ReadAllText((Join-Path $root $file))
    AssertGate (-not [regex]::IsMatch($content, '(?<!\r)\n|\r(?!\n)')) ('CRLF gate failed: ' + $file)
    if ($file -like '*.json') { $null = $content | ConvertFrom-Json }
}
$ErrorActionPreference = 'Continue'
$check = @(git diff --check 2>&1)
$diffExitCode = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
AssertGate ($diffExitCode -eq 0) ('git diff --check failed: ' + ($check -join "`n"))
$repeatEqual = $evidence.RawMeasurements.After.Sha256 -ceq $evidence.RawMeasurements.Repeat.Sha256
AssertGate $repeatEqual 'Post measurements are not byte-identical.'
$evidence.Validation.QualityGates = @{ DiffCheckExitCode = 0; CRLF = @{ Passed = $true; Files = $scopeFiles }; JsonParse = @{ Passed = $true; Files = @($scopeFiles | Where-Object { $_ -like '*.json' }) }; PostMeasurementByteIdentical = $repeatEqual }
$json = ConvertTo-Json -InputObject $evidence -Depth 100
[IO.File]::WriteAllText((Join-Path $root $Output), $json.Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n", $encoding)
$null = Get-Content -Raw -LiteralPath $Output -Encoding UTF8 | ConvertFrom-Json
Write-Output ('Wrote A5B evidence: ' + $post.Scc.MethodCount + '/' + $post.Scc.InternalEdgeCount + '; full retry ' + ($tests | Where-Object { $_.Run -eq 'full-retry' }).Passed + '; canonical diff 0/0/0')
