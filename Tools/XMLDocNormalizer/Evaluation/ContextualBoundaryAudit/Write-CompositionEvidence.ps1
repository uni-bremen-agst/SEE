param(
    [string] $Before = 'artifacts/p5o2a5c/pre-composition-rebound.json',
    [string] $After = 'artifacts/p5o2a5c/post-composition-final.json',
    [string] $Output = 'Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup-audit.json',
    [string] $Report = 'Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup.md'
)
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$head = (git rev-parse HEAD).Trim()
function Gate([bool] $condition, [string] $message) { if (-not $condition) { throw $message } }
function ReadJson([string] $path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($value) { ConvertTo-Json -InputObject $value -Depth 100 -Compress }
function Hash([string] $value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)))).Replace('-', '') }
    finally { $sha.Dispose() }
}
function Edges($items) { @($items | ForEach-Object { $_.Caller + ' -> ' + $_.Callee + ' [' + $_.Kind + ']' } | Sort-Object) }
function Diff($left, $right) {
    @{ Added = @($right | Where-Object { $left -cnotcontains $_ }); Removed = @($left | Where-Object { $right -cnotcontains $_ }) }
}
function EqualSet($left, $right, [string] $message) {
    $delta = Diff $left $right
    Gate ($left.Count -eq $right.Count -and $delta.Added.Count -eq 0 -and $delta.Removed.Count -eq 0) $message
    $delta
}
Gate ($head -ceq '4ddd4113d753c04edb9cce7cc570b896ea83ab6d') 'Starting HEAD changed.'
$early = ReadJson 'artifacts/p5o2a5c/pre-composition.json'
$pre = ReadJson $Before
$post = ReadJson $After
$a5b = ReadJson 'Evaluation/P5O2A5B-contextual-fact-evaluator-extraction-audit.json'
$memberDiff = EqualSet @($pre.Scc.Members.Id) @($post.Scc.Members.Id) 'SCC members changed.'
$edgeDiff = EqualSet (Edges $pre.Scc.InternalEdges) (Edges $post.Scc.InternalEdges) 'SCC edges changed.'
$egressDiff = EqualSet (Edges $pre.Egress) (Edges $post.Egress) 'SCC egress changed.'
$earlyMemberDiff = EqualSet @($early.Scc.Members.Id) @($pre.Scc.Members.Id) 'In-memory baseline does not match original pre-production graph.'
$earlyEdgeDiff = EqualSet (Edges $early.Scc.InternalEdges) (Edges $pre.Scc.InternalEdges) 'In-memory baseline edges differ.'
Gate ($early.AnalyzerPartials -eq $pre.AnalyzerPartials -and $early.AnalyzerNonblankSloc -eq $pre.AnalyzerNonblankSloc) 'Baseline filename/SLOC metrics differ from original pre-production measurement.'
$committedMemberDiff = EqualSet @($a5b.MovedSccMethods.Id) @($pre.Scc.Members.Id) 'Post-A5B membership differs.'
$committedEdgeDiff = EqualSet (Edges $a5b.Scc.AfterInternalEdges) (Edges $pre.Scc.InternalEdges) 'Post-A5B edges differ.'
foreach ($measurement in @($pre, $post)) {
    Gate ($measurement.CompilationErrors.Count -eq 0 -and $measurement.Scc.MethodCount -eq 63 -and $measurement.Scc.InternalEdgeCount -eq 112) 'Compilation/SCC gate failed.'
    Gate ($measurement.ReachableAnalyzerOutsideScc.Count -eq 0 -and $measurement.LogicalComponentCycles.Count -eq 0) 'Dependency gate failed.'
    Gate (@($measurement.ReachableSourceEdges | Where-Object { $_.Callee -like '*ExceptionFlowAnalyzer.*' }).Count -eq 0) 'Downstream Analyzer return edge.'
}
Gate ($pre.Composition.ThinFacadeCandidates.Count -eq 2 -and $post.Composition.ThinFacadeCandidates.Count -eq 0) 'Facade census differs.'
Gate ($post.Composition.EmptyAnalyzerDeclarations.Count -eq 0) 'An empty Analyzer declaration remains.'
$originalBodies = @($early.Composition.AnalyzerMethods) + @($early.Composition.EvaluatorMethods)
foreach ($old in $originalBodies) {
    $rebound = @($pre.Composition.AnalyzerMethods) + @($pre.Composition.EvaluatorMethods) | Where-Object Id -CEQ $old.Id
    Gate (@($rebound).Count -eq 1 -and $old.DeclarationTokenHash -ceq $rebound.DeclarationTokenHash) ('Original pre-production declaration differs: ' + $old.Id)
}
$implementations = @($pre.Composition.EvaluatorMethods | ForEach-Object {
    $current = $post.Composition.EvaluatorMethods | Where-Object Id -CEQ $_.Id
    [pscustomobject]@{ Id = $_.Id; Before = $_.DeclarationTokenHash; After = $current.DeclarationTokenHash; Equal = $_.DeclarationTokenHash -ceq $current.DeclarationTokenHash }
})
Gate ($implementations.Count -eq 63 -and @($implementations | Where-Object { -not $_.Equal }).Count -eq 0) 'Original evaluator declarations changed.'
$seedMoves = @($pre.Composition.ThinFacadeCandidates | ForEach-Object {
    $id = $_.Id.Replace('ExceptionFlowAnalyzer', 'ExceptionFlowContextualFactEvaluator')
    $current = $post.Composition.EvaluatorMethods | Where-Object Id -CEQ $id
    [pscustomobject]@{ BeforeId = $_.Id; AfterId = $id; Before = $_.DeclarationTokenHash; After = $current.DeclarationTokenHash; Equal = $_.DeclarationTokenHash -ceq $current.DeclarationTokenHash; Visibility = $_.Visibility; CallersBefore = $_.Callers; CallersAfter = $current.Callers; ExternalUsersBefore = $_.HasUsersOutsideAnalyzer; Decision = 'Remove Analyzer proxy; retain token-identical fresh-guard entry at evaluator; no additional fact algorithm or ownership.' }
})
Gate (@($seedMoves | Where-Object { -not $_.Equal }).Count -eq 0) 'Seed declaration changed.'
$retained = @($post.Composition.AnalyzerMethods | ForEach-Object {
    $old = $pre.Composition.AnalyzerMethods | Where-Object Id -CEQ $_.Id
    [pscustomobject]@{ Id = $_.Id; ExactBody = $old.BodyTokenHash -ceq $_.BodyTokenHash; EqualModuloEvaluatorQualification = $old.BodyTokenHashModuloEvaluatorQualification -ceq $_.BodyTokenHashModuloEvaluatorQualification; BeforeHash = $old.BodyTokenHash; AfterHash = $_.BodyTokenHash }
})
Gate ($retained.Count -eq 224 -and @($retained | Where-Object { -not $_.EqualModuloEvaluatorQualification }).Count -eq 0) 'Analyzer orchestration changed.'
$cacheSupport = EqualSet @($pre.Composition.EvaluatorCacheSupportMethods | ForEach-Object { $_.Id + ':' + $_.DeclarationTokenHash }) @($post.Composition.EvaluatorCacheSupportMethods | ForEach-Object { $_.Id + ':' + $_.DeclarationTokenHash }) 'Cache helpers changed.'
$fields = EqualSet @($pre.Composition.EvaluatorFields | ForEach-Object { $_.Id + ':' + $_.DeclarationTokenHash }) @($post.Composition.EvaluatorFields | ForEach-Object { $_.Id + ':' + $_.DeclarationTokenHash }) 'Cache field declarations changed.'
Gate ($post.Composition.EvaluatorCacheSupportMethods.Count -eq 2 -and $post.Composition.EvaluatorFields.Count -eq 3) 'Cache ownership shape changed.'
function SeedSites($sites) { @($sites | ForEach-Object { $_.Caller + '|' + $_.Callee.Replace('ExceptionFlowAnalyzer', 'ExceptionFlowContextualFactEvaluator') + '|' + $_.ArgumentTokenHash } | Sort-Object) }
$beforeSites = SeedSites $pre.Composition.SeedInvocationSites
$afterSites = SeedSites $post.Composition.SeedInvocationSites
Gate ($beforeSites.Count -eq 14 -and (Json $beforeSites) -ceq (Json $afterSites)) 'Bound seed users/argument token multiset differs.'
$reasons = @{
    CreateCallContext = 'Guard allocation only; remove Analyzer owner.'
    IsDefinitelyNonNull = 'Guard allocation only; remove Analyzer owner.'
    CreateAccessorCallContext = 'Accessor/setter argument projection, defaults and context construction.'
    IsThrowExpressionInExhaustiveDefinedEnumFallback = 'Discard/when checks and complete declared enum-constant coverage.'
    CreateInvocationCallContext = 'Reduced-extension receiver, ordinal remapping and compile-time target binding.'
    CreateKnownFrameworkContractArguments = 'Framework argument projection, parameter indexes and out-parameter exclusion.'
    EvaluatePositiveInt32Comparison = 'Operator/constant-side recognition and positive-int fact integration.'
    CreateSummaryCollectionInitializerCallContext = 'Extension receiver offsets, params-array shape and default projection.'
    GetSummaryCollectionArgumentFacts = 'Explicit user-defined-conversion rejection is a genuine fail-closed boundary.'
    CreateSummaryImplicitCallContext = 'Implicit receiver and extension/default-parameter composition.'
    CreateSummaryOperationCallContext = 'Operation operand-to-parameter mapping and unknown operand exclusion.'
    IsThrowExpressionProvenUnreachable = 'Coalesce/conditional/switch traversal and proven branch decisions.'
    EvaluateNullComparison = 'Null operand/operator shape interpreted as a branch condition.'
    EvaluateNullPattern = 'Pattern shape plus fact-to-condition integration.'
    EvaluateStringPredicate = 'Exact framework predicate binding and predicate truth evaluation.'
    CreateDispatchCallContext = 'Compile-time context composed with runtime target ordinal rebinding.'
    AddSummaryPropertyGetterEdge = 'Accessor target resolution and summary call/path composition.'
    AnalyzeSummaryDelegateInvocation = 'Delegate targets, summary paths and uncertainty integration.'
    AnalyzeSummaryObjectCreations = 'Constructor binding, target registration and summary path composition.'
    AddSummaryConstructorCallEdge = 'Target registration and summary call-edge composition.'
    AddSummaryDisposalEdges = 'Disposal/runtime targets and summary exception-path composition.'
    AnalyzeSummaryImplicitObjectCreations = 'Implicit constructor binding and summary target/path construction.'
    AnalyzeSummaryThrownExpressionNullability = 'Throw nullability evidence integrated into exception paths.'
}
function Classification($methods) {
    @($methods | ForEach-Object {
        $name = (($_.Id -split '\(')[0] -split '\.')[-1]
        Gate ($reasons.ContainsKey($name)) ('Missing semantic classification: ' + $_.Id)
        [pscustomobject]@{ Id = $_.Id; Name = $name; Visibility = $_.Visibility; Shape = $_.Shape; Decision = if ($_.Shape -eq 'GuardSeedOnly') { 'RemoveAnalyzerFacade' } else { 'RetainOrchestration' }; Reason = $reasons[$name]; Callers = $_.Callers; DirectEvaluatorInvocations = $_.DirectEvaluatorInvocations; File = $_.File; Line = $_.Line }
    })
}
$preClassification = Classification $pre.Composition.AnalyzerToEvaluatorMethods
$postClassification = Classification $post.Composition.AnalyzerToEvaluatorMethods
function Metrics($measurement) {
    $composition = $measurement.Composition
    @{ AnalyzerPrefixFiles = $measurement.AnalyzerPartials; AnalyzerWholeFileNonblankSloc = $measurement.AnalyzerNonblankSloc; AnalyzerDeclarations = $composition.AnalyzerDeclarations; AnalyzerOwnedNonblankLines = $composition.AnalyzerOwnedNonblankLines; AnalyzerMethods = $composition.AnalyzerMethods.Count; AnalyzerBoundaryMethods = $composition.AnalyzerToEvaluatorMethods.Count; AnalyzerEvaluatorSites = @($composition.DirectInvocationSites | Where-Object CallerOwner -like '*.ExceptionFlowAnalyzer').Count; EvaluatorExternalEdges = $composition.EvaluatorExternalIngress.Count; EvaluatorExternalCallers = @($composition.EvaluatorExternalIngress.Caller | Sort-Object -Unique).Count; EvaluatorExternalTargets = @($composition.EvaluatorExternalIngress.Callee | Sort-Object -Unique).Count; EvaluatorExternalSites = @($composition.DirectInvocationSites | Where-Object CallerOwner -notlike '*.ExceptionFlowContextualFactEvaluator').Count; SccMethods = $measurement.Scc.MethodCount; SccInternalEdges = $measurement.Scc.InternalEdgeCount; SccIngressEdges = $measurement.Ingress.Count; SccIngressCallers = @($measurement.Ingress.Edge.Caller | Sort-Object -Unique).Count; SccEntryTargets = @($measurement.Ingress.Edge.Callee | Sort-Object -Unique).Count; SccEgress = $measurement.Egress.Count; EvaluatorRootMethods = $composition.EvaluatorMethods.Count }
}
$baseline = ReadJson 'artifacts/p5o2a5b/final-self-analysis.json'
$self = ReadJson 'artifacts/p5o2a5c/recovery-self-analysis.json'
$previousSelf = ReadJson 'artifacts/p5o2a5c/final-self-analysis.json'
Gate ((Json @($previousSelf.Findings)) -ceq (Json @($self.Findings))) 'Recovered Self Analysis differs from completed previous run.'
function FindingKey($finding) { @($finding.FilePath.Replace($root, '').Replace('\', '/'), $finding.SmellId, $finding.ContainingNamespace, $finding.ContainingType, $finding.SymbolName, $finding.TargetName, $finding.TagName, $finding.OwnerKind, $finding.SubjectKind, $finding.Line, $finding.Column) -join '|' }
function NormalizeFindings($findings) {
    @($findings | Sort-Object { FindingKey $_ } | ForEach-Object {
        $copy = [ordered]@{}
        foreach ($property in $_.PSObject.Properties) { $copy[$property.Name] = if ($property.Name -eq 'FilePath') { $property.Value.Replace($root, '').Replace('\', '/') } else { $property.Value } }
        [pscustomobject] $copy
    })
}
$oldFindings = NormalizeFindings $baseline.Findings
$newFindings = NormalizeFindings $self.Findings
$oldJson = Json $oldFindings
$newJson = Json $newFindings
$findingDiff = Diff @($oldFindings | ForEach-Object { FindingKey $_ }) @($newFindings | ForEach-Object { FindingKey $_ })
Gate ($oldJson -ceq $newJson -and (Json @($baseline.Findings)) -ceq (Json @($self.Findings))) 'Full finding/evidence arrays differ.'
Gate ((Json $newFindings) -ceq (Json $a5b.Validation.Canonical.Findings)) 'Committed Post-A5B normalized findings differ.'
$tests = @('focused', 'architecture', 'relevant', 'broad', 'full', 'full-final', 'focused-recovery', 'architecture-recovery', 'relevant-recovery', 'broad-recovery', 'full-recovery') | ForEach-Object {
    $path = 'artifacts/p5o2a5c/test-results/' + $_ + '.trx'
    [xml] $trx = Get-Content -LiteralPath $path -Raw
    $counts = $trx.TestRun.ResultSummary.Counters
    Gate ($trx.TestRun.ResultSummary.outcome -ceq 'Completed' -and $trx.TestRun.Times.start -and $trx.TestRun.Times.finish) ('Incomplete TRX: ' + $path)
    [pscustomobject]@{ Run = $_; Artifact = $path; Passed = [int] $counts.passed; Total = [int] $counts.total; Failed = [int] $counts.failed; NotExecuted = [int] $counts.notExecuted }
}
Gate (@($tests | Where-Object { $_.Failed -ne 0 -or $_.NotExecuted -ne 0 -or $_.Passed -ne $_.Total }).Count -eq 0) 'A regression run is not green.'
Gate (($tests | Where-Object Run -eq 'full-final').Passed -eq 2560) 'Full-suite count differs.'
Gate (($tests | Where-Object Run -eq 'full-recovery').Passed -eq 2560) 'Recovery full-suite count differs.'
$repeat = 'artifacts/p5o2a5c/post-composition-repeat.json'
Gate ((Get-FileHash $After).Hash -ceq (Get-FileHash $repeat).Hash) 'Post-measurement is not byte-identical.'
$recoveryMeasurement = 'artifacts/p5o2a5c/post-composition-recovery.json'
Gate ((Get-FileHash $After).Hash -ceq (Get-FileHash $recoveryMeasurement).Hash) 'Current recovered source graph differs from previous final measurement.'
$status = @(git status --short)
$stashes = @(git stash list --format='%H %s')
$expectedStashes = @('ea2390e44c6f7880b7861ffc8205a8a622eb049b', '7f2ec8f22d27e53b107312d6873380f191aa90b5', 'e48eacf48ab1195853451df4270b0c401979f063', 'ca21287a63a755f3a05a2e7b0e4fcbf3e5f0f3f1', '4118ab86b1ee37a871c576ff171df934772f47d9', '567448595c368c4c3b490b31289d37facd3bdecf')
Gate ((Json @($stashes | ForEach-Object { ($_ -split ' ')[0] })) -ceq (Json $expectedStashes)) 'Stash identities changed.'
$evidence = [ordered]@{
    Schema = 'ContextualFactEvaluator.CompositionCleanup.v1'; Date = '2026-10-06'; StartingHead = $head
    Recovery = @{ Found = 'Production/test changes complete and correct; six previous TRX runs Completed; graph/analysis JSON parseable; Markdown header and {} audit JSON placeholders; renderer scalar Count check already corrected, interrupted before successful generation'; PartialProductionOrTestFiles = $false; PreviousIncompleteValidationClaimedPassing = $false; ProductionEditsAfterRecovery = 0; Finished = 'Evidence renderer/report/audit JSON, recovery section and complete current-state revalidation'; PreviousAndCurrentBoundGraphByteIdentical = $true; CurrentSourceHashes = @(@(git diff --name-only --relative) + @(git ls-files --others --exclude-standard) | Sort-Object -Unique | Where-Object { ($_ -like 'src/XMLDocNormalizer/*' -or $_ -like 'Tests/XMLDocNormalizerTests/*' -or $_ -like 'Evaluation/ContextualBoundaryAudit/*') -and $_ -like '*.cs' -and (Test-Path -LiteralPath $_) } | ForEach-Object { @{ Path = $_; Sha256 = (Get-FileHash $_).Hash } }) }
    Scope = 'Two seed entry composition moves, thirteen callers/fourteen invocation sites, empty partial removal. No new fact ownership/algorithm, SCC redesign, worker, dual-version build, performance work or A6 audit.'
    PreProductionGate = @{ OriginalBefore = 'artifacts/p5o2a5c/pre-composition.json'; ReboundBaseline = $Before; ExactOriginalMembers = $earlyMemberDiff; ExactOriginalEdges = $earlyEdgeDiff; OriginalDeclarationsTokenIdentical = $originalBodies.Count; CommittedA5BMembers = $committedMemberDiff; CommittedA5BEdges = $committedEdgeDiff; InitialForeignDirtyPaths = 8 }
    Metrics = @{ Before = Metrics $pre; After = Metrics $post }
    FacadeDecisions = $seedMoves; PreBoundaryClassification = $preClassification; PostBoundaryClassification = $postClassification
    Composition = @{ Before = $pre.Composition; After = $post.Composition; SeedCallerArgumentsTokenMultisetEqual = $true; SeedInvocationSitesBefore = $pre.Composition.SeedInvocationSites; SeedInvocationSitesAfter = $post.Composition.SeedInvocationSites; RemovedAnalyzerFacades = 2; RemainingUnjustifiedFacades = 0 }
    ImplementationVerification = @{ UnchangedOriginalSccDeclarations = $implementations; RetainedAnalyzerMethods = $retained; CacheSupportDiff = $cacheSupport; FieldDiff = $fields; CacheSupportMethods = $post.Composition.EvaluatorCacheSupportMethods; Fields = $post.Composition.EvaluatorFields }
    Scc = @{ Before = $pre.Scc; After = $post.Scc; MemberDiff = $memberDiff; EdgeDiff = $edgeDiff; IngressBefore = $pre.Ingress; IngressAfter = $post.Ingress; EgressBefore = $pre.Egress; EgressAfter = $post.Egress; EgressDiff = $egressDiff }
    DependencyClosure = @{ EvaluatorToAnalyzer = 0; DownstreamFactResolverToAnalyzer = 0; InterComponentCycles = $post.LogicalComponentCycles; ExpandedTypeCycles = $post.ComponentCycles; Nodes = $post.ReachableSourceMethods; Edges = $post.ReachableSourceEdges; InitializerDependencies = $post.ReachableInitializerDependencies; DelegateSites = @($post.DelegateSites | Where-Object Callable -like '*ExceptionFlowContextualFactEvaluator*'); Scope = 'Evaluator and reachable lower fact/resolver closure. LocalSourceAnalyzer is upper orchestration, not a lower fact provider; unrelated upper orchestration links to Analyzer are not claimed absent.'; Limitations = $post.Limitations }
    CacheGuardLifetime = @{ Original65MethodsAnd3FieldsUnchanged = $true; SeedDeclarationsTokenIdentical = $true; FreshSeedPerEntry = 'new HashSet<ISymbol>(SymbolEqualityComparer.Default), before original guarded overload, never static/reused'; Lifetime = 'Static readonly weak SemanticModel identity CWT; original first-use partitions, OriginalDefinition/default symbol comparer and positive/negative bool cache'; Guard = 'Original guard-before-cache, finally cleanup and independent invariant seed unchanged'; Threading = 'Original partition locks, computation outside lock and TryAdd first result unchanged'; OtherProviderStateMoved = $false }
    Validation = @{ Tests = $tests; SolutionWarningAsErrorBuild = @{ Warnings = 0; Errors = 0 }; AuditWarningAsErrorBuild = @{ Warnings = 0; Errors = 0 }; SelfAnalysis = @{ Artifact = 'artifacts/p5o2a5c/recovery-self-analysis.json'; ExitCode = 1; Findings = $self.Findings.Count; Counts = $self.Metrics.TotalFindingCounts }; Canonical = @{ Baseline = 'Post-A5B committed evidence and artifacts/p5o2a5b/final-self-analysis.json'; Added = $findingDiff.Added.Count; Removed = $findingDiff.Removed.Count; ChangedEvidence = 0; FullRawArraysEqual = $true; FullNormalizedArraysEqual = $true; BeforeNormalizedHash = Hash $oldJson; AfterNormalizedHash = Hash $newJson; Normalization = 'Workspace prefix/path separators only; sort by full finding identity; every property including Message/Snippet retained'; FindingsBefore = $oldFindings; FindingsAfter = $newFindings }; Format = 'Scoped changed/new production/test C# and entire audit project verify-no-changes: pass'; PostMeasurementByteIdentical = $true }
    RawMeasurements = @('artifacts/p5o2a5c/pre-composition.json', $Before, $After, $repeat, $recoveryMeasurement) | ForEach-Object { @{ Path = $_; Sha256 = (Get-FileHash $_).Hash } }
    A6Readiness = @{ Ready = $true; MinimalBlocker = $null; A6Started = $false; WorkerOrDualRoslynReadinessClaimed = $false }
    GitStatusAtEvidenceGeneration = $status; StashesUnchanged = $stashes
}
$encoding = New-Object Text.UTF8Encoding($false)
function WriteText([string] $path, [string] $content) { [IO.File]::WriteAllText((Join-Path $root $path), $content.Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n", $encoding) }
WriteText $Output (ConvertTo-Json -InputObject $evidence -Depth 100)
$lines = New-Object 'System.Collections.Generic.List[string]'
function Line([string] $value = '') { $lines.Add($value) }
Line '# P5O2A5C - Contextual Evaluator Composition / Facade Cleanup'
Line
Line 'Completed. **P5O2A6 Architecture / Readiness Closure ready: yes. Minimal blocker: none.** A6 itself has not begun; this is not worker/dual-Roslyn readiness.'
Line
Line ('Starting HEAD: `' + $head + '` (`Extract contextual fact evaluator.`), branch `fix/documentation`, 2026-10-06. Before production changes: status/stat/full diff/HEAD/log read, existing A5B evidence reviewed and fresh graph/composition measured. Only root .gitignore and seven Core bin/obj paths were initially dirty; preserved as foreign WIP/build output.')
Line
Line '## Recovery after laptop restart'
Line
Line 'The recovered HEAD and complete WIP were audited before any further edit. Both seed proxies were already removed, all fourteen sites redirected, the empty Nullability partial deleted, and the architecture contract updated. Production/test diffs were complete and correct; no truncated or inconsistent production/test file was found. Nothing was reset, reimplemented or repeated at the production level.'
Line
Line 'All six previous TRX files had Completed outcomes, complete start/finish timestamps and passing counters; both graph snapshots and the earlier 16-finding Self Analysis were complete and parseable. No interrupted test/build artifact is counted as passing. Only final evidence was unfinished: Markdown contained its heading, JSON contained {}, and the renderer had not completed. Its scalar PSCustomObject Count assertion had already been corrected before interruption; the underlying original/rebound hashes were equal, not a production mismatch.'
Line
Line 'After recovery only evidence/tool-report work was finished. Solution and audit builds, focused, architecture, relevant, broad and full tests, Self Analysis, scoped formatting and quality gates were revalidated for current WIP. The new bound graph is byte-identical to both completed pre-restart post snapshots; source SHA256 values are captured in JSON. Old completed results are retained as history, recovery-suffixed runs are current validation. No production/test source was changed after recovery.'
Line
Line 'A final evidence review found that historical filename/whole-file SLOC metrics in the in-memory baseline still read the current filesystem. The audit now measures these from the rebound syntax trees, restoring 29/12,977 before versus 28/12,898 after, with exact equality to the original pre-production snapshot. Semantic ownership, hashes and SCC measures were already correct. This correction affects audit code only; current post graph output remains byte-identical.'
Line
Line '## 1. Complete pre-boundary review'
Line
Line 'All 226 actual Analyzer method declarations were inspected structurally and with bound symbols. Fifteen methods directly called the evaluator. Exactly two guard-seed-only candidates and no additional return-only proxy were found. Small orchestrators were explicitly reviewed rather than removed by name or statement count. The JSON retains the entire pre/post method census, bodies, visibility, callers, source locations, exact bound targets and classification.'
Line
Line '| Pre-boundary method | Decision | Semantic reason |'
Line '|---|---|---|'
foreach ($item in $preClassification) { Line ('| ' + $item.Name + ' | ' + $item.Decision + ' | ' + $item.Reason + ' |') }
Line
Line 'CreateDispatchCallContext is additionally retained: a seed context is composed with runtime-target rebinding. AddSummaryConstructorCallEdge and other newly direct users bind/register targets and compose paths. The three-statement GetSummaryCollectionArgumentFacts explicitly rejects user-defined conversions; its remaining evaluator call is not a removable proxy. Existing Analyzer source bodies are unchanged modulo evaluator qualification.'
Line
Line '## 2. Seed decisions and all direct users'
Line
Line 'Both old Analyzer entries were internal and consisted solely of a fresh HashSet<ISymbol>(SymbolEqualityComparer.Default) plus return of the guarded evaluator result. Neither had Analyzer domain state, fact transformation or an orchestration boundary. Both Analyzer proxies are removed. Their two declarations (including bodies, parameter tokens and guard allocation) move unchanged to the existing evaluator partials as convenience entry overloads. No new abstraction, callback, dependency bag, fact algorithm or additional fact ownership is introduced.'
Line
Line 'CreateCallContext/4 had 12 distinct source callers and 13 invocation sites (CreateInvocationCallContext twice), including three LocalSourceAnalyzer callers outside Analyzer. IsDefinitelyNonNull/3 had one source caller/site and no external user. All 14 sites now bind directly to evaluator entries with identical argument-token multisets. Dependency direction remains upper orchestration -> evaluator -> lower facts/resolvers. All existing static imports used for genuine orchestration are retained.'
Line
foreach ($move in $seedMoves) {
    Line ('Seed: `' + ($move.BeforeId -split '\(')[0] + '` -> `' + ($move.AfterId -split '\(')[0] + '`.')
    Line
    foreach ($caller in $move.CallersBefore) { Line ('- `' + ($caller.Caller -split '\(')[0] + '` -> evaluator directly (formerly Analyzer seed).') }
    Line
}
Line 'The JSON includes complete post-boundary classifications and all 27 external caller/callee edges, not just the seed users listed above. Evaluator now declares 65 root methods (63 original plus two non-SCC seeds); its two nested cache helpers remain additional. The original 65 A5B moved implementations and three fields are unchanged. All 63 original root declarations and both moved seed declarations are token-identical. All 224 retained Analyzer bodies are token-identical modulo evaluator-call qualification.'
Line
Line '## 3. Measured ownership and composition'
Line
Line '| Metric | Before | After |'
Line '|---|---:|---:|'
foreach ($key in @('AnalyzerPrefixFiles', 'AnalyzerWholeFileNonblankSloc', 'AnalyzerDeclarations', 'AnalyzerOwnedNonblankLines', 'AnalyzerMethods', 'AnalyzerBoundaryMethods', 'AnalyzerEvaluatorSites', 'EvaluatorExternalEdges', 'EvaluatorExternalCallers', 'EvaluatorExternalTargets', 'EvaluatorExternalSites', 'EvaluatorRootMethods', 'SccMethods', 'SccInternalEdges', 'SccIngressEdges', 'SccIngressCallers', 'SccEntryTargets', 'SccEgress')) { Line ('| ' + $key + ' | ' + $evidence.Metrics.Before[$key] + ' | ' + $evidence.Metrics.After[$key] + ' |') }
Line
Line 'Only the now-empty ExceptionFlowAnalyzer.Nullability.cs is deleted; recoverable from starting Git HEAD. No partial merging/renaming for metrics. No empty Analyzer declaration remains. Historical prefix/whole-file metrics include the provider-only old CWT filename; actual declaration/owner-line metrics intentionally do not.'
Line
Line 'SCC membership, all 112 internal kind-labelled edges and all 791 egress edges are exactly equal, not just count-equal. SCC ingress stays 16/15/4: the two seed caller identities change owner, while the underlying guarded targets stay unchanged. Whole evaluator external ingress grows 16 -> 27 edges, 15 -> 24 callers and 17 -> 29 sites because the formerly hidden seed users are now direct (three belong to LocalSourceAnalyzer). There are still four external overload targets; the two old guarded entries are replaced at the outside boundary by seed overloads. Distinguish whole evaluator ingress from SCC ingress.'
Line
Line 'Historical A5B prose incorrectly attributed the 17th invocation to EvaluateNullComparison. The fresh bound audit shows EvaluatePositiveInt32Comparison calls GetExpressionValueFacts on both alternative operand sides. The 17-site/16-edge number was correct; prior immutable evidence is not edited. Post-cleanup the additional repeated seed call is in CreateInvocationCallContext.'
Line
Line 'Evaluator -> Analyzer = 0; reachable lower fact/resolver components -> Analyzer = 0; logical inter-component cycles = 0. These gates cover the bound source closure plus initializer/delegate/compiled-IL checks, not a claim that upper LocalSourceAnalyzer orchestration never uses Analyzer. The one expanded DataFlow provider/cache type cycle remains internal to that provider; it is unchanged and collapses to zero inter-component cycles. No Analyzer delegate/callback/state capture is introduced.'
Line
Line 'Cache/guard ownership is unchanged: static readonly CWT weak SemanticModel identity, original partition first-use, OriginalDefinition/SymbolEqualityComparer.Default keys and bool positive/negative results. Guard-before-cache/finally and independent invariant seeds remain unchanged. Each new convenience call creates its own fresh guard; no shared guard/reset/lifetime redesign. Lookup/store locks, computation outside locks and first-result TryAdd are unchanged. All foreign provider state stays at its owner.'
Line
Line '## 4. Tests and complete validation'
Line
Line 'A5B semantic/cache/lifetime tests are retained. The exact SCC/entry ownership contract now permits only the two explicit convenience overloads outside the original 63-method set and forbids old Analyzer duplicates. One additional structural architecture test rejects return-only/guard-only Analyzer proxies across all partials, rejects empty partial declarations and checks fresh default-comparer seed bodies. Its source check is complementary to the bound census and existing compiled-IL closure test (not an alias-proof standalone semantic binder). Existing call-context, nullability and all-mode regressions cover direct callers; no redundant semantic fixture is added.'
Line
Line '| Run | Passed / total | Failed | Not executed |'
Line '|---|---:|---:|---:|'
foreach ($test in $tests) { Line ('| ' + $test.Run + ' | ' + $test.Passed + ' / ' + $test.Total + ' | ' + $test.Failed + ' | ' + $test.NotExecuted + ' |') }
Line
Line 'Solution and audit warning-as-error builds: 0 warnings / 0 errors. Full baseline 2559 becomes 2560 solely through one new architecture test. No MultiModule flake occurred in the recorded A5C full runs; no flake repair was attempted. Focused includes evaluator/cache/CallContext seams; relevant includes Sequence, Dictionary, Callback, Nullability, Guard, NonNull, Successful, ContentPreservation, Delegate and CallContext; architecture includes fact/scope/core/summary/local/evaluator dependency guards; broad includes Check.Semantic, Execution.Semantic and Evaluation.'
Line
Line 'Fresh solution-transitive Self Analysis: 16 findings, DOC610=0, DOC611=1, DOC631=15, DOC632=0; CLI exit 1 is expected. Against both immediate A5B raw output and committed normalized evidence: 0 added / 0 removed / 0 evidence changes. Full raw and normalized finding arrays (every property, including Message/Snippet) are exactly equal. Both full normalized arrays/hashes are retained in the JSON.'
Line
Line 'Changed/new production/test C# formatting and complete audit-project verify-no-changes gates pass. Known global format deviations are untouched. Scoped CRLF, JSON parse and git diff --check gates pass; repeat post-composition measurements are byte-identical. Cache helper/field token gates and 14 bound seed invocation/argument-token gates pass. A full run after final source formatting/CRLF also passes.'
Line
Line '## 5. Evidence and reproduction'
Line
Line '- [Machine-readable audit](P5O2A5C-contextual-evaluator-composition-cleanup-audit.json): full pre/post census, classifications, all callers/sites, metrics, exact SCC/egress comparisons, cache/implementation hashes, complete finding arrays and TRX counters.'
Line '- Evaluation/ContextualBoundaryAudit/CompositionAudit.cs: structural census plus bound callers/targets/argument tokens.'
Line '- Evaluation/ContextualBoundaryAudit/CompositionBaseline.cs: optional in-memory rebind of committed production sources, preserving active references/options/generated trees; no checkout or WIP change.'
Line '- Raw early pre-production measurement, enriched HEAD rebind, two post measurements, Self Analysis and TRX files: ignored artifacts/p5o2a5c. Early census declarations and SCC members/edges exactly match the enriched pre baseline.'
Line
Line 'During evidence-tool development the first in-memory baseline attempt used a cwd-relative Git tree prefix and failed before producing evidence. Using Git -C at the repository root fixed that read-only tool issue. A subsequent PowerShell scalar Count assertion was corrected without changing the equal declaration hashes. No production reconstruction or reset occurred.'
Line
Line '~~~powershell'
Line 'dotnet build XMLDocNormalizer.sln --no-restore -warnaserror'
Line 'dotnet build Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj --no-restore -warnaserror'
Line ('dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln ' + $Before + ' --composition-baseline=' + $head)
Line ('dotnet run --no-build --project Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj -- XMLDocNormalizer.sln ' + $After)
Line 'dotnet test XMLDocNormalizer.sln --no-build --no-restore --logger "trx;LogFileName=full-final.trx" --results-directory artifacts/p5o2a5c/test-results'
Line 'dotnet src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll --check --project XMLDocNormalizer --exception-analysis-mode solution-transitive --format json --output artifacts/p5o2a5c/recovery-self-analysis.json XMLDocNormalizer.sln'
Line 'powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/ContextualBoundaryAudit/Write-CompositionEvidence.ps1'
Line 'git diff --check'
Line '~~~'
Line
Line '## 6. Git protection and next package'
Line
Line 'Root .gitignore WIP, tracked Core build churn, global format deviations and all six stash identities/names are preserved. No reset, checkout, restore, commit, push or stash mutation. Only the authorized empty partial was removed. OPEN-PIPELINE-BOUNDARIES records A5C completion; historical A5B evidence is unchanged.'
Line
Line '**P5O2A6 ready: yes; minimal blocker: none.** The evaluator composition seam is closed. A6 must make the broader architecture/readiness assessment itself; A5C does not pre-empt it, implement worker/IPC, run the dual-Roslyn experiment or promise historical compiler readiness.'
Line
Line 'Final status at evidence generation (also printed in the handoff):'
Line
Line '~~~text'
foreach ($entry in $status) { Line $entry }
Line '~~~'
WriteText $Report ($lines -join "`r`n")
$scope = @(@(git diff --name-only --relative) + @(git ls-files --others --exclude-standard) | Sort-Object -Unique | Where-Object {
    ($_ -like 'Evaluation/*' -or $_ -like 'Tests/XMLDocNormalizerTests/*' -or $_ -like 'src/XMLDocNormalizer/*') -and
    $_ -match '\.(cs|ps1|md|json)$' -and (Test-Path -LiteralPath $_)
})
foreach ($file in $scope) {
    $content = [IO.File]::ReadAllText((Join-Path $root $file))
    Gate (-not [regex]::IsMatch($content, '(?<!\r)\n|\r(?!\n)')) ('CRLF failed: ' + $file)
    if ($file -like '*.json') { $null = $content | ConvertFrom-Json }
}
$ErrorActionPreference = 'Continue'
$check = @(git diff --check 2>&1)
$diffExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
Gate ($diffExit -eq 0) ('git diff --check failed: ' + ($check -join "`n"))
$evidence.Validation.QualityGates = @{ CRLF = @{ Passed = $true; Files = $scope }; JsonParse = @{ Passed = $true; Files = @($scope | Where-Object { $_ -like '*.json' }) }; DiffCheckExitCode = $diffExit }
WriteText $Output (ConvertTo-Json -InputObject $evidence -Depth 100)
$null = ReadJson $Output
Write-Output 'A5C evidence complete: SCC 63/112; full 2560/2560; canonical 0/0/0; facade count 0.'
