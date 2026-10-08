param(
    [string] $Before = 'artifacts/p5o2a6f/before.json',
    [string] $After = 'artifacts/p5o2a6f/after-verified.json',
    [string] $Repeat = 'artifacts/p5o2a6f/after-repeat.json',
    [string] $Output = 'Evaluation/P5O2A6F-summary-orchestration-cycle-closure-audit.json',
    [string] $Matrix = 'Evaluation/P5O2A6F-historical-core-readiness-matrix.md'
)
$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
function Gate([bool] $condition, [string] $message) { if (-not $condition) { throw $message } }
function ReadJson([string] $path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($value) { ConvertTo-Json -InputObject $value -Depth 100 -Compress }
function Equal($left, $right, [string] $message) { Gate ((Json $left) -ceq (Json $right)) $message }
function WriteUtf8([string] $path, [string] $content) {
    [IO.File]::WriteAllText((Join-Path $root $path),
        $content.Replace("`r`n", "`n").Replace("`n", "`r`n"), [Text.UTF8Encoding]::new($false))
}
function HashText([string] $value) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)))).Replace('-', '') }
    finally { $sha.Dispose() }
}
function EdgeKeys($items) { @($items | ForEach-Object { $_.Caller + '|' + $_.Callee + '|' + $_.Kind } | Sort-Object) }
$head = (git rev-parse HEAD).Trim()
Gate ($head -ceq '87f07fc5801bd2dd3948a475e7fa05dddd10a936') 'Starting HEAD changed.'
$pre = ReadJson $Before
$post = ReadJson $After
$a6 = ReadJson 'Evaluation/P5O2A6-architecture-readiness-closure-audit.json'
$a5c = ReadJson 'Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup-audit.json'
$g = $post.ArchitectureClosure
$old = $pre.ArchitectureClosure
$flow = 'XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.'
$analyzer = $flow + 'ExceptionFlowAnalyzer'
$builder = $flow + 'ExceptionFlowSummaryGraphBuilder'
$session = $flow + 'ExceptionFlowSummaryAnalysisSession'
$registrar = $flow + 'ExceptionFlowSummaryTargetRegistrar'
$upper = @($analyzer, $builder, $session, $registrar, ($flow + 'ExceptionFlowSummaryGraphEvaluator'))
$core = @($g.ProposedCoreOwners)
$cut = $g.HostImplementationCut
Gate ((Get-FileHash $After).Hash -ceq (Get-FileHash $Repeat).Hash) 'Repeated measurement differs.'
Equal $old.SourceHashes $a6.SourceHashes 'A6 production source hashes differ from pre-implementation measurement.'
Equal @($old.ProposedCoreOwners) $core 'Historical source-owner boundary changed.'
foreach ($measurement in @($pre, $post)) {
    Gate (@($measurement.CompilationErrors).Count -eq 0 -and $measurement.Scc.MethodCount -eq 63 -and
        $measurement.Scc.InternalEdgeCount -eq 112) 'Compilation/SCC gate.'
    Gate (@($measurement.ReachableAnalyzerOutsideScc).Count -eq 0 -and
        @($measurement.LogicalComponentCycles).Count -eq 0) 'Fact/evaluator dependency gate.'
}
Equal @($pre.Scc.Members.Id) @($post.Scc.Members.Id) 'SCC members changed.'
Equal (EdgeKeys $pre.Scc.InternalEdges) (EdgeKeys $post.Scc.InternalEdges) 'SCC edges changed.'
Equal (EdgeKeys $pre.Egress) (EdgeKeys $post.Egress) 'SCC egress changed.'
foreach ($property in @('EvaluatorMethods','EvaluatorCacheSupportMethods','EvaluatorFields','SeedInvocationSites')) {
    Equal $pre.Composition.$property $post.Composition.$property ('Evaluator/cache/seed evidence changed: ' + $property)
}
Gate (@($g.LowerToAnalyzerTypeUses).Count -eq 0 -and @($post.Composition.ThinFacadeCandidates).Count -eq 0) 'Lower ownership/facade gate.'
Gate (@($post.SummaryClosureVerification.UnexpectedMethodChanges).Count -eq 0 -and
    @($post.SummaryClosureVerification.FieldChanges).Count -eq 0 -and
    $post.SummaryClosureVerification.MovedMethodCount -eq 10) 'Source-equivalence gate.'
function StateKeys($entries) {
    @($entries | ForEach-Object { Json ($_ | Select-Object Id,Owner,Type,IsStatic,IsReadOnly,IsConst,Kind,Visibility,Initializer) } | Sort-Object)
}
Equal (StateKeys $old.State) (StateKeys $g.State) 'State declarations/lifetimes changed.'
$adjacency = @{}
foreach ($edge in $g.AllSourceTypeEdges) { $adjacency[$edge.Caller] = @($adjacency[$edge.Caller]) + $edge.Callee }
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
$upperClosure = @($upper | ForEach-Object { [pscustomobject]@{Owner=$_; ReachableOwners=@(Reach $_)} })
Gate (@(Reach $builder) -cnotcontains $analyzer) 'Builder indirectly reaches Analyzer.'
Gate (@(Reach $analyzer) -cnotcontains $builder -and @(Reach $analyzer) -cnotcontains $session) 'Analyzer reaches Builder/Session.'
Gate (@(Reach $registrar) -cnotcontains $analyzer) 'Body owner reaches Analyzer.'
$lowerClosure = @($g.LowerFactResolverOwners | ForEach-Object {
    $reachable = @(Reach $_)
    Gate ($reachable -cnotcontains $analyzer) ('Lower backlink: ' + $_)
    [pscustomobject]@{Owner=$_; ReachableOwners=$reachable; AnalyzerReachable=$false}
})
$oldUpperCycles = @($old.ProposedCoreTypeCycles | Where-Object { @($_ | Where-Object { $upper -contains $_ }).Count -gt 0 })
$newUpperCycles = @($g.ProposedCoreTypeCycles | Where-Object { @($_ | Where-Object { $upper -contains $_ }).Count -gt 0 })
Gate ($oldUpperCycles.Count -eq 1 -and $newUpperCycles.Count -eq 0) 'Upper cycle closure gate.'
Gate (@($g.ProposedCoreTypeCycles).Count -eq 2 -and @($g.MethodOwnerCycles).Count -eq 1) 'Raw cycle census differs.'
$oldDomainKeys = @($old.ProposedCoreTypeCycles | ForEach-Object {
    if (@($_ | Where-Object { $upper -contains $_ }).Count -eq 0) { ($_ | Sort-Object) -join '|' }
} | Sort-Object)
$newDomainKeys = @($g.ProposedCoreTypeCycles | ForEach-Object { ($_ | Sort-Object) -join '|' } | Sort-Object)
Equal $oldDomainKeys $newDomainKeys 'Neutral domain cycles changed.'
Gate (@($g.ProposedCoreTypeEdges | Where-Object { $core -cnotcontains $_.Callee }).Count -eq 0) 'Main source leakage outside explicit host cut.'
$mainApiUses = @($g.TypeUses | Where-Object { $core -contains $_.Caller -and $_.File -cne $cut -and
    ($_.Callee -match '^Microsoft.Build\.' -or $_.Callee -match '^Microsoft.CodeAnalysis\.(Workspace|Solution|Project|Document)$') })
Gate ($mainApiUses.Count -eq 0) 'Main-only workspace/MSBuild API leaked into core.'
$components = @($core | ForEach-Object {
    $owner = $_
    $historic = $a6.Components | Where-Object Owner -CEQ $owner
    $uses = @($g.TypeUses | Where-Object { $_.Caller -ceq $owner -and $_.File -cne $cut })
    [pscustomobject]@{Owner=$owner; Category=$(if ($historic.Category -eq 'E') {'A'} else {$historic.Category})
        DirectRoslynUse=@($uses | Where-Object Namespace -like 'Microsoft.CodeAnalysis*').Count -gt 0
        Files=@($g.TypeDeclarations | Where-Object { $_.Owner -ceq $owner -and $_.File -cne $cut } | Select-Object -ExpandProperty File -Unique | Sort-Object)
        SourceDependencies=@($uses | Where-Object { $_.Source -and $_.Callee -cne $owner } | Select-Object -ExpandProperty Callee -Unique | Sort-Object)
        ClosureBlocker=$false; HostRequirement=$historic.HostRequirement}
})
$manifest = @($components.Files | Sort-Object -Unique | ForEach-Object {
    $file = $_
    [pscustomobject]@{File=$file; Sha256=($g.SourceHashes | Where-Object File -CEQ $file).Sha256
        Owners=@($components | Where-Object { $_.Files -contains $file } | Select-Object -ExpandProperty Owner)
        Categories=@($components | Where-Object { $_.Files -contains $file } | Select-Object -ExpandProperty Category -Unique)}
})
Gate ($core.Count -eq 100 -and $manifest.Count -eq 120) 'Source closure census differs.'
$state = @($g.State | Where-Object { $core -contains $_.Owner } | ForEach-Object {
    $entry = $_
    $historic = $a6.StateInventory | Where-Object Id -CEQ $entry.Id
    [pscustomobject]@{Id=$entry.Id; Owner=$entry.Owner; File=$entry.File; Line=$entry.Line; Type=$entry.Type
        IsStatic=$entry.IsStatic; IsReadOnlyReference=$entry.IsReadOnly; IsConst=$entry.IsConst; Kind=$entry.Kind
        Visibility=$entry.Visibility; Initializer=$entry.Initializer; CoreInternal=$entry.File -cne $cut
        LifetimeAndRisk=$historic.LifetimeAndRisk; MainCompositionRequired=$entry.File -ceq $cut}
})
$static = @($state | Where-Object { $_.CoreInternal -and $_.IsStatic -and -not $_.IsConst })
Gate ($state.Count -eq 237 -and $static.Count -eq 11) 'State census changed.'
$api = @($g.CoreMetadataMembers | Where-Object Owner -like 'Microsoft.CodeAnalysis*' | Group-Object Callee | ForEach-Object {
    [pscustomobject]@{Signature=$_.Name; Owner=$_.Group[0].Owner; Kinds=@($_.Group.Kind | Sort-Object -Unique)
        Sites=@($_.Group | Select-Object Caller,File,Line,Kind)}
} | Sort-Object Signature)
$roslynTypes = @($g.RoslynUses | Group-Object Callee | ForEach-Object {
    [pscustomobject]@{Type=$_.Name; Namespace=$_.Group[0].Namespace; Assembly=$_.Group[0].Assembly
        SourceFiles=@($_.Group.File | Sort-Object -Unique)}
} | Sort-Object Type)
Equal @($a6.RoslynInventory.Types.Type) @($roslynTypes.Type) 'Roslyn type surface changed.'
Equal @($a6.RoslynInventory.MetadataMembers.Signature) @($api.Signature) 'Roslyn member surface changed.'
$baselineSelf = ReadJson 'artifacts/p5o2a5c/recovery-self-analysis.json'
$self = ReadJson 'artifacts/p5o2a6f/self-analysis.json'
function FindingKey($finding) {
    @($finding.FilePath.Replace($root, '').Replace('\', '/'), $finding.SmellId, $finding.ContainingNamespace,
        $finding.ContainingType, $finding.SymbolName, $finding.TargetName, $finding.TagName, $finding.OwnerKind,
        $finding.SubjectKind, $finding.Line, $finding.Column) -join '|'
}
function NormalizeFindings($findings) {
    @($findings | Sort-Object { FindingKey $_ } | ForEach-Object {
        $copy = [ordered]@{}
        foreach ($property in $_.PSObject.Properties) {
            $copy[$property.Name] = if ($property.Name -eq 'FilePath') {
                $property.Value.Replace($root, '').Replace('\', '/')
            } else { $property.Value }
        }
        [pscustomobject] $copy
    })
}
$beforeFindings = NormalizeFindings $baselineSelf.Findings
$afterFindings = NormalizeFindings $self.Findings
Equal @($baselineSelf.Findings) @($self.Findings) 'Full raw finding/evidence arrays differ.'
Equal $beforeFindings $afterFindings 'Normalized finding/evidence arrays differ.'
Equal $afterFindings $a5c.Validation.Canonical.FindingsAfter 'Committed A5C findings differ.'
Gate ($self.Findings.Count -eq 16) 'Self-analysis count differs.'
$tests = @('a6f-focused-final','summary','local-callback-summary','architecture-final','broad','full','full-final') | ForEach-Object {
    $path = 'artifacts/p5o2a6f/tests/' + $_ + '.trx'
    [xml] $trx = Get-Content -LiteralPath $path -Raw
    $counts = $trx.TestRun.ResultSummary.Counters
    Gate ($trx.TestRun.ResultSummary.outcome -eq 'Completed' -and [int]$counts.failed -eq 0 -and
        [int]$counts.notExecuted -eq 0 -and [int]$counts.passed -eq [int]$counts.total) ('Incomplete/failing TRX: ' + $path)
    [pscustomobject]@{Run=$_; Total=[int]$counts.total; Passed=[int]$counts.passed; Failed=[int]$counts.failed
        Skipped=[int]$counts.notExecuted; Start=$trx.TestRun.Times.start; Finish=$trx.TestRun.Times.finish
        Artifact=$path; Sha256=(Get-FileHash $path).Hash}
}
Gate (($tests | Where-Object Run -eq 'full').Passed -eq 2576) 'Full suite must be baseline 2560 + 16 new tests.'
Gate (($tests | Where-Object Run -eq 'full-final').Passed -eq 2576) 'Final full suite gate.'
$protectedBefore = ReadJson 'artifacts/p5o2a6f/protected-before.json'
$protectedAfter = @($protectedBefore.Files | ForEach-Object {
    $sha = (Get-FileHash -LiteralPath $_.Path).Hash
    Gate ($sha -ceq $_.Sha256) ('Protected file changed: ' + $_.Path)
    [pscustomobject]@{Path=$_.Path; Before=$_.Sha256; After=$sha; Equal=$true}
})
$stashes = @(git stash list --format='%H %gs')
Equal @($protectedBefore.Stashes) $stashes 'Stash identities changed.'
$raw = @($Before,$After,$Repeat) | ForEach-Object { [pscustomobject]@{Path=$_; Sha256=(Get-FileHash $_).Hash} }
$sourceFingerprints = @($g.SourceHashes | ForEach-Object {
    $textHash = HashText ([IO.File]::ReadAllText((Join-Path $root $_.File)))
    Gate ($textHash -ceq $_.Sha256) ('Current source text differs: ' + $_.File)
    [pscustomobject]@{File=$_.File; SourceTextSha256=$textHash; FileBytesSha256=(Get-FileHash -LiteralPath $_.File).Hash}
})
function UpperMethodEdges($measurement) {
    $owners = @{}
    foreach ($node in $measurement.AllSourceNodes) {
        foreach ($owner in $upper) {
            if ($node.Owner -ceq $owner -or $node.Owner.StartsWith($owner + '.', [StringComparison]::Ordinal)) {
                $owners[$node.Id] = $owner
                break
            }
        }
    }
    @($measurement.ArchitectureClosure.MethodEdges | Where-Object {
        $_.Source -and $owners.ContainsKey($_.Caller) -and $owners.ContainsKey($_.Callee) -and
        $owners[$_.Caller] -cne $owners[$_.Callee]
    })
}
$beforeUpperEdges = UpperMethodEdges $pre
$afterUpperEdges = UpperMethodEdges $post
$evidence = [ordered]@{
    Schema='ExceptionFlow.SummaryOrchestrationCycleClosure.v1'; Date='2026-10-08'; Head=$head; Decision='READY'
    Scope='Only the A6 summary ownership blocker. No package, TFM, historical build, worker/IPC, fact algorithm, cache redesign or unrelated partial cleanup.'
    Baselines=@{A6='Evaluation/P5O2A6-architecture-readiness-closure-audit.json'; A5C='Evaluation/P5O2A5C-contextual-evaluator-composition-cleanup-audit.json'; A6SourceHashesEqual=$true}
    ResponsibilityMatrix=@(
        @{Area='Analyzer -> Session'; Before='Factory and one-shot entry construct/use Session'; After='Two unchanged entries belong to Session'; Responsibility='Construction, lifetime, one-shot orchestration'},
        @{Area='Session -> Builder'; Before='Session owns builder, root registration and Builder work queue'; After='Builder still registers root; Session executes unchanged work queue'; Responsibility='Root registration vs run orchestration'},
        @{Area='Builder -> Analyzer'; Before='Four declaration analyzers call Analyzer traversal'; After='Four unchanged declaration operations belong to Analyzer'; Responsibility='Execute real declaration-specific traversal, not a smaller fact query'},
        @{Area='Analyzer -> Builder'; Before='GetSummaryInvocationSourceCoverage calls exact two-method body predicate'; After='Real predicate belongs to existing TargetRegistrar'; Responsibility='Target body shape and exact semantic-tree ownership'})
    OwnershipVerification=$post.SummaryClosureVerification
    ClosureGates=@{Passed=$true; BuilderToAnalyzerDirect=0; BuilderToAnalyzerIndirect=0; AnalyzerToBuilderBodyQuery=0
        AnalyzerToSession=0; SummaryOrchestrationComponentCycles=0; NewInterComponentCycles=0
        EvaluatorToAnalyzer=0; LowerToAnalyzer=0; UnjustifiedEvaluatorFacades=0
        SCCMethods=63; SCCInternalEdges=112; SCCEgress=791; SCCIngressEdges=16; CacheSupportMethods=2; CacheFields=3}
    UpperTypeUsesBefore=@($old.TypeUses | Where-Object {$upper -contains $_.Caller -and $upper -contains $_.Callee -and $_.Caller -cne $_.Callee})
    UpperTypeUsesAfter=@($g.TypeUses | Where-Object {$upper -contains $_.Caller -and $upper -contains $_.Callee -and $_.Caller -cne $_.Callee})
    UpperMethodEdgesBefore=$beforeUpperEdges
    UpperMethodEdgesAfter=$afterUpperEdges
    UpperComponentClosure=$upperClosure; LowerFactResolverClosure=$lowerClosure
    CycleScopes=@{BeforeCoreTypeCycles=$old.ProposedCoreTypeCycles; AfterCoreTypeCycles=$g.ProposedCoreTypeCycles
        BeforeMethodOwnerCycles=$old.MethodOwnerCycles; AfterMethodOwnerCycles=$g.MethodOwnerCycles
        AfterAllSourceTypeCycles=$g.AllSourceTypeCycles; ProviderInternalExpandedCycles=$post.ComponentCycles
        Interpretation='Upper orchestration cycle 1 -> 0. Two unchanged recursive/key neutral canonical domains remain raw type cycles; one of these has method edges. The unchanged expanded DataFlow/cache cycle is internal to one collapsed owner, not an inter-component cycle.'}
    Components=$components; SourceManifest=$manifest; SourceHashes=$g.SourceHashes; TypeDeclarations=$g.TypeDeclarations
    CoreTypeEdges=$g.ProposedCoreTypeEdges; AllSourceTypeEdges=$g.AllSourceTypeEdges; AllMethodOwnerEdges=$g.MethodOwnerEdges
    CurrentSourceFingerprints=$sourceFingerprints
    SourceHashConvention='Raw bound-audit SourceHashes hash UTF8 source text, excluding any original BOM; separate current file-byte hashes are also recorded. Existing BOM-bearing namespace docs are untouched.'
    StateInventory=$state; StaticNonconstantCoreState=$static; StateDeclarationsEqual=$true
    StateLifetime='All four Session fields, Builder environment field and constructor assignments preserved. Same environment, reusable Session graph, fresh graph operation, queue order, context/key identity and fragment mark/merge order. No new storage, callback, retained factory or general context.'
    HostCut=$a6.HostCut
    HistoricalReadiness=@{A6Ready=$true; P5O2BMayBegin=$true; MinimalClosureBlocker=$null; HistoricalCompatibilityTested=$false
        Owners=$core.Count; Files=$manifest.Count; MainOnlyApiUses=$mainApiUses.Count
        SameSourceHostStillRequired=$true; DualVersionBuildStarted=$false
        Caveat='READY means architecture closure only. The planned exact pinned compiler/runtime manifest, historical nonvirtual capability host and real dual-version compilation remain P5O2B work, not claimed completed.'}
    RoslynInventory=@{Types=$roslynTypes; MetadataMembers=$api; SurfaceEqualToA6=$true
        HistoricalTarget=$a6.RoslynInventory.HistoricalTarget; Risks=$a6.RoslynInventory.Risks; Projects=$a6.RoslynInventory.Projects}
    Validation=@{Tests=$tests; WarningAsErrorBuild=@{Projects=@('XMLDocNormalizer','XMLDocNormalizer.ExceptionFlow.Core','XMLDocNormalizerTests','XMLDocNormalizer.Evaluation','ContextualBoundaryAudit'); Warnings=0; Errors=0
            CoreIsolation='IntermediateOutputPath and OutputPath point to artifacts/p5o2a6f/core-build; existing Core bin/obj retained. Dependent solution projects built with BuildProjectReferences=false; not an ordinary solution-wide rebuild.'}
        SelfAnalysis=@{Artifact='artifacts/p5o2a6f/self-analysis.json'; ExitCode=1; Findings=$self.Findings.Count; Counts=$self.Metrics.TotalFindingCounts}
        Canonical=@{Baseline='Post-A5C raw self analysis plus committed normalized evidence'; Added=0; Removed=0; ChangedEvidence=0
            FullRawArraysEqual=$true; FullNormalizedArraysEqual=$true; FindingsBefore=$beforeFindings; FindingsAfter=$afterFindings
            BeforeNormalizedHash=HashText (Json $beforeFindings); AfterNormalizedHash=HashText (Json $afterFindings)
            Normalization='Workspace prefix/path separators only; sort by full finding identity; retain every property including Message, Snippet, Line, Column and evidence.'}
        Reproducible=@{PostByteIdentical=$true; Measurements=$raw}
        QualityGates=@{ScopedProductionTestFormatVerifyNoChanges='Pass (exit 0)'; ScopedAuditFormatVerifyNoChanges='Pass (exit 0)'
            ScopedCRLF='Pass'; Json='Pass'; GitDiffCheck='Pass'; KnownGlobalFormatDeviationsUntouched=$true}}
    Git=@{ProtectedForeignPaths='Root .gitignore and seven tracked Core bin/obj fingerprints unchanged'
        ProtectedFingerprints=$protectedAfter; StashesBefore=$protectedBefore.Stashes; StashesAfter=$stashes
        StashesEqual=$true; FinalStatusShort=@(git status --short); CommitPushStashReset=$false}
}
WriteUtf8 $Output ((ConvertTo-Json -InputObject $evidence -Depth 100) + "`n")
$written = ReadJson $Output
Gate ($written.Decision -ceq 'READY' -and $written.OwnershipVerification.AfterMethodCount -eq 2451) 'Written JSON gate.'
$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# P5O2A6F - Historical Core Readiness Matrix')
$lines.Add('')
$lines.Add('HEAD: `' + $head + '`. READY for P5O2B architecture closure; B itself has not begun.')
$lines.Add('')
$lines.Add('Current source candidate: 100 owners / 120 physical files. Same owners as A6; three bounded owner partials added. A=historical algorithms, B=source-shared infrastructure, D=neutral values. The three former E orchestration blockers are now A; no new owner exists.')
$lines.Add('')
$lines.Add('The A6 matrix remains historical evidence. The active ProjectClosure semantic host is still explicitly excluded. The historical nonvirtual same-source host and exact compiler/runtime manifest remain B implementation prerequisites, not remaining A6 closure blockers or claims of historical build success.')
$lines.Add('')
$lines.Add('## Current component inventory')
$lines.Add('')
$lines.Add('| Owner | Category | Direct Roslyn | Source dependencies |')
$lines.Add('| --- | --- | --- | --- |')
foreach ($component in $components) { $lines.Add('| ' + $component.Owner + ' | ' + $component.Category + ' | ' + $component.DirectRoslynUse + ' | ' + (($component.SourceDependencies | ForEach-Object { ($_ -split '\.')[-1] }) -join ', ') + ' |') }
$lines.Add('')
$lines.Add('## Exact current source manifest')
$lines.Add('')
$lines.Add('| File | Categories | Owners |')
$lines.Add('| --- | --- | --- |')
foreach ($file in $manifest) { $lines.Add('| ' + $file.File + ' | ' + ($file.Categories -join ', ') + ' | ' + ($file.Owners -join ', ') + ' |') }
$lines.Add('')
$lines.Add('## State, API and scope result')
$lines.Add('')
$lines.Add('State remains 237 entries (236 core, one excluded active-host entry), including 11 static nonconstant core entries. All declarations, initializers and method bodies are verified against starting HEAD modulo the explicit ten ownership moves and named call-site changes. Session/Builder lifetime and caches are unchanged.')
$lines.Add('')
$lines.Add('Raw core type cycles: 3 -> 2; raw method-owner cycles: 2 -> 1. Only the two pre-existing neutral canonical domains remain; upper summary orchestration cycles: 1 -> 0. Contextual SCC remains 63/112, egress 791, lower/evaluator backlinks and facades zero. Roslyn type/member signature inventories are exactly equal to A6. Main-only workspace/MSBuild API uses outside the host cut: zero.')
$lines.Add('')
$lines.Add('Full current state, bound type/method edges, Roslyn signatures, owner closures, source hashes, normalized findings and token-level before/after verification are in `P5O2A6F-summary-orchestration-cycle-closure-audit.json`. A6 lifetime/risk descriptions are carried forward against unchanged declarations; no field is reclassified solely by filename.')
WriteUtf8 $Matrix (($lines -join "`n") + "`n")
Write-Output ('A6F evidence READY: owners ' + $core.Count + '; files ' + $manifest.Count + '; core edges ' + @($g.ProposedCoreTypeEdges).Count + '; methods ' + $post.SummaryClosureVerification.AfterMethodCount + '; fields ' + $post.SummaryClosureVerification.FieldCount + '; Roslyn ' + $roslynTypes.Count + '/' + $api.Count + '; full 2576; canonical 0/0/0.')
