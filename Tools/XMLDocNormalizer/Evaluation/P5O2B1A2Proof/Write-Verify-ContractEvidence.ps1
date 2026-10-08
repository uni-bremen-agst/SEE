param([string] $Root = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
function Gate($condition, [string] $message) { if (-not $condition) { throw $message } }
function ReadJson([string] $path) { Get-Content -Raw -Encoding UTF8 -LiteralPath $path | ConvertFrom-Json }
function Json($value) { ConvertTo-Json -InputObject $value -Depth 80 -Compress }
function HashText([string] $value) {
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try { ([BitConverter]::ToString($algorithm.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)))).Replace('-', '') }
    finally { $algorithm.Dispose() }
}
$headCommit = (& git rev-parse HEAD).Trim()
Gate ($headCommit -eq '622388dcd2faa9af0ea1ecf61d52396ef1179a9c') 'Unexpected starting HEAD.'
$b1 = ReadJson 'Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json'
$b1a = ReadJson 'Evaluation/P5O2B1A-runtime-await-compatibility-proof-audit.json'
$a6f = ReadJson 'Evaluation/P5O2A6F-summary-orchestration-cycle-closure-audit.json'
$surface = ReadJson 'artifacts/p5o2b1a2/surface-proof.json'
$protected = @($b1.Git.ProtectedFiles | ForEach-Object {
    $sha = (Get-FileHash -LiteralPath $_.Path).Hash
    Gate ($sha -ceq $_.Sha256) ('Protected bytes changed: ' + $_.Path)
    [ordered]@{Path=$_.Path; Sha256=$sha; Unchanged=$true}
})
$stashes = @(& git stash list '--format=%H %gs')
Gate ((Json $stashes) -ceq (Json @($b1.Git.Stashes))) 'Stashes changed.'
Gate (@(& git diff --cached --name-only).Count -eq 0) 'Index changed.'
$prefix = 'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/'
$allowed = @('src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphAwaits.cs',
    'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs',
    'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs')
$capability = $prefix + 'ExceptionFlowRuntimeAwaitCapability.cs'
$originalFiles = @($b1.SourceVerification.CurrentMainSourceFiles | ForEach-Object {
    $sha = (Get-FileHash -LiteralPath $_.File).Hash
    $changed = $sha -cne $_.Sha256
    Gate (-not $changed -or $allowed -ccontains $_.File) ('Unexpected original source change: ' + $_.File)
    [ordered]@{File=$_.File; BeforeSha256=$_.Sha256; Sha256=$sha; Changed=$changed}
})
Gate (@($originalFiles | Where-Object Changed).Count -eq 3) 'Expected exactly three existing production files changed.'
Gate ($surface.ProductiveFileCount -eq 121 -and $surface.DirectNativeRuntimeAwaitReads -eq 0 -and
    $surface.CurrentSourceCompileErrors -eq 0 -and $surface.AdditionalApiDrifts -eq 0 -and
    -not $surface.HistoricalRuntimeLoaded) 'Surface proof failed.'
Gate (@($surface.Members).Count -eq 430 -and @($surface.Types).Count -eq 210 -and
    @($surface.EnumConstants).Count -eq 84 -and @($surface.Members | Where-Object ExpectedAbsence).Count -eq 1) 'API inventory differs.'
foreach ($file in $surface.ProductiveFiles) {
    Gate ((Get-FileHash -LiteralPath $file.File).Hash -ceq $file.Sha256) ('Source changed after proof: ' + $file.File)
}
$evaluated = (& dotnet msbuild Evaluation/P5O2B1A2Proof/HistoricalContractCompile.csproj -getItem:Compile -getProperty:TargetFramework,AssemblyName,BaseIntermediateOutputPath,BaseOutputPath | Out-String) | ConvertFrom-Json
Gate ($LASTEXITCODE -eq 0) 'MSBuild evaluation failed.'
$actual = @($evaluated.Items.Compile.FullPath | ForEach-Object { [IO.Path]::GetFullPath($_) } | Sort-Object)
$expected = @(@($b1.SourceVerification.Sources.File) + @($capability,
    'Evaluation/P5O2B1CompileProbe/CompileOnlySemanticEnvironment.cs') | ForEach-Object {
        [IO.Path]::GetFullPath((Join-Path $Root $_))
    } | Sort-Object)
Gate ((Json $actual) -ceq (Json $expected)) 'Historical sources omitted, copied or unexpectedly added.'
Gate ($actual.Count -eq 122) 'Must retain 120 original productive files + capability + compile-only host.'
$assetsPath = 'artifacts/p5o2b1a2/HistoricalContractCompile/obj/project.assets.json'
$assets = ReadJson $assetsPath
$version = '5.0.0-2.25451.107'
foreach ($id in @('Microsoft.CodeAnalysis.Common', 'Microsoft.CodeAnalysis.CSharp')) {
    Gate ($null -ne $assets.libraries.PSObject.Properties["$id/$version"]) ('Wrong package: ' + $id)
}
Gate (@($assets.project.restore.frameworks.'net8.0'.projectReferences.PSObject.Properties).Count -eq 0) 'Historical ProjectReference leakage.'
$references = @($b1.HistoricalReferenceAssemblies | ForEach-Object {
    $path = $_.Path.Replace('artifacts/p5o2b1/packages', 'artifacts/p5o2b1a2/packages')
    Gate ((Get-FileHash -LiteralPath $path).Hash -ceq $_.Sha256) ('Historical binary changed: ' + $path)
    [ordered]@{Path=$path; Sha256=$_.Sha256; InformationalVersion=$_.InformationalVersion; Mvid=$_.Mvid}
})
$sources = @($b1a.Proof.CompilerSourcePdbChecksums | ForEach-Object {
    $bytes = [IO.File]::ReadAllBytes($_.Source)
    Gate ((Get-FileHash -LiteralPath $_.Source).Hash -ceq $_.Sha256) 'Retained compiler source bytes changed.'
    $text = [Text.Encoding]::UTF8.GetString($bytes) # Preserve any BOM as U+FEFF.
    $normalized = $text.Replace("`r`n", "`n").Replace("`n", "`r`n")
    $sha = HashText $normalized
    Gate ($sha -ceq $_.PdbExpectedChecksum) 'Compiler source/PDB mismatch.'
    [ordered]@{Source=$_.Source; Document=$_.Document; RawSha256=$_.Sha256;
        ExpectedPdbChecksum=$_.PdbExpectedChecksum; CheckoutChecksum=$sha; PdbChecksumEqual=$true;
        Normalization='LF to build-checkout CRLF in memory, preserving existing UTF8 BOM; no external source file rewritten.'}
})
Gate ($sources.Count -eq 8 -and $b1a.Proof.HistoricalBinaryAssertions.BindingHasBothRuntimeAndPatternPaths -and
    $b1a.Proof.HistoricalBinaryAssertions.PublicAwaitInfoOmitsRuntimeHelper) 'Historical semantic evidence incomplete.'
$firstImage = ReadJson 'artifacts/p5o2b1a2/historical-first-image.json'
$lastHash = (Get-FileHash -LiteralPath $firstImage.Path).Hash
Gate ($firstImage.Hash -ceq $lastHash) 'Repeated historical images differ.'
$buildLogs = @('current-main','current-core','current-evaluation','current-tests','current-architecture-audit','surface-build','historical-first','historical-repeat')
$builds = @($buildLogs | ForEach-Object {
    $path = 'artifacts/p5o2b1a2/' + $_ + '.log'
    $log = Get-Content -Raw -LiteralPath $path
    Gate ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler' -and $log -notmatch '\): error ') ('Build gate failed: ' + $path)
    [ordered]@{Name=$_; Warnings=0; Errors=0; Artifact=$path; Sha256=(Get-FileHash $path).Hash}
})
$tests = @('focused-final','await-summary-regression','full','full-final') | ForEach-Object {
    $path = 'artifacts/p5o2b1a2/tests/' + $_ + '.trx'
    [xml]$trx = Get-Content -Raw -LiteralPath $path
    $counts = $trx.TestRun.ResultSummary.Counters
    Gate ($trx.TestRun.ResultSummary.outcome -eq 'Completed' -and [int]$counts.failed -eq 0 -and
        [int]$counts.notExecuted -eq 0 -and [int]$counts.passed -eq [int]$counts.total) ('TRX gate failed: ' + $path)
    [ordered]@{Run=$_; Total=[int]$counts.total; Passed=[int]$counts.passed; Failed=[int]$counts.failed;
        Skipped=[int]$counts.notExecuted; Artifact=$path; Sha256=(Get-FileHash $path).Hash}
}
Gate (($tests | Where-Object Run -eq 'focused-final').Passed -eq 24) 'New tests differ.'
Gate (($tests | Where-Object Run -eq 'full-final').Passed -eq 2600) 'Final full count differs.'
$before = ReadJson 'artifacts/p5o2a6f/self-analysis.json'
$self = ReadJson 'artifacts/p5o2b1a2/self-analysis-final.json'
function Key($finding) {
    @($finding.FilePath.Replace($Root, '').Replace('\', '/'), $finding.SmellId, $finding.ContainingNamespace,
        $finding.ContainingType, $finding.SymbolName, $finding.TargetName, $finding.TagName, $finding.OwnerKind,
        $finding.SubjectKind, $finding.Line, $finding.Column) -join '|'
}
function Normalize($findings) {
    @($findings | Sort-Object { Key $_ } | ForEach-Object {
        $copy = [ordered]@{}
        foreach ($property in $_.PSObject.Properties) {
            $copy[$property.Name] = if ($property.Name -eq 'FilePath') {
                $property.Value.Replace($Root, '').Replace('\', '/')
            } else { $property.Value }
        }
        [pscustomobject]$copy
    })
}
$oldNormalized = Normalize $before.Findings
$newNormalized = Normalize $self.Findings
Gate ($self.Findings.Count -eq 16 -and $self.Metrics.TotalFindingCounts.DOC611 -eq 1 -and
    $self.Metrics.TotalFindingCounts.DOC631 -eq 15) 'Self counts differ.'
Gate ((Json @($before.Findings)) -ceq (Json @($self.Findings))) 'Complete raw finding/evidence arrays differ.'
Gate ((Json $oldNormalized) -ceq (Json $newNormalized)) 'Normalized finding/evidence arrays differ.'
Gate ((Json $newNormalized) -ceq (Json @($a6f.Validation.Canonical.FindingsAfter))) 'Committed A6F canonical baseline differs.'
$qualityFiles = @($allowed + @($capability, 'Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowRuntimeAwaitCapabilityTests.cs',
    'Evaluation/P5O2B1A2-runtime-await-call-site-contract.md', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md') +
    @(Get-ChildItem Evaluation/P5O2B1A2Proof -File | ForEach-Object { $_.FullName }))
foreach ($path in $qualityFiles) {
    $text = [Text.UTF8Encoding]::new($false, $true).GetString([IO.File]::ReadAllBytes($path))
    Gate (-not ($text -match '(?<!\r)\n|\r(?!\n)|(?m)[ \t]+\r?$') -and $text.EndsWith("`r`n")) ('CRLF/UTF8/whitespace gate failed: ' + $path)
    if ([IO.Path]::GetExtension($path) -in @('.csproj','.props')) { $null = [xml]$text }
    if ([IO.Path]::GetExtension($path) -eq '.ps1') {
        $tokens = $null; $errors = $null
        $null = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$errors)
        Gate ($errors.Count -eq 0) 'PowerShell parse failed.'
    }
}
& git diff --check
Gate ($LASTEXITCODE -eq 0) 'git diff --check failed.'
$callSites = @(
    @{File='ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs'; OriginalLine=218; Role='Runtime-helper route selection'; Method='AddSummaryExplicitAwaitEdges'; Active=$false},
    @{File='ExceptionFlowAnalyzer.SummaryGraphImplicitCalls.cs'; OriginalLine=221; Role='Exact helper edge target'; Method='AddSummaryExplicitAwaitEdges'; Active=$false},
    @{File='ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs'; OriginalLine=275; Role='Runtime-helper route selection before dispatch'; Method='AddSummaryExplicitAwaitDispatchEdges'; Active=$true},
    @{File='ExceptionFlowAnalyzer.SummaryGraphImplicitDispatch.cs'; OriginalLine=278; Role='Exact helper edge target'; Method='AddSummaryExplicitAwaitDispatchEdges'; Active=$true}
) | ForEach-Object {
    [ordered]@{File=$prefix + $_.File; OriginalLine=$_.OriginalLine; Method=$_.Method; Role=$_.Role; ActiveProductiveCaller=$_.Active;
        ConcreteSymbol='AddSummaryImplicitMethodEdge -> register exact target/context -> RuntimeAwaitCall edge -> return; replaces normal chain.';
        KnownNull=if ($_.Active) {'Normal awaiter receiver/dispatch resolution; retain completeness uncertainty and three pattern targets.'} else {'Normal selected GetAwaiter/get_IsCompleted/GetResult edges; retain completeness uncertainty.'};
        Unavailable='AddUncertainTarget + return, no speculative helper/pattern edge; never known-null.';
        FinalDecision='Fragment Merge -> Summary.UncertainTargets -> evaluator frame/result union -> caller MergeWithPrefixExcluding preserves uncertainty -> HasUncertainPaths -> DOC632 suppressed, DOC631 if relevant uncovered tag. Independent proven paths remain.';
        CatchTransfer='Typed catch retains uncertainty. Existing proven unfiltered catch-all may SuppressAll; rethrow/filter guards remain unchanged.'}
}
$audit = [ordered]@{
    Schema='P5O2B1A2-runtime-await-call-site-contract-v1'; Date='2026-10-08'; Head=$headCommit;
    Decision='READY for P5O2B2'; Scope='Compile compatibility and local unavailable-information contract; no historical runtime or B2 scaffold.';
    Inputs=@('Evaluation/P5O2B1-historical-roslyn-build-feasibility.md','Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json',
        'Evaluation/P5O2B1-roslyn-api-drift-matrix.md','Evaluation/P5O2B1A-runtime-await-compatibility-proof.md',
        'Evaluation/P5O2B1A-runtime-await-compatibility-proof-audit.json') | ForEach-Object { @{Path=$_; Sha256=(Get-FileHash $_).Hash} };
    Owner=@{File=$capability; Sha256=(Get-FileHash $capability).Hash; States=@('available/symbol','available/native-null','unavailable');
        Default='unavailable'; StaticState='One cached open ref-receiver getter delegate; thread-safe CLR initialization. No cached semantic objects or Analyzer state.';
        TestBoundary='Immutable per-call Information input on the two existing private bodies. Tests reflect only our own bodies, no global hooks or historical execution.'};
    FourCallSites=$callSites; OriginalMainSourceFingerprints=$originalFiles;
    HistoricalCompilerEvidence=@{Version=$version; References=$references; SourcePdbChecksums=$sources;
        BinaryAssertions=$b1a.Proof.HistoricalBinaryAssertions; BinaryMethodEvidence=$b1a.Proof.HistoricalBinaryMethodEvidence;
        BinaryEvidenceReuse='B1A decoded IL is retained. All historical PE bytes rehashed identical; source raw bytes and eight PDB document checksums independently reverified.';
        SourceLink=$b1a.Proof.HistoricalSourceLink};
    ApiSurface=$surface;
    HistoricalCompile=@{Project='Evaluation/P5O2B1A2Proof/HistoricalContractCompile.csproj'; Properties=$evaluated.Properties;
        CompileItems=$evaluated.Items.Compile | Select-Object FullPath,Link; OriginalProductiveFiles=120; AddedProductiveFiles=1;
        ExplicitCompileItems=122; Host='Original five always-throwing compile-only members, never executed.';
        AssetsPath=$assetsPath; AssetsSha256=(Get-FileHash $assetsPath).Hash; ResolvedLibraries=$assets.libraries;
        Runs=2; Warnings=0; Errors=0; FirstImageSha256=$firstImage.Hash; RepeatImageSha256=$lastHash; ImagesEqual=$true;
        InMemoryProjection=$false; HistoricalRuntimeExecuted=$false; Driver='Installed SDK 8.0.418 / csc 4.11.0; targeting exact historical API references, not executing historical compiler.'};
    Validation=@{Tests=$tests; WarningAsErrorBuilds=$builds;
        CoreIsolation='All four solution projects built. Core intermediate/output isolated under artifacts/p5o2b1a2/core-build; dependent projects BuildProjectReferences=false. Not an ordinary solution-wide rebuild.';
        SelfAnalysis=@{Artifact='artifacts/p5o2b1a2/self-analysis-final.json'; Sha256=(Get-FileHash 'artifacts/p5o2b1a2/self-analysis-final.json').Hash;
            ExitCode=1; Findings=16; Counts=$self.Metrics.TotalFindingCounts};
        Canonical=@{Baseline='A6F'; Added=0; Removed=0; ChangedEvidence=0; FullRawArraysEqual=$true; FullNormalizedArraysEqual=$true;
            Normalization='Only workspace prefix/separators and deterministic finding order; no messages, evidence, positions or uncertainty counts removed.';
            FindingsBefore=$oldNormalized; FindingsAfter=$newNormalized; BeforeNormalizedHash=(HashText (Json $oldNormalized)); AfterNormalizedHash=(HashText (Json $newNormalized))};
        InitialVerificationIssues=@('Three test inventory compile errors corrected (ID/MethodCall names); two structural-constructor test failures corrected to actual argument order; no productive fix driven by these failures.',
            'Initial Self Analysis 18: two param-doc order errors fixed. Next Self Analysis 16 with one DOC631 evidence-count delta: carrier auto-getters added two uncertainty targets in self-analysis versus one removed native getter. Availability now a readonly data flag; final full raw/normalized finding arrays exactly match A6F.',
            'Read-only diagnostic fixes: duplicate platform reference filenames and exact detector overload selection. No foreign flake/production analyzer algorithm was changed.')};
    QualityGates=@{ScopedFormatVerified=$true; StrictUtf8CrLfFinalNewline=$true; JsonXmlPowerShellParse=$true; GitDiffCheck=$true};
    B2Recommendation=@{Project='Dedicated source-linked historical Analyzer owner, distinct assembly identity; no Main/Core runtime ProjectReferences.';
        Sources='All original 120 + the one shared capability; one implementation, explicit shared source-item manifest; replace compile-only host with a real compile-local semantic capability host in later authorized work.';
        TargetFramework='net8.0'; Packages=@{Common='[5.0.0-2.25451.107]'; CSharp='[5.0.0-2.25451.107]'; Analyzers='[3.11.0] private'; Immutable='[9.0.0]'; Metadata='[9.0.0]'};
        NotNeeded=@('CodeAnalysis umbrella','Workspaces/MSBuild packages','TFM switch','Toolset as compiler driver');
        Isolation='Dedicated BaseIntermediateOutputPath/MSBuildProjectExtensionsPath and BaseOutputPath before SDK imports; independent project.assets.json and package graph, no current bin/obj overwrite.';
        Runtime='Identical public Roslyn assembly identities still require later isolation. No worker/IPC/ALC implemented or historical runtime readiness claimed.'};
    Git=@{ProtectedFiles=$protected; Stashes=$stashes; StashesUnchanged=$true; Commit=$false; Push=$false; Reset=$false; IndexChanged=$false;
        TransientGeneratedCoreChurn=@{Files=@('Core obj/AssemblyInfo.cs','Core obj/AssemblyInfoInputs.cache');
            Cause='Design-time current-project evaluation regenerates revision/culture-sensitive assembly metadata even when actual Core builds use isolated output paths.';
            Recovery='Only task-induced changes reversed with apply_patch after initial-byte SHA256 matching. Original cache content independently matched retained A6F isolated artifact.';
            FinalInitialByteHashesEqual=$true; GitRestoreOrResetUsed=$false; OriginalWipDiscarded=$false};
        IgnoredProjects=@(& git check-ignore -v Evaluation/P5O2B1A2Proof/HistoricalContractCompile.csproj Evaluation/P5O2B1A2Proof/CurrentSurfaceProof.csproj);
        StatusShort=@(& git status --short)}
}
$output = 'Evaluation/P5O2B1A2-runtime-await-call-site-contract-audit.json'
$jsonText = (($audit | ConvertTo-Json -Depth 80) -replace "`r?`n", "`r`n") + "`r`n"
[IO.File]::WriteAllText((Join-Path $Root $output), $jsonText, [Text.UTF8Encoding]::new($false))
$null = ReadJson $output
Write-Output 'A2 READY: 24 focused, 420 relevant, 2600 full/final; self 16; canonical 0/0/0; historical 120+1 sources twice 0/0; protected files/stashes unchanged.'
