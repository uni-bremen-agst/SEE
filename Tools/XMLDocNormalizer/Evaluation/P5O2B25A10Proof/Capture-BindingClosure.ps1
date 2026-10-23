# New read-only Phase-0 audit, NOT a retry or replacement of the failed gate.
# Writes only its new A10 evidence record; never changes a protected baseline.
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$output = Join-Path $PSScriptRoot 'phase0-binding-closure-validation.json'
if (Test-Path -LiteralPath $output) { throw 'Preserve completed closure record.' }
function ReadJson($relative) { Get-Content -LiteralPath (Join-Path $tool $relative) -Raw -Encoding UTF8 | ConvertFrom-Json }
function Hash($path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
function BytesHash([byte[]]$bytes) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($algorithm.ComputeHash($bytes)).Replace('-', '') } finally { $algorithm.Dispose() }
}
function Differences($files) {
    foreach ($f in $files) {
        $actual = if (Test-Path -LiteralPath $f.Path -PathType Leaf) { Hash $f.Path } else { 'MISSING' }
        if ($actual -cne $f.Sha256) { [ordered]@{ Path = $f.Path; Expected = $f.Sha256; Actual = $actual } }
    }
}
$manifest = ReadJson 'artifacts/p5o2b25a9/start.json'
$inventory = ReadJson 'artifacts/p5o2b25a10/test-path-inventory-r1.json'
$old = ReadJson 'Evaluation/P5O2B25A10Proof/Complete-fixture-preflight.json'
$bound = ReadJson 'Evaluation/P5O2B25A10Proof/Supplementary-resolved-bindings-r2.json'
$audit = ReadJson 'Evaluation/P5O2B25A10-finite-orbit-admission-and-projection-audit.json'
$coreDifferences = @(Differences $manifest.Core)
$originalDifferences = @(Differences $manifest.OriginalFiles)
$testDifferences = @($inventory.Files | Where-Object { (Hash (Join-Path $tool $_.Path)) -cne $_.Sha256 })

$oldKeys = @(foreach ($m in $old.WorkerMethods) { foreach ($c in $m.Cases) { foreach ($a in $c.Assertions) {
    if ($a.Classification -eq 'AMBIGUOUS') { $m.Test + '|' + $c.Case + '|' + $a.File.Replace('\','/') + '|' + $a.Line + '|' + $a.Current }
} } })
$newKeys = @($bound.Observations | ForEach-Object { $_.Test + '|' + $_.Case + '|' + $_.File.Replace('\','/') + '|' + $_.Line + '|' + $_.Assertion })
$keyDifferences = @(Compare-Object $oldKeys $newKeys)
$sourceTexts = @{}
$badSpans = @(foreach ($o in $bound.Observations) {
    if (!$sourceTexts.ContainsKey($o.File)) { $sourceTexts[$o.File] = [IO.File]::ReadAllText((Join-Path $tool $o.File)) }
    $actualText = $sourceTexts[$o.File].Substring($o.SpanStart, $o.Assertion.Length)
    if (($actualText -replace '\s+','') -cne ($o.Assertion -replace '\s+','')) { $o.Observation }
})
$counts = [ordered]@{}
foreach ($g in $bound.Observations.Bindings | Group-Object Classification) { $counts[$g.Name] = $g.Count }
$pending = [ordered]@{}
foreach ($g in $old.Changes | Where-Object Group -in @('R8','R9','R10') | Group-Object Group) { $pending[$g.Name] = $g.Count }

$info = Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfo.cs'
$cache = Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core/obj/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.AssemblyInfoInputs.cache'
$currentRevision = '9b76e179a568c64ae972cc3a45980a094e44245e'
$previousRevision = 'c777f9642c145a57c112c28216c3996128c6234b'
$infoText = [IO.File]::ReadAllText($info)
$oldText = $infoText.Replace($currentRevision, $previousRevision)
$generated = @(foreach ($revision in @($previousRevision, $currentRevision)) {
    $text = $infoText.Replace($currentRevision, $revision)
    $attributes = @([regex]::Matches($text, '\[assembly: ([^(]+)\("([^"]*)"\)\]') | ForEach-Object { $_.Groups[1].Value + $_.Groups[2].Value })
    $inputText = ($attributes -join [string][char]0x2028) + [char]0x2028
    $digest = (BytesHash ([Text.Encoding]::UTF8.GetBytes($inputText))).ToLowerInvariant()
    [ordered]@{ Revision = $revision; Attributes = $attributes.Count; AttributeDigest = $digest; CacheFileSha256 = BytesHash ([Text.Encoding]::UTF8.GetBytes($digest + "`r`n")) }
})
$oldInfoHash = BytesHash ([Text.Encoding]::UTF8.GetBytes($oldText))
$manifestInfo = @($manifest.Core | Where-Object Path -eq $info)[0]
$manifestCache = @($manifest.Core | Where-Object Path -eq $cache)[0]
$s1ContentProved = $oldInfoHash -ceq $manifestInfo.Sha256 -and $generated[0].CacheFileSha256 -ceq $manifestCache.Sha256 -and
    $generated[1].CacheFileSha256 -ceq (Hash $cache) -and $generated[1].AttributeDigest -ceq ([IO.File]::ReadAllText($cache).Trim())
$expectedTurnStart = @{
    $info = 'A6B602AD3DABBB3B7AD41B26C8B73CE2C65E8EE2D09DB60DBFF811440F1647E8'
    $cache = '1284572B6EEED1C6B6668BC02AF762B4DE5040B2D9F343AC90E54F4529B32BCD'
}
$unexpectedCore = @($coreDifferences | Where-Object { !$expectedTurnStart.ContainsKey($_.Path) -or $expectedTurnStart[$_.Path] -cne $_.Actual })

Push-Location $tool
try {
    $head = & git --no-optional-locks rev-parse HEAD
    $parent = & git --no-optional-locks rev-parse 'HEAD^'
    $lastCommit = & git --no-optional-locks log -1 --format='%H %cI %s'
    & git --no-optional-locks diff --cached --quiet
    $indexClean = $LASTEXITCODE -eq 0
    $cachedNames = @(& git --no-optional-locks diff --cached --name-only)
    $sourceChanges = @(& git --no-optional-locks diff --name-only -- src Tests build)
    $status = @(& git --no-optional-locks status --short)
    $stashes = @(& git --no-optional-locks stash list --format='%H %gs')
    $indexHash = Hash (Join-Path $repo '.git/index')
    $ignoreHash = Hash (Join-Path $repo '.gitignore')
    $checks = [ordered]@{
        HeadAndParent = $head -ceq $currentRevision -and $parent -ceq $previousRevision
        IndexBytesUnchanged = $indexHash -ceq 'CB38F3D3B845ABD54FEC4C2E111E77BFF7F31B80A73F438A657BAF03173F8158'
        SemanticIndexClean = $indexClean -and $cachedNames.Count -eq 0
        RootIgnoreBytesUnchanged = $ignoreHash -ceq $manifest.IgnoreSha256
        CoreTurnStartBytesUnchanged = $coreDifferences.Count -eq 2 -and $unexpectedCore.Count -eq 0
        BothGeneratedDriftsExplainedByteExactly = $s1ContentProved
        Inherited1092Unchanged = $manifest.OriginalFiles.Count -eq 1092 -and $originalDifferences.Count -eq 0
        TestHelper340Unchanged = $inventory.Files.Count -eq 340 -and $testDifferences.Count -eq 0
        Stashes6Unchanged = $stashes.Count -eq 6 -and ($stashes -join "`n") -ceq ($manifest.Stashes -join "`n")
        AllObservationsOneToOne = $oldKeys.Count -eq 3202 -and $newKeys.Count -eq 3202 -and $keyDifferences.Count -eq 0
        AllAssertionSourceSpansMatch = $badSpans.Count -eq 0
        All85Methods429CasesBound = @($bound.CaseRecords.Test | Sort-Object -Unique).Count -eq 85 -and $bound.CaseRecords.Count -eq 429
        NoAmbiguousOriginCategory = @($bound.Observations.Bindings | Where-Object Classification -notin @('EXACT','OUTSIDE PROFILE','DIAGNOSTIC','UNRELATED')).Count -eq 0
        OriginalFinalRequestUnchanged = $pending.R8 -eq 32 -and $pending.R9 -eq 27 -and $pending.R10 -eq 14 -and !@($old.Changes | Where-Object { $_.Group -in @('R8','R9','R10') -and $_.Approved }).Count
        NoProductionTestOrGateChanges = $sourceChanges.Count -eq 0
        AuditNoImplementation = !$audit.ProductionImplementationStarted -and !$audit.Commit -and !$audit.Push -and !$audit.StashOperations -and !$audit.IndexOperations
    }
    $passed = !@($checks.Values | Where-Object { $_ -ne $true }).Count
    $files = @('Supplementary-resolved-bindings-r2.json','Supplementary-binding-closure.md','Supplementary-method-reviews.txt','Resolve-Supplementary.csx','Inspect-Supplementary.csx','Protected-obj-drift-analysis.md','Capture-BindingClosure.ps1')
    $record = [ordered]@{
        Status = $(if ($passed) { 'STATIC_PHASE0_AUDIT_COMPLETE_STOP_NO_IMPLEMENTATION' } else { 'FAIL_STOP_NO_IMPLEMENTATION' })
        CapturedUtc = [DateTime]::UtcNow.ToString('o'); Head = $head; Parent = $parent; LastCommit = $lastCommit
        Checks = $checks; IndexSha256 = $indexHash; RootIgnoreSha256 = $ignoreHash; CachedPaths = $cachedNames
        CoreFiles = 23; CoreRawFrozenHashMatches = 21; CoreRawFrozenHashDifferences = $coreDifferences
        CoreUnexplainedDifferences = $unexpectedCore; ProtectionDecision = 'S1'; S1ContentProof = $generated
        ReconstructedOriginalAssemblyInfoSha256 = $oldInfoHash
        CoreMetadata = @($info,$cache | ForEach-Object { $item = Get-Item -LiteralPath $_; [ordered]@{ Path = $item.FullName; Size = $item.Length; LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o'); Tracked = $false; IgnoredBy = 'Core .gitignore:3 obj/'; Sha256 = Hash $item.FullName } })
        HistoricalManifest = 'artifacts/p5o2b25a9/start.json'; HistoricalManifestSha256 = Hash (Join-Path $tool 'artifacts/p5o2b25a9/start.json')
        HistoricalRawHashVerifierExitCode = 1; HistoricalGateReclassifiedAsPass = $false; GateRetried = $false
        ManifestChanged = $false; BaselineAccepted = $false; CoreRepairedOrRegenerated = $false; ActualWriterProved = $false
        InheritedFiles = 1092; InheritedDifferences = $originalDifferences; TestHelperFiles = 340; TestHelperDifferences = $testDifferences
        Stashes = $stashes; GitStatus = $status
        Original202CaseCounts = [ordered]@{ Exact = 81; Outside = 96; Diagnostic = 1; Unrelated = 25; Ambiguous = 0 }
        SupplementaryMethods = 85; SupplementaryCases = 429; SupplementaryObservations = 3202; Unbound = 0
        InactiveParameterBranchObservations = @($bound.Observations | Where-Object ParameterBranchReachability -eq 'INACTIVE in this parameter case').Count
        OriginCounts = $counts; NoChangeObservations = 3194; ExistingGrantOrRequestChangeObservations = 8
        R1ThroughR7PreviouslyApproved = $true; PendingFinalConditionalRows = $pending; NewR11Required = $false
        ProductBuilds = 0; TestBuilds = 0; TestsRun = 0; HistoricalWorkersRun = 0; BroadFullBoundaryDualWaeRun = $false
        SelfAnalysisRun = $false; CanonicalFindingDiffRun = $false; ImplementationClearance = $false; B25BClosed = $true
        ProcessRegistryDumpOrServiceChanges = $false; StashOperations = $false; IndexOperations = $false; Commit = $false; Push = $false
        A10Evidence = @($files | ForEach-Object { [ordered]@{ Path = $_; Sha256 = Hash (Join-Path $PSScriptRoot $_) } })
    }
    [IO.File]::WriteAllText($output, ($record | ConvertTo-Json -Depth 15) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
    $checks.GetEnumerator() | ForEach-Object { '{0}: {1}' -f $_.Key,$_.Value }
    $record.Status
    if (!$passed) { exit 1 }
} finally { Pop-Location }
