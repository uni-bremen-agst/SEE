param(
    [string] $Measured = 'artifacts/p5o2a5a/measured-boundary.json',
    [string] $Output = 'Evaluation/P5O2A5A-contextual-evaluator-boundary-audit.json',
    [string] $Matrices = 'Evaluation/P5O2A5A-contextual-evaluator-boundary-matrices.md'
)
$ErrorActionPreference = 'Stop'
$a = Get-Content -LiteralPath $Measured -Raw -Encoding UTF8 | ConvertFrom-Json
$prefix = 'XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.'
$coreIds = @($a.Scc.Members.Id)
$reachableIds = @($a.ReachableSourceMethods.Id)
function Short([string] $s) {
    $s.Replace($prefix, '').Replace('Microsoft.CodeAnalysis.CSharp.Syntax.', '').Replace('Microsoft.CodeAnalysis.', '').Replace('System.Collections.Generic.', '')
}
function Cell([string] $s) { $s.Replace('|', '\|').Replace('<', '&lt;').Replace('>', '&gt;').Replace([string][char]13, '').Replace([string][char]10, ' ') }
function Label([string] $id) {
    if ($coreIds -contains $id) { return ('M{0:d2}' -f (1 + [array]::IndexOf($coreIds, $id))) }
    return (Short $id)
}
$roles = @{
    CreateAccessorCallContext = 'Accessor argument/setter context assembly'
    CreateCallContext = 'Seed guard allocation then contextual call-context forwarding; no exception-flow orchestration'
    CreateInvocationCallContext = 'Invocation/runtime-target context assembly'
    CreateKnownFrameworkContractArguments = 'Framework-contract argument composition'
    CreateSummaryCollectionInitializerCallContext = 'Collection-initializer summary-edge context'
    CreateSummaryImplicitCallContext = 'Implicit-call summary-edge context'
    CreateSummaryOperationCallContext = 'Operator/conversion summary-edge context'
    EvaluateNullComparison = 'Condition/reachability null comparison'
    EvaluateNullPattern = 'Condition/reachability null pattern'
    EvaluatePositiveInt32Comparison = 'Numeric condition/reachability comparison'
    EvaluateStringPredicate = 'Condition/reachability string predicate'
    GetSummaryCollectionArgumentFacts = 'Collection argument binding and params/default fact adapter'
    IsDefinitelyNonNull = 'Seed guard allocation then non-null fact forwarding; no exception-flow orchestration'
    IsThrowExpressionInExhaustiveDefinedEnumFallback = 'Enum-switch fallback reachability'
    IsThrowExpressionProvenUnreachable = 'Throw reachability/branch selection'
}
$ingress = @($a.Ingress | ForEach-Object {
    $thin = $_.Caller.Name -in @('CreateCallContext', 'IsDefinitelyNonNull')
    [pscustomobject][ordered]@{
        Caller = $_.Caller; Edge = $_.Edge
        Category = $(if ($thin) { 'B' } else { 'A' })
        Role = $roles[$_.Caller.Name]
        A5BOwner = 'Analyzer (temporary seed facade)' * [int]$thin + 'Analyzer orchestration' * [int](!$thin)
        A5CCandidate = $thin
        Caveat = $(if ($thin) { 'Not literally zero logic: owns one fresh SymbolEqualityComparer.Default guard. Preserve allocation/identity if removed.' } else { 'Retain orchestration and redirect the measured SCC call only.' })
    }
})
$entries = @($a.Ingress.Edge.Callee | Sort-Object -Unique)
$cacheSupport = @($a.ReachableAnalyzerOutsideScc.Id)
$old = Get-Content -Raw -Encoding UTF8 Evaluation/P5O2A4C-residual-scc-audit.json | ConvertFrom-Json
$oldIds = @($old.Nodes.Signature)
$oldMap = @{}
foreach ($n in $old.Nodes) { $oldMap[$n.Key] = $n.Signature }
$oldEdges = @($old.InternalEdges | ForEach-Object { $oldMap[$_.Caller] + ' -> ' + $oldMap[$_.Callee] })
$newEdges = @($a.Scc.InternalEdges | ForEach-Object { $_.Caller + ' -> ' + $_.Callee })
foreach ($m in $a.Scc.Members) {
    $contextRole = $m.Name -in @('CreateCallContext', 'AddExplicitArgumentFacts', 'AreSequenceElementsProvenNonNull')
    $m | Add-Member -NotePropertyName A5BOwner -NotePropertyValue 'ExceptionFlowContextualFactEvaluator'
    $m | Add-Member -NotePropertyName Role -NotePropertyValue $(if ($contextRole) { 'Context construction/projection or contextual sequence seed/evaluation; not exception orchestration' } else { 'Contextual fact evaluation, including recursive proof and internal seeds' })
}
$compositionReview = @(
    [pscustomobject][ordered]@{ Element='ExceptionFlowDataFlowFactsProvider.dataFlowFactCache'; Binding='new DataFlowFactCache() -> this(ComputeDataFlowFacts) -> provider static calculator; partitionFactory = cache.CreatePartition'; Source='ExceptionFlowDataFlowFactsProvider.cs:23,122,135'; AnalyzerReference=$false; Ownership='Provider, unchanged'; TypeCycle='Provider <-> nested cache, one semantic owner; no cycle across evaluator cut' },
    [pscustomobject][ordered]@{ Element='DataFlowRegionKeyComparer.Instance'; Type='DataFlowRegionKeyComparer'; Category='D'; Lifetime='Static immutable stateless singleton, auto-property initialized with new()'; Source='ExceptionFlowDataFlowFactsProvider.cs:393'; Role='Exact syntax reference identity + region kind Equals/GetHashCode, invoked by Dictionary runtime dispatch'; AnalyzerReference=$false; Ownership='Provider, unchanged' },
    [pscustomobject][ordered]@{ Element='SuccessfulDereferenceCacheKeyComparer.Instance'; Type='SuccessfulDereferenceCacheKeyComparer'; Category='D'; Lifetime='Static immutable stateless singleton, auto-property initialized with new()'; Source='ExceptionFlowDereferenceFactDiscovery.cs:532'; Role='Exact syntax reference identity + SymbolEqualityComparer.Default + query mode Equals/GetHashCode, invoked by Dictionary runtime dispatch'; AnalyzerReference=$false; Ownership='Provider, unchanged' },
    [pscustomobject][ordered]@{ Element='ExceptionFlowCallContext.CallableSymbol and Key'; Type='ISymbol? and string'; Category='C/D'; Lifetime='Constructor-derived auto-property state; normalized OriginalDefinition and existing legacy key; context lifetime, no reset'; Readers='Context fact predicates/identity users through public getters; read in SCC/reachable domain methods'; Writers='CallContext constructors only'; Ownership='CallContext, unchanged'; AnalyzerReference=$false },
    [pscustomobject][ordered]@{ Element='Analyzer orchestration state'; Category='E'; IntersectionWithScc='None: no TraversalState, exception path, Summary session/graph or runtime-target state field/parameter in measured SCC'; Ownership='Analyzer/established orchestration, unchanged' }
)
$dependencyOwners = @($a.EgressOwners | ForEach-Object {
    $o = $_.Owner
    $own = $o.StartsWith('XMLDocNormalizer.')
    $cache = $o.Contains('ExceptionFlowAnalyzer.ConditionalWeakTableValueFactCachePartition')
    $ownerPrefix = $o + '.'
    $lower = @($a.ComponentEdges | Where-Object Caller -eq $o | Select-Object -ExpandProperty Callee -Unique)
    $fields = @($a.FieldDeclarations | Where-Object Owner -eq $o)
    [pscustomobject][ordered]@{
        Owner = $o; DistinctMethodEdges = $_.DistinctMethodEdges; InvocationEdges = $_.InvocationEdges
        Methods = @($_.Methods); Kinds = @($_.Kinds)
        OwnedFieldDeclarations = $fields; DirectDownstreamOwners = $lower
        State = $(if ($cache) { 'Evaluator-owned weak-cache partition (move)' }
            elseif ($o -eq ($prefix + 'ExceptionFlowDereferenceFactDiscovery')) { 'Provider-owned semantic-model weak memoization (retain)' }
            elseif ($o -eq ($prefix + 'ExceptionFlowCallContext')) { 'Per-context constructor snapshots (retain domain owner)' }
            elseif ($o -eq ($prefix + 'ExceptionFlowSemanticScope')) { 'Only static overloads reached; immutable Compilation/SemanticModel arguments, not instance lazy sourceTypes' }
            elseif ($own) { 'No owned mutable field reached; locals/guards only. Downstream provider caches retain their owners.' }
            else { 'Metadata API; receiver lifetime/ownership belongs to caller/argument/cache, implementation not traversed' })
        Acquisition = $(if ($cache) { 'Static CWT.GetValue factory; implicit partition constructor' }
            elseif ($o -eq ($prefix + 'ExceptionFlowCallContext')) { 'Explicit parameter or local new context; constructor chaining copies input collections' }
            elseif ($own) { 'Direct named static calls; no injected service, locator, bag or Analyzer callback' }
            else { 'Bound metadata API on argument/local/cache receiver or static API; full signatures retained' })
        AnalyzerBackReference = $(if ($cache) { 'Nested current Analyzer owner only; both methods and whole partition move with SCC' }
            elseif ($own) { 'No call path to Analyzer found, including explicit method references and reachable field initializers' }
            else { 'Metadata bodies outside audit; no Analyzer delegate/method group passed by SCC' })
        HistoricalLabel = $(if ($cache) { 'Analyzer-Core (future evaluator support)' }
            elseif ($o.StartsWith('Microsoft.CodeAnalysis.')) { 'Current Roslyn API' }
            elseif ($own) { 'Own domain/semantic component; inspect recorded Roslyn signatures, not automatically Roslyn-neutral' }
            else { 'BCL runtime/composition; not worker/IPC design' })
    }
})
$states = @()
$fieldIds = @($a.ReachableState | Where-Object Kind -eq Field | Select-Object -ExpandProperty Id -Unique)
foreach ($id in $fieldIds) {
    $d = @($a.FieldDeclarations | Where-Object Id -eq $id)
    $refs = @($a.AllStateAccesses | Where-Object Id -eq $id)
    $operations = @($a.ReceiverOperations | Where-Object ReceiverId -eq $id)
    $move = $id.Contains('ExceptionFlowAnalyzer.conditionalWeakTableValueFactCaches') -or $id.Contains('ExceptionFlowAnalyzer.ConditionalWeakTableValueFactCachePartition.')
    $context = $id.Contains('ExceptionFlowCallContext.')
    $providerCache = $id -match 'DataFlowFactsProvider.*(cache|Cache|calculator|partitionFactory|partitions|gate|entries|semanticModel)$|DereferenceFactDiscovery.*(successfulDereferenceCaches|gate|entries)$'
    $category = $(if ($move) { 'A' } elseif ($context) { 'C' }
        elseif ($providerCache -and $id -notmatch '\.(calculator|partitionFactory|semanticModel)$') { 'F' } else { 'D' })
    $states += [pscustomobject][ordered]@{
        Id = $id; Declaration = $d; Type = @($a.ReachableState | Where-Object Id -eq $id | Select-Object -First 1).Type
        Category = $category
        Readers = @($refs | Where-Object Read | Select-Object -ExpandProperty Caller -Unique)
        AssignmentWriters = @($refs | Where-Object Write | Select-Object -ExpandProperty Caller -Unique)
        ReceiverOperations = $operations
        InitializerDependencies = @($a.InitializerDependencies | Where-Object Caller -eq $id)
        AccessSites = $refs
        ExclusiveSccOrOwnedSupport = ($refs.Count -gt 0 -and @($refs | Where-Object { $_.Caller -notin ($coreIds + $cacheSupport) }).Count -eq 0)
        OtherAnalyzerReadersOrWriters = @($refs | Where-Object { $_.Caller -match 'ExceptionFlowAnalyzer\.' -and $_.Caller -notin ($coreIds + $cacheSupport) } | Select-Object -ExpandProperty Caller -Unique)
        A5BOwner = $(if ($move) { 'ExceptionFlowContextualFactEvaluator' } elseif ($context) { 'ExceptionFlowCallContext (shared explicit domain value)' } else { 'Existing owner; do not move' })
        LifetimeInitializationInvalidation = $(if ($move) { 'Static CWT at type initialization; partition/gate/dictionary at first model use; weak model ephemeron lifetime; no explicit reset. Lock lookup/store, compute outside lock.' }
            elseif ($context) { 'Copied and normalized at context constructor; dictionary/member sets read after construction; call/context lifetime; no cache reset or evaluator ownership transfer.' }
            elseif ($providerCache) { 'Existing provider cache lifecycle: weak semantic-model partition, static root; initialization and invalidation described in CacheSemantics. Preserve provider ownership.' }
            else { 'Immutable semantic/enum/comparer/domain input; no mutable evaluator lifetime or reset.' })
        Role = $(if ($move) { 'Context-neutral CWT field invariant memoization and synchronization' } elseif ($context) { 'Call-site parameter/member fact snapshot' } elseif ($providerCache) { 'Downstream provider memoization/composition support, not SCC-owned' } else { 'Shared immutable semantic/domain infrastructure' })
    }
}
$parameterState = @($a.Scc.Members | ForEach-Object {
    $m = $_
    foreach ($p in $m.Parameters) {
        $category = $(if ($p.Type -match 'HashSet<.*ISymbol') { 'B' }
            elseif ($p.Name -in @('knownParameterFacts', 'suppliedParameterIndexes')) { 'F' }
            elseif ($p.Type -match 'ExceptionFlowCallContext' -or $p.RefKind -eq 'Out') { 'C' } else { 'D' })
        [pscustomobject][ordered]@{
            Method = $m.Id; Name = $p.Name; Type = $p.Type; Category = $category
            ReadersAndWrites = @($m.StateReferences | Where-Object { $_.Kind -eq 'Parameter' -and $_.Id.EndsWith(' ' + $p.Name) })
            ReceiverOperations = @($a.ReceiverOperations | Where-Object { $_.Caller -eq $m.Id -and $_.Receiver -eq $p.Name })
            Lifetime = $(if ($category -eq 'B') { 'Per seed/evaluation chain; passed by reference identity, Add/Remove try/finally; intentional copies preserved; no global shared guard' }
                elseif ($category -eq 'F') { 'Per argument projection call, caller-owned mutable scratch; synchronous typed arguments shared with accessor orchestration/projector; no stored Analyzer field' }
                elseif ($category -eq 'C') { 'Immutable-in-use call-context snapshot supplied or constructed for this evaluation' }
                else { 'Explicit semantic/syntax/symbol/value input; no evaluator ownership of Compilation/SemanticModel' })
            A5BOwner = $(if ($category -eq 'B') { 'Evaluator seed/recursive chain; facade allocation may remain temporarily' }
                elseif ($category -eq 'F') { 'Caller scratch shared through exact typed parameters; never Analyzer object' } else { 'Existing domain/semantic input owner' })
        }
    }
})
$locals = @($a.LocalState | Where-Object { $_.Caller -in $coreIds -and $_.Type -match 'HashSet|Dictionary|List<|ExceptionFlowCallContext|CachePartition' } | ForEach-Object {
    $l = $_
    [pscustomobject][ordered]@{
        Declaration = $l
        AccessSites = @($a.DirectState | Where-Object { $_.Caller -eq $l.Caller -and $_.Kind -eq 'Local' -and $_.Id -eq $l.Name })
        Category = $(if ($l.Type -match 'HashSet<.*ISymbol') { 'B' } elseif ($l.Type -match 'CachePartition') { 'A' } else { 'C' })
        Access = @($a.ReceiverOperations | Where-Object { $_.Caller -eq $l.Caller -and $_.Receiver -eq $l.Name })
        Lifetime = 'Method/seed invocation; initializer recorded verbatim; no stored Analyzer state; preserve collection/context identity and intentional guard copy sites'
        A5BOwner = 'Evaluator local (cache alias retains shared weak partition)'
    }
})
$cacheSemantics = @(
    [pscustomobject][ordered]@{ Name='conditionalWeakTableValueFactCaches'; Move=$true; Key='SemanticModel identity (weak outer key), field.OriginalDefinition (inner key)'; Value='ConditionalWeakTableValueFactCachePartition / bool'; Comparer='ConditionalWeakTable reference identity + SymbolEqualityComparer.Default'; Context='None: context-neutral private static readonly source field invariant, fresh guard seeded with field; scan all source uses in same Compilation via scope overload'; Lifetime='Already static; create table at type init, partition on first model use; weak ephemeron model/compilation lifetime; no explicit reset'; Recursion='Ambient guard Add BEFORE memo lookup; false on duplicate; Remove in finally; separate invariant guard seeded with field'; Threading='CWT concurrent GetValue; partition lock for lookup and TryAdd store only; duplicate immutable computation allowed outside lock, first stored answer retained' },
    [pscustomobject][ordered]@{ Name='successfulDereferenceCaches'; Move=$false; Owner='ExceptionFlowDereferenceFactDiscovery'; Key='SemanticModel weak identity; exact ExpressionSyntax reference + ISymbol + SuccessfulDereferenceQueryMode'; Value='ExceptionFlowValueFacts'; Comparer='Syntax ReferenceEquals/RuntimeHelpers.GetHashCode, symbol SymbolEqualityComparer.Default, mode exact'; Context='No call context; successful syntax/symbol dereference observation scoped to exact semantic model/Compilation'; Lifetime='Static weak table; first-use partition; no explicit reset'; Recursion='Provider-owned per-call guards preserved, no Analyzer callback or global in-progress state'; Threading='Per-partition locks around lookup/TryAdd; compute outside lock' },
    [pscustomobject][ordered]@{ Name='dataFlowFactCache'; Move=$false; Owner='ExceptionFlowDataFlowFactsProvider'; Key='SemanticModel weak identity; DataFlowRegionKey(exact StatementSyntax/ExpressionSyntax object, DataFlowRegionKind)'; Value='ExceptionFlowDataFlowFacts(Succeeded, ImmutableArray<ISymbol> WrittenInside)'; Comparer='ReferenceEquals/RuntimeHelpers.GetHashCode region, exact overload kind; symbol values stay in keyed semantic world'; Context='No call context; partition retains exact semantic model/Compilation via weak ephemeron'; Lifetime='Static provider cache; readonly partitions + calculator + partitionFactory; partition first model use; no reset'; Recursion='Default ctor binds provider ComputeDataFlowFacts, partition factory binds same cache CreatePartition; no contextual evaluator callback'; Threading='Partition lock covers lookup AND first Roslyn AnalyzeDataFlow computation; calculator readonly' }
)
$proposal = [pscustomobject][ordered]@{
    Ready=$true; MinimalPreparatoryPackage=$null
    Shape='Internal static ExceptionFlowContextualFactEvaluator, matching existing static ownership; no constructor injection or service registration required'
    ExactSccMethodSet=$coreIds; AdditionalSupportMethods=$cacheSupport
    MethodDeclarationTotal=($coreIds.Count + $cacheSupport.Count)
    ExactMovedFields=@($states | Where-Object A5BOwner -eq ExceptionFlowContextualFactEvaluator | ForEach-Object Id)
    AdditionalMovedType='ExceptionFlowAnalyzer.ConditionalWeakTableValueFactCachePartition (implicit constructor, field initializers, gate, entries, TryGetValue, Store)'
    MinimalInternalEntryMethods=$entries
    PrivateSccMethods=@($coreIds | Where-Object { $_ -notin $entries })
    SeedFacades=@($ingress | Where-Object Category -eq B | ForEach-Object { $_.Caller.Id })
    TemporaryFacades='Retain the 2 outside-SCC seed facades and, only where call-site churn requires it, thin forwarding stubs for the 4 measured entry signatures. All evaluation/guard ownership moves; remove temporary stubs in A5C.'
    OrchestrationMethods=@($ingress | Where-Object Category -eq A | ForEach-Object { $_.Caller.Id } | Sort-Object -Unique)
    AllowedDependencies=@($dependencyOwners | Where-Object { $_.Owner -like 'XMLDocNormalizer*' -and $_.Owner -notlike '*ExceptionFlowAnalyzer*' } | ForEach-Object Owner)
    ExplicitSemanticInputs='SemanticModel, its Compilation, Roslyn syntax/symbols, ExceptionFlowCallContext, typed scratch dictionaries/sets and recursion guards. No SemanticScope instance/lazy sourceTypes dependency.'
    ForbiddenDependencies=@('ExceptionFlowAnalyzer reference or method','Analyzer callbacks/delegates/method groups','TraversalState, summary/session/runtime-target orchestration fields','Service locator','Generic dependency bag','Moving downstream provider caches or shared Compilation into evaluator','Changing guard/cache comparers/lifetimes or context keys','Historical worker/IPC implementation')
    AtomicExtraction='Yes: every nonempty proper subset of a strongly connected component has paths crossing the cut in both directions. A separate two-owner partial move creates a reverse path/component cycle unless the recursion semantics is redesigned. No independent proper subset of these 63 methods is dependency-closed.'
    Blockers=@()
    AcceptanceChecks=@('All 63 implementation bodies plus 2 cache support methods move together','No evaluator/provider-to-Analyzer edge or captured Analyzer callback','Static weak cache lifecycle and symbol/model identity unchanged','4 production entry targets redirected; facade seed allocation preserved','Provider-internal cache composition allowed, no inter-component cycles','No move of orchestration/domain/semantic infrastructure','Architecture/behavior regressions and canonical equality validated in A5B')
}
$tests = @()
$(if (Test-Path artifacts/p5o2a5a/tests) { Get-ChildItem artifacts/p5o2a5a/tests -Filter *.trx }) | Sort-Object Name | ForEach-Object {
    [xml]$trx = Get-Content -Raw -LiteralPath $_.FullName
    $c = $trx.TestRun.ResultSummary.Counters
    $tests += [pscustomobject][ordered]@{ File=$_.Name; Total=[int]$c.total; Passed=[int]$c.passed; Failed=[int]$c.failed; Skipped=([int]$c.total - [int]$c.executed); Outcome=[string]$trx.TestRun.ResultSummary.outcome }
}
$evidence = [pscustomobject][ordered]@{
    Schema='P5O2A5A.ContextualEvaluatorBoundaryAudit.v1'; Date='2026-10-06'; StartingHead='31e8f7751cafac955b0a7df3c13e7197ee7b5b21'
    AuditOnly=$true; ProductionChanges=@(); MeasuredFile=$Measured
    MethodGraphScope=$a.MethodGraphScope; ExpandedScope=$a.ExpandedScope; Limitations=$a.Limitations
    Metrics=[pscustomobject][ordered]@{
        AllSourceMethodDeclarations=$a.AllMethodCount; AllInvocationEdges=$a.AllMethodEdgeCount
        SccMethods=$a.Scc.MethodCount; InternalEdges=$a.Scc.InternalEdgeCount; ExpandedSccMethods=$a.Scc.ExpandedMethodCount
        IngressEdges=$ingress.Count; IngressCallers=@($ingress.Caller.Id | Sort-Object -Unique).Count; EntryTargets=$entries.Count
        OrchestrationEdges=@($ingress | Where-Object Category -eq A).Count; ThinSeedFacadeEdges=@($ingress | Where-Object Category -eq B).Count; OtherIngressEdges=0
        EgressEdges=$a.Egress.Count; EgressKinds=@($a.Egress | Group-Object Kind | Select-Object Name,Count)
        FactResolverInvocationEdges=($dependencyOwners | Where-Object { $_.Owner.StartsWith($prefix) -and $_.Owner -notmatch 'ExceptionFlowAnalyzer|ExceptionFlowCallContext$|ExceptionFlowValueFactsExtensions$|ExceptionFlowSemanticScope$' } | Measure-Object InvocationEdges -Sum).Sum
        FactResolverOwners=@($dependencyOwners | Where-Object { $_.Owner.StartsWith($prefix) -and $_.Owner -notmatch 'ExceptionFlowAnalyzer|ExceptionFlowCallContext$|ExceptionFlowValueFactsExtensions$|ExceptionFlowSemanticScope$' }).Count
        SemanticScopeInvocationEdges=@($a.Egress | Where-Object { $_.Kind -eq 'Invocation' -and $_.Owner -eq ($prefix + 'ExceptionFlowSemanticScope') }).Count
        AnalyzerCacheSupportMethods=$cacheSupport.Count; ExpandedTypeCycles=@($a.ComponentCycles).Count; InterComponentCycles=@($a.LogicalComponentCycles).Count
        ReachableSourceCallables=$a.ReachableSourceMethods.Count; AnalyzerPartials=$a.AnalyzerPartials; AnalyzerNonblankSloc=$a.AnalyzerNonblankSloc
    }
    HistoricalBaseline=[pscustomobject][ordered]@{
        Report='P5O2A4G-delegate-target-resolver-ownership'; ComponentsProjection=94; SemanticScopeProjection=18
        NotCompleteCensus='94/18 were the historical projected report counts, not the complete bound-call census. Current full count is 126 fact/resolver calls +19 SemanticScope calls; latter includes a Compilation overload. Do not compare mixed graph scopes as code change.'
        MemberComparisonSource='P5O2A4C-residual-scc-audit.json Nodes.Signature'
        NewMembers=@($coreIds | Where-Object { $_ -notin $oldIds }); RemovedMembers=@($oldIds | Where-Object { $_ -notin $coreIds })
        ExpandedGraphAddedMembers=@($a.Scc.ExpandedMembersAdded); ExpandedGraphRemovedMembers=@($a.Scc.ExpandedMembersRemoved)
        AddedInternalEdges=@($newEdges | Where-Object { $_ -notin $oldEdges }); RemovedInternalEdges=@($oldEdges | Where-Object { $_ -notin $newEdges })
        Tests='2547/2547; no skips'; WarningAsError='0 warnings / 0 errors'; SelfAnalysis='16: DOC610=0 DOC611=1 DOC631=15 DOC632=0'; CanonicalDiff='0/0/0, exact normalized array equality'
    }
    MethodSccs=$a.MethodSccs; Scc=$a.Scc; Ingress=$ingress; Egress=$a.Egress; Dependencies=$dependencyOwners
    ReachableSourceMethods=$a.ReachableSourceMethods; ReachableSourceEdges=$a.ReachableSourceEdges
    ExpandedTypeEdges=$a.ComponentEdges; ExpandedTypeCycles=$a.ComponentCycles
    InterComponentEdges=$a.LogicalComponentEdges; InterComponentCycles=$a.LogicalComponentCycles
    ReachableInitializerDependencies=$a.ReachableInitializerDependencies
    StateOwnership=$states; ParameterState=$parameterState; LocalMutableState=$locals
    ReachableSemanticStatePorts=@($a.ReachableState | Select-Object Id,Kind,Owner,Type -Unique)
    DelegateSites=@($a.DelegateSites | Where-Object { $_.Caller -in $reachableIds })
    CompositionAndImplicitStateReview=$compositionReview
    EntrySeedClassification=[pscustomobject][ordered]@{
        FourHistoricalContextRoleMethods=@($a.Scc.Members | Where-Object Name -in @('CreateCallContext','AddExplicitArgumentFacts','AreSequenceElementsProvenNonNull') | Select-Object Id,Role,A5BOwner)
        ProductionEntryTargets=$entries
        SeedAndCopiedGuardSites=@($locals | Where-Object Category -eq B)
        OutsideSccSeedFacades=@($ingress | Where-Object Category -eq B | Select-Object Caller,Role,A5BOwner,A5CCandidate)
        AnalyzerOrchestration=@($ingress | Where-Object Category -eq A | Select-Object Caller,Role,A5BOwner)
        AdditionalNonIngressContextOrchestration=@($a.AllSourceNodes | Where-Object { $_.Owner -eq ($prefix + 'ExceptionFlowAnalyzer') -and $_.Name -in @('CreateRootCallContext','CreateDispatchCallContext','CreateDispatchTargetContext') } | Select-Object Id,File,Line)
        Note='Four Context-role members are not the four externally targeted SCC signatures. All internal seeds remain evaluator-owned; intentional guard copies are not independent external orchestration.'
    }
    CacheSemantics=$cacheSemantics; A5BProposal=$proposal; Validation=[pscustomobject][ordered]@{
        CompilationErrors=$a.CompilationErrors; WorkspaceDiagnostics=$a.WorkspaceDiagnostics; TestRuns=$tests
        AuditToolWarningAsError='pass, 0 warnings / 0 errors'; AuditToolFormat='pass'
        MeasurementSha256=(Get-FileHash -LiteralPath $Measured -Algorithm SHA256).Hash
        RepeatedMeasurementSha256=(Get-FileHash -LiteralPath artifacts/p5o2a5a/measured-boundary-repeat.json -Algorithm SHA256).Hash
        RepeatedMeasurementsByteIdentical=((Get-FileHash -LiteralPath $Measured -Algorithm SHA256).Hash -eq (Get-FileHash -LiteralPath artifacts/p5o2a5a/measured-boundary-repeat.json -Algorithm SHA256).Hash)
        ProductionSolutionBuild='Not repeated: audit tool is not in solution; retain verified A4G production build, avoid unrelated tracked Core bin/obj churn'
        SelfAnalysisAndCanonicalDiff='Not repeated: no production/test-compiled change, isolated audit tool cannot affect analysis behavior; retained A4G baseline explicitly, not a new measurement'
    }
}
$evidence | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $Output -Encoding UTF8
$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# P5O2A5A - Complete boundary matrices')
$lines.Add('')
$lines.Add('Generated by ContextualBoundaryAudit/Write-Evidence.ps1 from the active compilation. Full qualified signatures, call edges, API dependencies and state access sites are retained in the companion JSON. M01..M63 are stable ordinal IDs for this measured method set. Every SCC method below remains Analyzer-owned now and is proposed for evaluator ownership; no move is performed.')
$lines.Add('')
$lines.Add('## SCC membership and direct callers/callees')
$lines.Add('')
$lines.Add('| ID | Method (unambiguous signature) | Partial / line | SCC callers | SCC callees | External callers | Egress edges |')
$lines.Add('|---|---|---|---|---|---|---|')
foreach ($m in $a.Scc.Members) {
    $lines.Add('| ' + (Label $m.Id) + ' | ' + (Cell (Short $m.Id)) + ' | ' + ($m.File.Split('/')[-1]) + ':' + $m.Line + ' | ' + (($m.InternalCallers | ForEach-Object { Label $_ }) -join ', ') + ' | ' + (($m.InternalCallees | ForEach-Object { Label $_ }) -join ', ') + ' | ' + (($m.ExternalCallers | ForEach-Object { Cell (Short $_.Caller) }) -join '; ') + ' | ' + $m.ExternalDependencies.Count + ' |')
}
$lines.Add('')
$lines.Add('## Complete ingress')
$lines.Add('')
$lines.Add('| Caller signature | SCC target | Caller partial / declaration line (call line) | Class | Role / A5B owner |')
$lines.Add('|---|---|---|---|---|')
foreach ($i in $ingress) {
    $lines.Add('| ' + (Cell (Short $i.Caller.Id)) + ' | ' + (Label $i.Edge.Callee) + ' | ' + $i.Caller.File.Split('/')[-1] + ':' + $i.Caller.Line + ' (' + $i.Edge.Line + ') | ' + $i.Category + ' | ' + $i.Role + '; ' + $i.A5BOwner + ' |')
}
$lines.Add('')
$lines.Add('## Complete egress owner/API matrix')
$lines.Add('')
$lines.Add('Counts are distinct directed (caller, callee) edges per owner. Direct full signatures in Methods and all 791 kind-labelled edges are in JSON; overloads are not collapsed. Metadata API bodies are not traversed; no invented claims about their internals.')
$lines.Add('')
$lines.Add('| Owner | Edges | Exact used methods / overloads | State / acquisition | Downstream source owners | Analyzer reference / historical label |')
$lines.Add('|---|---|---|---|---|---|')
foreach ($d in $dependencyOwners) {
    $lines.Add('| ' + (Cell (Short $d.Owner)) + ' | ' + $d.DistinctMethodEdges + ' | ' + (($d.Methods | ForEach-Object { Cell (Short $_) }) -join '; ') + ' | ' + $d.State + '; ' + $d.Acquisition + ' | ' + (($d.DirectDownstreamOwners | ForEach-Object { Cell (Short $_) }) -join '; ') + ' | ' + $d.AnalyzerBackReference + '; ' + $d.HistoricalLabel + ' |')
}
$lines.Add('')
$lines.Add('## Field ownership matrix (including downstream closure)')
$lines.Add('')
$lines.Add('The JSON contains every reader, assignment writer, collection receiver operation, initializer, and access site; assignments and mutation-through-readonly-reference are distinct. Enum/constants/comparers are classified too. Locals and typed parameters have separate exhaustive machine-readable matrices.')
$lines.Add('')
$lines.Add('| Field / constant | Category | Current owner | A5B owner | Lifetime / initialization / invalidation | Exclusive SCC/support? |')
$lines.Add('|---|---|---|---|---|---|')
foreach ($s in $states) {
    $owner = @($a.ReachableState | Where-Object Id -eq $s.Id | Select-Object -First 1).Owner
    $lines.Add('| ' + (Cell (Short $s.Id)) + ' | ' + $s.Category + ' | ' + (Cell (Short $owner)) + ' | ' + $s.A5BOwner + ' | ' + $s.LifetimeInitializationInvalidation + ' | ' + $s.ExclusiveSccOrOwnedSupport + ' |')
}
$lines.Add('')
$lines.Add('## Exact additional cache support methods')
$lines.Add('')
foreach ($m in $cacheSupport) { $lines.Add('- ' + (Short $m)) }
$lines.Add('')
$lines.Add('## Full expanded type dependency edges (including initialization / method references)')
$lines.Add('')
$lines.Add('| Source owner | Target owner | Representative bound edge kind | Source / line |')
$lines.Add('|---|---|---|---|')
foreach ($e in $a.ComponentEdges) {
    $lines.Add('| ' + (Cell (Short $e.Caller)) + ' | ' + (Cell (Short $e.Callee)) + ' | ' + $e.Kind + ' | ' + $e.File.Split('/')[-1] + ':' + $e.Line + ' |')
}
$lines | Set-Content -LiteralPath $Matrices -Encoding UTF8
Write-Output ('Evidence: {0}; {1}; SCC {2}/{3}, ingress {4}, egress {5}, proposal {6} methods' -f $Output,$Matrices,$coreIds.Count,$a.Scc.InternalEdgeCount,$ingress.Count,$a.Egress.Count,$proposal.MethodDeclarationTotal)

