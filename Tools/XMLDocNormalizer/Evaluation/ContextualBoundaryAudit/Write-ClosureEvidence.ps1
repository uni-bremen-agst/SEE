param(
    [string] $Measurement = 'artifacts/p5o2a6/architecture-final.json',
    [string] $Repeat = 'artifacts/p5o2a6/architecture-repeat.json',
    [string] $Output = 'Evaluation/P5O2A6-architecture-readiness-closure-audit.json',
    [string] $Matrix = 'Evaluation/P5O2A6-historical-core-readiness-matrix.md'
)
$ErrorActionPreference = 'Stop'
function Gate([bool] $condition, [string] $message) { if (-not $condition) { throw $message } }
function ReadJson([string] $path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($value) { ConvertTo-Json -InputObject $value -Depth 100 -Compress }
function Equal($left, $right, [string] $message) { Gate ((Json $left) -ceq (Json $right)) $message }
function WriteUtf8([string] $path, [string] $content) {
    [IO.File]::WriteAllText((Join-Path (Get-Location) $path),
        $content.Replace("`r`n", "`n").Replace("`n", "`r`n"), [Text.UTF8Encoding]::new($false))
}
$head = (git rev-parse HEAD).Trim()
Gate ($head -ceq '4ee61abee61b7677857100aa46173977d64283f0') 'Audited HEAD changed.'
git diff --quiet HEAD -- src/XMLDocNormalizer Tests
Gate ($LASTEXITCODE -eq 0) 'Production or test source differs from audited HEAD.'
Gate (@(git ls-files --others --exclude-standard -- src/XMLDocNormalizer Tests).Count -eq 0) 'Untracked production or test source.'
$a = ReadJson $Measurement
$base = ReadJson 'Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup-audit.json'
$rawBase = 'artifacts/p5o2a5c/post-composition-recovery.json'
Gate ((Get-FileHash $rawBase).Hash -ceq '00C8C21283293BDAB78DCC3838E37627A0C8C2D24FFF9EBEF05C8CE91E32BC59') 'A5C raw graph evidence changed.'
$old = ReadJson $rawBase
$rawHash = (Get-FileHash $Measurement).Hash
Gate ($rawHash -ceq (Get-FileHash $Repeat).Hash) 'Repeated A6 measurement differs.'
foreach ($property in @('Composition','Scc','Ingress','Egress','AllSourceNodes',
    'AllStateAccesses','InitializerDependencies','DelegateSites','LogicalComponentEdges')) {
    Equal $old.$property $a.$property ('A5C bound graph/declaration evidence differs: ' + $property)
}
$validatedHashes = @($base.Recovery.CurrentSourceHashes | Where-Object { $_.Path -like 'src/*' -or $_.Path -like 'Tests/*' } | ForEach-Object {
    $current = (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash
    Gate ($current -ceq $_.Sha256) ('A5C validated source changed: ' + $_.Path)
    [pscustomobject]@{ File = $_.Path; A5C = $_.Sha256; Current = $current; Equal = $true }
})
$g = $a.ArchitectureClosure
$hostCutFile = $g.HostImplementationCut
$core = @($g.ProposedCoreOwners)
$flow = 'XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.'
$analyzer = $flow + 'ExceptionFlowAnalyzer'
$blocked = @($analyzer, ($flow + 'ExceptionFlowSummaryAnalysisSession'), ($flow + 'ExceptionFlowSummaryGraphBuilder'))
Gate (@($a.CompilationErrors).Count -eq 0 -and $a.Scc.MethodCount -eq 63 -and $a.Scc.InternalEdgeCount -eq 112) 'Compilation/SCC gate.'
Gate (@($g.LowerToAnalyzerTypeUses).Count -eq 0 -and @($a.ReachableAnalyzerOutsideScc).Count -eq 0) 'Lower ownership gate.'
Gate (@($a.Composition.ThinFacadeCandidates).Count -eq 0) 'Evaluator facade gate.'
Gate (@($g.ProposedCoreTypeCycles).Count -eq 3 -and @($g.MethodOwnerCycles).Count -eq 2) 'Whole architecture cycle census changed.'
Gate (@($a.Composition.EvaluatorCacheSupportMethods).Count -eq 2 -and @($a.Composition.EvaluatorFields).Count -eq 3) 'Cache ownership gate.'
function TopOwner([string] $owner) {
    @($core | Where-Object { $owner -ceq $_ -or $owner.StartsWith($_ + '.', [StringComparison]::Ordinal) } | Sort-Object Length -Descending | Select-Object -First 1)[0]
}
$nodeOwners = @{}
foreach ($node in $a.AllSourceNodes) { $nodeOwners[$node.Id] = TopOwner $node.Owner }
$coreNodes = @($a.AllSourceNodes | Where-Object { $nodeOwners.ContainsKey($_.Id) -and $nodeOwners[$_.Id] -and $_.File -ne $hostCutFile })
$coreIds = @{}; foreach ($node in $coreNodes) { $coreIds[$node.Id] = $true }
$coreCalls = @($g.MethodEdges | Where-Object { $coreIds.ContainsKey($_.Caller) })
$blockEdges = @($coreCalls | Where-Object { $_.Source -and $blocked -contains $nodeOwners[$_.Caller] -and $blocked -contains $nodeOwners[$_.Callee] -and $nodeOwners[$_.Caller] -cne $nodeOwners[$_.Callee] })
Gate ($blockEdges.Count -eq 12) 'Orchestration blocker edge census changed.'
$adjacency = @{}
foreach ($edge in $g.ProposedCoreTypeEdges) { $adjacency[$edge.Caller] = @($adjacency[$edge.Caller]) + $edge.Callee }
function Reach([string] $start) {
    $seen = @{}; $pending = [Collections.Generic.Queue[string]]::new(); $pending.Enqueue($start)
    while ($pending.Count -gt 0) {
        $item = $pending.Dequeue()
        if ($seen.ContainsKey($item)) { continue }
        $seen[$item] = $true
        foreach ($target in @($adjacency[$item])) { if ($target) { $pending.Enqueue($target) } }
    }
    @($seen.Keys | Sort-Object)
}
$lowerReach = @($g.LowerFactResolverOwners | ForEach-Object {
    $reachable = @(Reach $_)
    Gate ($reachable -cnotcontains $analyzer) ('Indirect Analyzer dependency: ' + $_)
    [pscustomobject]@{ Owner = $_; ReachableOwners = $reachable; AnalyzerReachable = $false }
})
function Category([string] $owner) {
    if ($blocked -contains $owner) { return 'E' }
    if ($owner -like '*Flow.Canonical.Canonical*' -or $owner -like '*Flow.Canonical.NamespaceDoc' -or
        $owner -like 'XMLDocNormalizer.Models.ExceptionFlowPath*' -or $owner -like '*Models.ExceptionFlowSourceKind' -or
        $owner -like '*Models.ExceptionFlowDetails') { return 'D' }
    if ($owner -notlike ($flow + '*') -or $owner -match 'ExceptionFlow(ValueFacts|ValueFactsExtensions|SemanticScope|SemanticEnvironment)$') { return 'B' }
    return 'A'
}
$components = @($core | ForEach-Object {
    $owner = $_
    $uses = @($g.TypeUses | Where-Object { $_.Caller -ceq $owner -and $_.File -ne $hostCutFile })
    [pscustomobject]@{
        Owner = $owner; Category = Category $owner; IntendedCategory = $(if ($blocked -contains $owner) { 'A' } else { Category $owner })
        DirectRoslynUse = @($uses | Where-Object Namespace -like 'Microsoft.CodeAnalysis*').Count -gt 0
        Files = @($g.TypeDeclarations | Where-Object { $_.Owner -ceq $owner -and $_.File -ne $hostCutFile } | Select-Object -ExpandProperty File -Unique | Sort-Object)
        SourceDependencies = @($uses | Where-Object { $_.Source -and $_.Callee -cne $owner } | Select-Object -ExpandProperty Callee -Unique | Sort-Object)
        ClosureBlocker = $blocked -contains $owner
        HostRequirement = $(if ($owner -like '*ExceptionFlowSemanticEnvironment') { 'Compile a same-source nonvirtual historical capability host; exclude active ProjectClosure host partial.' } else { 'No Main assembly reference; compile source locally in its Roslyn universe.' })
    }
})
$sourceFiles = @($components.Files | Sort-Object -Unique | ForEach-Object {
    $file = $_; $owners = @($components | Where-Object { $_.Files -contains $file })
    [pscustomobject]@{ File = $file; Categories = @($owners.Category | Sort-Object -Unique); Owners = @($owners.Owner)
        FileHasRoslynUse = @($g.RoslynUses | Where-Object File -CEQ $file).Count -gt 0
        Sha256 = ($g.SourceHashes | Where-Object File -CEQ $file).Sha256 }
})
Gate ($core.Count -eq 100 -and $sourceFiles.Count -eq 117) 'Source closure census changed.'
Gate (@($g.ProposedCoreTypeEdges | Where-Object { $core -cnotcontains $_.Callee }).Count -eq 0) 'Unexpected Main source dependency outside explicit host cut.'
$mainApiUses = @($g.TypeUses | Where-Object { $core -contains $_.Caller -and $_.File -cne $hostCutFile -and
    ($_.Callee -match '^Microsoft.Build\.' -or $_.Callee -match '^Microsoft.CodeAnalysis\.(Workspace|Solution|Project|Document)$') })
Gate ($mainApiUses.Count -eq 0) 'Main workspace/MSBuild API leaked into proposed core.'
$mainOwners = @($g.TypeDeclarations | Where-Object { $core -cnotcontains $_.Owner } | Select-Object Owner,File -Unique | Sort-Object Owner,File)
$responsibility = @{
    CallContext = 'Root/accessor argument and parameter-context composition; defaults and setter projection, not downstream fact discovery.'
    InvocationCallContext = 'Invocation binding, reduced-extension receiver/ordinal mapping and caller context composition.'
    EnumSwitchReachability = 'Exhaustive enum fallback branch decision using declared constants and guard facts.'
    KnownFrameworkContracts = 'Framework contract argument projection and exception-source/evidence integration.'
    LocalSourceFacts = 'Exception-factory delegate and direct-throw source policy shared by local/summary analysis; not generic delegate-target or value-fact resolution.'
    NumericConditions = 'Operator/constant-side branch truth integration; numerical facts remain in providers.'
    ThrowReachability = 'Coalesce/conditional/switch/null/string branch decisions integrating evaluator facts.'
    SummaryGraphEvaluation = 'Upper session factory and one-shot entry composition; intended outer host/session ownership; part of A6 blocker.'
    SummaryGraphDispatch = 'Invocation target planning, source coverage and runtime dispatch coordination; coverage body query is a blocker edge.'
    SummaryGraphDispatchCompleteness = 'Target-set completeness and uncertainty coordination, not the extracted method-shape classifier.'
    SummaryGraphAccessorDispatch = 'Accessor runtime target/path coordination.'
    SummaryGraphImplicitDispatch = 'Implicit-call runtime target and argument coordination.'
    SummaryGraphDisposalResolution = 'Disposal pattern/runtime target selection and source-coverage coordination.'
    SummaryGraphTraversal = 'Summary syntax traversal, scoped fragment/source/path coordination.'
}
$groups = @($a.Composition.AnalyzerMethods | Group-Object File | ForEach-Object {
    $slice = [IO.Path]::GetFileName($_.Name).Replace('ExceptionFlowAnalyzer.', '').Replace('.cs', '')
    $reason = $responsibility[$slice]
    if (-not $reason) { $reason = 'Summary ' + $slice.Replace('SummaryGraph', '') + ' syntax/operation handling, call-edge registration, exception-path and uncertainty composition.' }
    [pscustomobject]@{ File = $_.Name; Slice = $slice; MethodCount = $_.Count; Responsibility = $reason
        HistoricalCoreRelevant = $slice -ne 'SummaryGraphEvaluation'; MainCompositionOnly = $slice -eq 'SummaryGraphEvaluation'
        AnalyzerOwnedDownstreamFacts = 0; DeclarationIds = @($_.Group.Id) }
})
$analyzerMethods = @($a.Composition.AnalyzerMethods | ForEach-Object {
    [pscustomobject]@{ Id = $_.Id; File = $_.File; Line = $_.Line; DeclarationTokenHash = $_.DeclarationTokenHash
        Responsibility = ($groups | Where-Object File -CEQ $_.File).Responsibility; Callers = $_.Callers }
})
function StateRole($entry) {
    if ($entry.File -ceq $hostCutFile) { return 'Main host environment lifetime; readonly ProjectClosure context, not in historical source manifest.' }
    if ($entry.IsConst) { return 'Compile-time constant, no mutable state/lifetime dependency.' }
    if ($entry.Owner -like '*ExternalDocumentationExceptionModel') { return 'Process-local sidecar XML string/array index; strong path-keyed static cache, no invalidation; immutable artifacts required; file access supplied by local artifact layout, no Roslyn objects cached.' }
    if ($entry.Owner -like '*ContextualFactEvaluator') { return 'Weak SemanticModel partition / partition gate / OriginalDefinition symbol-to-bool entries; process-local core state, guard-before-cache and finally removal; locks unchanged.' }
    if ($entry.Owner -like '*DataFlowFactsProvider') { return 'Core-local weak SemanticModel partitions; exact syntax/overload key, immutable snapshots, per-partition lock around lookup/computation; calculator and factory bind only provider methods.' }
    if ($entry.Owner -like '*DereferenceFactDiscovery') { return 'Core-local weak SemanticModel partitions; exact syntax/symbol/query-mode keys; locked lookup/store, uncached computation outside lock.' }
    if ($entry.Owner -like '*SemanticScope') { return 'One Compilation lifetime; exact tree ownership and sequential lazy source-types list; not global, no cross-world symbol sharing.' }
    if ($entry.Owner -like '*KnownFramework*') { return 'Process-local private contract registry / readonly contract snapshots; static matcher/evaluator method references stay within KnownFramework owner; no Analyzer callback or global configuration.' }
    if ($entry.Owner -like '*Flow.Canonical.Canonical*') { return 'Roslyn-neutral identity/result value graph, owned snapshots/collections; no shared semantic state or Main composition. Readonly reference alone is not proof of deep immutability.' }
    if ($entry.Owner -like '*Flow.Canonical.Roslyn*') { return 'Roslyn-bound adapter/resolver context or resolved summary snapshot; Compilation, symbols, callable key and context stay in the local compiler universe. Not transportable neutral state.' }
    if ($entry.Owner -like '*Models.ExceptionFlowPath*' -or $entry.Owner -like '*Models.ExceptionFlowDetails') { return 'Roslyn-neutral path/dedup/details values; stepDeduplicationKeys is a weak path-step memo; no semantic-world key or Main configuration.' }
    if ($entry.Owner -like '*ExceptionFlowAnalysisResult') { return 'Per-analysis mutable exception/evidence/path sets and dictionaries; Roslyn symbol universe stays local; predicate delegates are synchronous merge/filter inputs, not retained factories.' }
    if ($entry.Owner -like '*Summary*') { return 'Sequential environment/session/graph lifetime or per-evaluation traversal; mutable graph/dictionaries/sets are core-local. Session is intentionally not thread-safe; no sharing across compiler universes.' }
    if ($entry.Owner -like '*Analyzer') { return 'Nested per-plan target/context/dispatch snapshots; no Analyzer static nonconstant field. Analyzer is not owner of downstream caches.' }
    return 'Per-analysis callable/context/catch/traversal value or local resolver context; core-local, Roslyn symbols remain within one compiler universe; no Main global configuration.'
}
$state = @($g.State | Where-Object { $core -contains $_.Owner } | ForEach-Object {
    [pscustomobject]@{ Id = $_.Id; Owner = $_.Owner; File = $_.File; Line = $_.Line; Type = $_.Type
        IsStatic = $_.IsStatic; IsReadOnlyReference = $_.IsReadOnly; IsConst = $_.IsConst; Kind = $_.Kind
        Visibility = $_.Visibility; Initializer = $_.Initializer; CoreInternal = $_.File -cne $hostCutFile
        LifetimeAndRisk = StateRole $_; MainCompositionRequired = $_.File -ceq $hostCutFile }
})
$static = @($state | Where-Object { $_.CoreInternal -and $_.IsStatic -and -not $_.IsConst })
Gate ($static.Count -eq 11) 'Static-state census changed.'
$api = @($g.CoreMetadataMembers | Where-Object Owner -like 'Microsoft.CodeAnalysis*' | Group-Object Callee | ForEach-Object {
    [pscustomobject]@{ Signature = $_.Name; Owner = $_.Group[0].Owner; Kinds = @($_.Group.Kind | Sort-Object -Unique)
        Sites = @($_.Group | Select-Object Caller,File,Line,Kind) }
} | Sort-Object Signature)
$roslynTypes = @($g.RoslynUses | Group-Object Callee | ForEach-Object {
    [pscustomobject]@{ Type = $_.Name; Namespace = $_.Group[0].Namespace; Assembly = $_.Group[0].Assembly
        SourceFiles = @($_.Group.File | Sort-Object -Unique) }
} | Sort-Object Type)
$packages = @(@('src/XMLDocNormalizer/XMLDocNormalizer.csproj', 'src/XMLDocNormalizer.ExceptionFlow.Core/XMLDocNormalizer.ExceptionFlow.Core.csproj',
    'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj', 'Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj') | ForEach-Object {
    [xml] $project = Get-Content -LiteralPath $_ -Raw
    [pscustomobject]@{ Project = $_; Sha256 = (Get-FileHash $_).Hash; TargetFramework = @($project.Project.PropertyGroup.TargetFramework | Where-Object { $_ })
        Packages = @($project.Project.ItemGroup.PackageReference | Where-Object { $_ } | ForEach-Object { [pscustomobject]@{ Include = $_.Include; Version = $_.Version } })
        ProjectReferences = @($project.Project.ItemGroup.ProjectReference | Where-Object { $_ } | Select-Object Include,ReferenceOutputAssembly)
        LinkedSource = @($project.Project.ItemGroup.Compile | Where-Object { $_ } | Select-Object Include,Link) }
})
[xml] $trx = Get-Content -LiteralPath 'artifacts/p5o2a6/test-results/architecture.trx' -Raw
$counts = $trx.TestRun.ResultSummary.Counters
Gate ([int]$counts.passed -eq 27 -and [int]$counts.failed -eq 0 -and [int]$counts.notExecuted -eq 0) 'Architecture TRX gate.'
$history = @(
    @{ Package='A4C Semantic-model seam'; Downstream='102 -> 92'; FactEdges='68 after'; Components='20 after'; Scope='18 after'; Report='P5O2A4C-semantic-model-resolution-and-fact-extraction.md' },
    @{ Package='A4C Residual families'; Downstream='92 -> 64'; FactEdges='68 -> 38'; Components='20 -> 50'; Scope='18'; Report='P5O2A4C-residual-fact-family-extraction.md' },
    @{ Package='A4C Runtime/stable-member'; Downstream='64 -> 57'; FactEdges='38 -> 36'; Components='50 -> 57'; Scope='18'; Report='P5O2A4C-runtime-dispatch-stable-source-member-extraction.md' },
    @{ Package='A4C Return/condition'; Downstream='57 -> 49'; FactEdges='36 -> 32'; Components='57 -> 61'; Scope='18'; Report='P5O2A4C-return-condition-ownership.md' },
    @{ Package='A4C Sequence/context/element/source'; Downstream='49 -> 27'; FactEdges='32 -> 12'; Components='61 -> 81'; Scope='18'; Report='P5O2A4C-sequence-context-element-source-ownership.md' },
    @{ Package='A4D'; Downstream='27 -> 23'; FactEdges='12 -> 8'; Components='81 -> 85'; Scope='18'; Report='P5O2A4D-dictionary-value-fact-ownership.md' },
    @{ Package='A4E'; Downstream='23 -> 14'; FactEdges='8 -> 1'; Components='85 -> 92'; Scope='18'; Report='P5O2A4E-sequence-range-dictionary-mutation-fact-ownership.md' },
    @{ Package='A4F'; Downstream='14 -> 0'; FactEdges='1 -> 0'; Components='92 -> 93'; Scope='18'; Report='P5O2A4F-successful-sequence-validation-fact-ownership.md' },
    @{ Package='A4G'; Downstream='0 -> 0'; FactEdges='0 -> 0; delegate 1 -> 0'; Components='93 -> 94'; Scope='18'; Report='P5O2A4G-delegate-target-resolver-ownership.md' },
    @{ Package='A5A'; Downstream='0'; FactEdges='0'; Components='126 / 19 owners, full kind-labelled census'; Scope='19 incl Compilation overload'; Report='P5O2A5A-contextual-evaluator-boundary-audit.md' },
    @{ Package='A5B'; Downstream='0'; FactEdges='0'; Ownership='63 SCC + 2 helpers + 3 fields Analyzer -> Evaluator'; Report='P5O2A5B-contextual-fact-evaluator-extraction.md' },
    @{ Package='A5C'; Downstream='0'; FactEdges='0'; Facades='2 -> 0; 14 sites direct'; Report='P5O2A5C-contextual-evaluator-composition-cleanup.md' },
    @{ Package='A6'; Downstream='0'; FactEdges='0'; Facades='0'; WholeArchitectureComponentCycles=1; Report='P5O2A6-architecture-readiness-closure.md' }
)
$evidence = [ordered]@{
    Schema = 'ExceptionFlow.ArchitectureReadinessClosure.v1'; Date = '2026-10-06'; Head = $head; Decision = 'NOT READY'
    Scope = 'Audit/evidence only. No production/test/TFM/package/cache/worker/IPC change. Type/member/initializer/lambda dependencies bound against current active compilation; explicitly cut active nonvirtual semantic host implementation; not a successful historical compile. Source-declared callable graph excludes compiler-generated method bodies; record-backed state is inventoried explicitly. Static dependency audit, not metadata implementation/runtime points-to/dynamic execution proof.'
    ClosureGates = @{ AnalyzerOwnedDownstreamFacts=0; EvaluatorToAnalyzer=0; LowerToAnalyzer=0; UnjustifiedEvaluatorFacades=0
        SCCMethods=63; SCCInternalEdges=112; CacheSupportMethods=2; CacheFields=3; SCCIngressEdges=16; SCCIngressCallers=15; SCCIngressTargets=4
        SCCEgress=791; EvaluatorIngressEdges=27; EvaluatorIngressCallers=24; EvaluatorIngressTargets=4
        SCCDownstreamComponentCycles=0; ProviderInternalDataFlowCycles=1; WholeCoreTopLevelTypeCycles=3
        InternalCanonicalDomainTypeCycles=2; WholeSourceMethodOwnerCycles=2; InterOrchestrationComponentCycles=1; Passed=$false }
    Blocker = @{ Types=$blocked; MethodEdges=$blockEdges; TypeUses=@($g.TypeUses | Where-Object { $_.Source -and $blocked -contains $_.Caller -and $blocked -contains $_.Callee -and $_.Caller -cne $_.Callee })
        RootCause='Analyzer creates/calls SummarySession, which invokes Builder, which calls Analyzer traversal; Analyzer additionally asks Builder for exact summary-body availability. Both upper orchestration back-seams must close.'
        MinimalPackage='Separate P5O2A6-Fix: relocate session factory/one-shot entry ownership to outer composition or existing Session; share the exact two-method body-availability predicate below Analyzer and Builder; preserve nonvirtual same-source host and fail-closed body/semantic-tree lookup. No implementation in A6.'
        Risk='Localized ownership change but multiple components and source-coverage semantics involved; keep predicates/token behavior, session reuse and SameCompilation precision; rerun supporting-source/runtime-summary/guards plus full semantic regression after authorized fix.'
        WhyNotB='User requires zero inter-component cycles. Source candidate is known, but current upper component DAG is not closed. A technically compilable cohesive source unit would not prove that gate.' }
    CycleScopes = @{ WholeCoreTypeCycles=$g.ProposedCoreTypeCycles; AllSourceTypeCycles=$g.AllSourceTypeCycles; MethodOwnerCycles=$g.MethodOwnerCycles
        ProviderInternalExpandedCycles=$a.ComponentCycles
        DomainInterpretation='CanonicalCallContext/IdentityKeyWriter form one neutral key domain; FunctionPointerParameter/TypeIdentity form one recursive type domain. Report both raw cycles, not erased counts. Analyzer/Session/Builder is a genuine orchestration component cycle.'
        HistoricalInterpretation='A5A-C cycle=0 was SCC-downstream closure, not every upper orchestration edge. A6 broadens scope; no production regression.' }
    SourceManifest = $sourceFiles; Components = $components; MainOnlyDeclarations = $mainOwners
    HostCut = @{ File=$hostCutFile; Type=$flow+'ExceptionFlowSemanticEnvironment'; Dependencies=$g.HostImplementationDependencies
        Requirements=@('TryGetSemanticModel exact tree/Compilation identity','GetAnalysisScopes','TryResolveSupportingSourceMethod overloads','TryGetExternalSupportingSourceScope')
        ActiveState=@($state | Where-Object { -not $_.CoreInternal }); UnimplementedHistoricalHost=$true }
    SourceHashes = $g.SourceHashes; TypeDeclarations=$g.TypeDeclarations; CoreTypeEdges=$g.ProposedCoreTypeEdges; AllSourceTypeEdges=$g.AllSourceTypeEdges
    GraphCensus=@{SourceDeclaredCallableNodes=@($a.AllSourceNodes).Count; MethodDeclarationNodes=$a.AllMethodCount; PrimarySourceInvocationEdges=$a.AllMethodEdgeCount; AllKindLabelledEdges=@($g.MethodEdges).Count; SourceTargetKindLabelledEdges=@($g.MethodEdges | Where-Object Source).Count; CoreTypeEdges=@($g.ProposedCoreTypeEdges).Count}
    SourceMethodEdges=@($g.MethodEdges | Where-Object Source); AllMethodOwnerEdges=$g.MethodOwnerEdges; CoreCallableDeclarations=$coreNodes
    LowerFactResolverClosure=$lowerReach; AnalyzerResponsibilityGroups=$groups; AnalyzerMethods=$analyzerMethods
    AnalyzerSize=@{ PrefixFiles=$a.AnalyzerPartials; WholeFileNonblankSloc=$a.AnalyzerNonblankSloc; ActualDeclarations=$a.Composition.AnalyzerDeclarations; OwnedNonblankLines=$a.Composition.AnalyzerOwnedNonblankLines; Methods=$analyzerMethods.Count }
    ContextualClosure=@{ Members=$a.Scc.Members; InternalEdges=$a.Scc.InternalEdges; Ingress=$a.Ingress; Egress=$a.Egress
        CacheSupport=$a.Composition.EvaluatorCacheSupportMethods; Fields=$a.Composition.EvaluatorFields; SeedSites=$a.Composition.SeedInvocationSites
        TokenIdenticalToCommittedA5C=$true; Lifetime=$base.CacheGuardLifetime; ThinFacadeCandidates=$a.Composition.ThinFacadeCandidates; EvaluatorExternalIngress=$a.Composition.EvaluatorExternalIngress }
    CallbackInventory=@{ LambdaSites=@($a.DelegateSites | Where-Object { $coreIds.ContainsKey($_.Caller) }); Initializers=@($a.InitializerDependencies | Where-Object { $sourceFiles.File -contains $_.File })
        Decisions=@('DataFlow calculator=ComputeDataFlowFacts and partitionFactory=CreatePartition, provider-internal; test-injected calculator is not production Analyzer factory.',
        'KnownFramework registry static matcher/evaluator references remain within KnownFramework model.',
        'CallContext.RebindCallable callback from SummaryTargetRegistrar resolves via CrossCompilationResolver into supporting Scope, no Analyzer target.',
        'AnalysisResult merge/filter predicates from SummaryGraphEvaluator are synchronous per-analysis catch decisions; no retained Analyzer callback.',
        'Evaluator callback-return analysis inspects user syntax, not an injected Analyzer computation. Whole type closure plus bound lambda/method references finds no lower return path.') }
    StateInventory=$state; StaticNonconstantCoreState=$static
    GlobalConfiguration='No core mutable global configuration found. Active ProjectClosure/acquisition/workspace/options state belongs to excluded Main host. Local artifact layout is required by sidecar XML cache; cached files must be immutable during process lifetime.'
    RoslynInventory=@{ Namespaces=@($g.RoslynUses.Namespace | Sort-Object -Unique); Types=$roslynTypes; MetadataMembers=$api; Projects=$packages
        BoundAssemblies=@($g.RoslynUses.Assembly | Sort-Object -Unique)
        ActiveMainBinFingerprints=@(@('Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll') | ForEach-Object {
            $binary = Get-Item -LiteralPath ('src/XMLDocNormalizer/bin/Debug/net8.0/' + $_)
            [pscustomobject]@{File=$binary.Name; ProductVersion=$binary.VersionInfo.ProductVersion; Sha256=(Get-FileHash -LiteralPath $binary.FullName).Hash}
        })
        HistoricalTarget='Use exact P5N validated net8 compiler pair (5.0.0-2.25451.107+2db1f5ee2bdda2e8d873769325fabede32e420e0), not an invented older NuGet version. P5O2B must establish complete pinned binary/runtime dependency manifest before dual compile.'
        Risks=@('Function-pointer symbol/ref-kind signatures; list/recursive/unary pattern syntax and operation shapes.',
        'SemanticModel/ModelExtensions typed overloads, data-flow and symbol/type binding; CSharp await/foreach/deconstruction/conversion overloads.',
        'DocumentationCommentId, SymbolDisplayFormat and symbol identity/rebinding; matching public assembly version does not prove compiler behavior.',
        'LanguageVersion, operation lowering, diagnostics and emitted provenance can differ even if source compiles.',
        'BCL/runtime/System.Collections.Immutable/System.Reflection.Metadata closure must be pinned with exact compiler binaries; not established by A6.')
        CompatibilityTested=$false; WorkspacesOrMSBuildInProposedCore=$mainApiUses.Count }
    Progress=$history
    Validation=@{ AuditBuild=@{Warnings=0;Errors=0;WarningAsError=$true}; ArchitectureTests=@{Passed=27;Total=27;Failed=0;Skipped=0;Artifact='artifacts/p5o2a6/test-results/architecture.trx';Sha256=(Get-FileHash 'artifacts/p5o2a6/test-results/architecture.trx').Hash}
        Reproducible=@{ByteIdentical=$true;Measurements=@(@{Path=$Measurement;Sha256=$rawHash},@{Path=$Repeat;Sha256=(Get-FileHash $Repeat).Hash})}
        ProductionUnchanged=$true; TestsUnchanged=$true; A5CBoundGraphAndDeclarationsEqual=$true; ValidatedA5CSourceHashes=$validatedHashes
        InheritedNotRerun=@{Source='Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup-audit.json'; FullTests=@($base.Validation.Tests | Where-Object Run -eq 'full-recovery'); SolutionBuild=$base.Validation.SolutionWarningAsErrorBuild; SelfAnalysis=$base.Validation.SelfAnalysis; Canonical=$base.Validation.Canonical}
    }
    Git=@{ ProtectedForeignPaths='Root .gitignore and seven tracked Core bin/obj paths remain unchanged; no solution/Core build in A6.'; Stashes=@(git stash list --format='%H %gs'); CommitPushStashReset=$false }
}
WriteUtf8 $Output ((ConvertTo-Json -InputObject $evidence -Depth 100) + "`n")
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# P5O2A6 - Historical Core Readiness Matrix')
$lines.Add(''); $lines.Add('HEAD: `' + $head + '`. NOT READY: upper orchestration cycle; no production changes.')
$lines.Add(''); $lines.Add('Exact candidate: 100 top-level source owners / 117 physical source files. A=historical algorithms, B=source-shared infrastructure, C=Main-only host/composition, D=neutral boundary values, E=blocked current orchestration (intended A). Mixed files carry all categories; a neutral declaration does not make its containing file neutral. Roslyn=no means no direct bound Roslyn use, not proven transitive binary independence.')
$lines.Add(''); $lines.Add('The active ProjectClosure semantic host partial is explicitly excluded. A same-source nonvirtual historical host with equivalent capabilities remains required in future B; this is not a closed standalone historical compilation today. No ordinary Main/Core runtime project-reference experiment is prescribed.')
$lines.Add(''); $lines.Add('## Component inventory'); $lines.Add('')
$lines.Add('| Owner | Category | Direct Roslyn | Source dependencies |'); $lines.Add('| --- | --- | --- | --- |')
foreach ($component in $components) {
    $lines.Add('| ' + $component.Owner + ' | ' + $component.Category + ' | ' + $component.DirectRoslynUse + ' | ' + (($component.SourceDependencies | ForEach-Object { ($_ -split '\.')[-1] }) -join ', ') + ' |')
}
$lines.Add(''); $lines.Add('## Exact source manifest'); $lines.Add('')
$lines.Add('| File | Categories | File uses Roslyn | Owners |'); $lines.Add('| --- | --- | --- | --- |')
foreach ($file in $sourceFiles) { $lines.Add('| [' + $file.File + '](../' + $file.File + ') | ' + ($file.Categories -join '/') + ' | ' + $file.FileHasRoslynUse + ' | ' + (($file.Owners | ForEach-Object { ($_ -split '\.')[-1] }) -join ', ') + ' |') }
$lines.Add(''); $lines.Add('## C: excluded active host and Main source declarations'); $lines.Add('')
$lines.Add('| Owner | File |'); $lines.Add('| --- | --- |'); $lines.Add('| ExceptionFlowSemanticEnvironment active host | ' + $hostCutFile + ' |')
foreach ($owner in $mainOwners) { $lines.Add('| ' + $owner.Owner + ' | ' + $owner.File + ' |') }
$lines.Add(''); $lines.Add('## Remaining Analyzer responsibilities'); $lines.Add('')
$lines.Add('| Slice | Methods | Core relevant | Main entry composition | Responsibility |'); $lines.Add('| --- | ---: | --- | --- | --- |')
foreach ($group in $groups) { $lines.Add('| ' + $group.Slice + ' | ' + $group.MethodCount + ' | ' + $group.HistoricalCoreRelevant + ' | ' + $group.MainCompositionOnly + ' | ' + $group.Responsibility + ' |') }
$lines.Add(''); $lines.Add('## State and lifetime (fields, auto-properties and positional record properties)'); $lines.Add('')
$lines.Add('Readonly below describes the declared reference/property, not deep immutability. Full types, initializers and source lines are in the paired audit JSON. Per-call locals and receiver operations remain in reproducible raw measurements; no shared state is silently inferred from a local name.')
$lines.Add(''); $lines.Add('| State | Static | Core-local | Lifetime, ownership and risk |'); $lines.Add('| --- | --- | --- | --- |')
foreach ($entry in $state) { $lines.Add('| ' + $entry.Id + ' | ' + $entry.IsStatic + ' | ' + $entry.CoreInternal + ' | ' + $entry.LifetimeAndRisk + ' |') }
$lines.Add(''); $lines.Add('## Roslyn API inventory'); $lines.Add('')
$lines.Add('All ' + $roslynTypes.Count + ' directly bound Roslyn types and ' + $api.Count + ' unique metadata member signatures (with source call sites) are in the paired audit JSON. Namespaces: ' + (($g.RoslynUses.Namespace | Sort-Object -Unique) -join ', ') + '. No compatibility claim follows; exact historical pair/runtime closure has not been compiled in A6.')
WriteUtf8 $Matrix (($lines -join "`n") + "`n")
[pscustomobject]@{ Decision='NOT READY'; CoreOwners=$core.Count; CoreFiles=$sourceFiles.Count; LowerOwners=$lowerReach.Count; BlockerEdges=$blockEdges.Count; StateEntries=$state.Count; StaticCoreState=$static.Count; RoslynTypes=$roslynTypes.Count; RoslynMembers=$api.Count; RepeatHash=$rawHash; Output=$Output; Matrix=$Matrix } | ConvertTo-Json
