param([string]$Root = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
$b1 = Get-Content Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json -Raw -Encoding UTF8 | ConvertFrom-Json
$proof = Get-Content artifacts/p5o2b1a/proof-results.json -Raw -Encoding UTF8 | ConvertFrom-Json
$headCommit = (& git rev-parse HEAD).Trim()
if ($headCommit -ne 'd3140d7aa444809fa7dad7ad8ec4fa5b228040b3') { throw 'Unexpected B1A HEAD.' }
if (@(& git diff --name-only -- src/XMLDocNormalizer Tests XMLDocNormalizer.sln Evaluation/P5O2B1CompileProbe src/XMLDocNormalizer.ExceptionFlow.Core/XMLDocNormalizer.ExceptionFlow.Core.csproj).Count -ne 0) {
    throw 'Productive, test, solution or committed B1 source changed.'
}
foreach ($file in $b1.SourceVerification.CurrentMainSourceFiles) {
    if ((Get-FileHash -LiteralPath $file.File).Hash -ne $file.Sha256) { throw "Productive source hash changed: $($file.File)" }
}
foreach ($file in $b1.Git.ProtectedFiles) {
    if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Sha256) { throw "Protected foreign file changed: $($file.Path)" }
}
$stashes = @(& git stash list '--format=%H %gs')
if (Compare-Object @($b1.Git.Stashes) $stashes) { throw 'Stashes changed.' }
$capability = 'Evaluation/P5O2B1AProof/RuntimeAwaitCapability.cs'
if ((Get-FileHash -LiteralPath $capability).Hash -ne $proof.CapabilitySourceSha256) { throw 'Capability changed since proof run.' }
$projects = @('CurrentCapabilityProof', 'HistoricalCapabilityProof')
$projectEvidence = @($projects | ForEach-Object {
    $name = $_
    $path = "Evaluation/P5O2B1AProof/$name.csproj"
    $evaluated = (& dotnet msbuild $path -getItem:Compile -getProperty:TargetFramework,AssemblyName,BaseIntermediateOutputPath,BaseOutputPath | Out-String) | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) { throw 'Compile item evaluation failed.' }
    $actualSource = @($evaluated.Items.Compile | Where-Object Filename -eq 'RuntimeAwaitCapability')
    if ($actualSource.Count -ne 1 -or [IO.Path]::GetFullPath($actualSource[0].FullPath) -ne (Join-Path $Root $capability)) { throw 'Capability source not identical.' }
    $assetsPath = "artifacts/p5o2b1a/$name/obj/project.assets.json"
    $assets = Get-Content -LiteralPath $assetsPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $expectedVersion = if ($name -eq 'HistoricalCapabilityProof') { '5.0.0-2.25451.107' } else { '5.0.0' }
    if (-not $assets.libraries.PSObject.Properties["Microsoft.CodeAnalysis.CSharp/$expectedVersion"] -or
        -not $assets.libraries.PSObject.Properties["Microsoft.CodeAnalysis.Common/$expectedVersion"]) { throw 'Incorrect compiler packages.' }
    [ordered]@{ Project = $path; Sha256 = (Get-FileHash -LiteralPath $path).Hash
        Properties = $evaluated.Properties; CompileInputs = @($evaluated.Items.Compile | Select-Object FullPath,Link)
        AssetsPath = $assetsPath; AssetsSha256 = (Get-FileHash -LiteralPath $assetsPath).Hash
        PackageLibraries = $assets.libraries; DirectVersions = $assets.project.frameworks; RestoreMessages = @($assets.logs) }
})
$h = @($proof.ProjectedCompiles | Where-Object Name -eq 'HistoricalProjected')
if ($h.Count -ne 2 -or $h[0].ImageSha256 -ne $h[1].ImageSha256 -or
    @($proof.ProjectedCompiles | Where-Object Success -eq $false).Count -ne 0 -or
    @($proof.CompilerSourcePdbChecksums | Where-Object PdbChecksumEqual -eq $false).Count -ne 0 -or
    $proof.HistoricalRuntimeLoaded -or $proof.ProductiveSourceFiles -ne 120 -or $proof.Projection.Count -ne 4 -or
    $proof.AdditionalUsedApiDrifts -ne 0) { throw 'Proof invariants failed.' }

& dotnet format Evaluation/P5O2B1AProof/CurrentCapabilityProof.csproj --include Evaluation/P5O2B1AProof/RuntimeAwaitCapability.cs Evaluation/P5O2B1AProof/ProofProgram.cs --no-restore --verify-no-changes --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Current proof format gate failed.' }
& dotnet format Evaluation/P5O2B1AProof/HistoricalCapabilityProof.csproj --include Evaluation/P5O2B1AProof/RuntimeAwaitCapability.cs --no-restore --verify-no-changes --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Historical proof format gate failed.' }
& git diff --check
if ($LASTEXITCODE -ne 0) { throw 'git diff --check failed.' }
$files = @(Get-ChildItem Evaluation/P5O2B1AProof -Recurse -File | ForEach-Object FullName) + @(
    (Join-Path $Root 'Evaluation/P5O2B1A-runtime-await-compatibility-proof.md'),
    (Join-Path $Root 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
foreach ($path in $files) {
    $text = [Text.UTF8Encoding]::new($false, $true).GetString([IO.File]::ReadAllBytes($path))
    if ($text -match '(?<!\r)\n|\r(?!\n)|(?m)[ \t]+\r?$' -or -not $text.EndsWith("`r`n")) { throw "CRLF/whitespace gate failed: $path" }
    if ([IO.Path]::GetExtension($path) -in @('.csproj', '.props')) { $null = [xml]$text }
    if ([IO.Path]::GetExtension($path) -eq '.ps1') {
        $parseTokens = $null
        $parseErrors = $null
        $null = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$parseTokens, [ref]$parseErrors)
        if ($parseErrors.Count -ne 0) { throw "PowerShell parse gate failed: $path" }
    }
}
$logs = @('artifacts/p5o2b1a/original-boundary.log', 'artifacts/p5o2b1a/current-main-build.log',
    'artifacts/p5o2b1a/current-capability-build.log', 'artifacts/p5o2b1a/historical-capability-build.log',
    'artifacts/p5o2b1a/historical-capability-repeat.log')
$audit = [ordered]@{
    Schema = 'P5O2B1A-runtime-await-proof-v1'; Date = '2026-10-08'; Head = $headCommit; Decision = 'NOT READY'
    Reason = 'Missing public API is not missing runtime-async capability. A method-or-null fallback can lose a real helper target while the normal awaiter pattern remains complete.'
    B1EvidenceFullyReadAndVerified = $true; HistoricalVersion = '5.0.0-2.25451.107'
    Proof = $proof; Projects = $projectEvidence
    FourCallSites = @($b1.Validation.HistoricalInitial.Errors | Select-Object File,Line,Column,Api)
    ProductiveSourceUnchanged = $true; CommittedB1SourcesUnchanged = $true; ProductiveSourceFingerprints = $b1.SourceVerification.CurrentMainSourceFiles
    UnprojectedHistoricalBoundary = @{ Errors = 4; Code = 'CS1061'; Api = 'RuntimeAwaitMethod'; Warnings = 0 }
    CurrentMainBuild = @{ Errors = 0; Warnings = 0; ExitCode = 0 }
    CapabilityBuilds = @{ Current = '0 warnings / 0 errors'; Historical = '0 warnings / 0 errors'; HistoricalRepeat = '0 warnings / 0 errors'; IdenticalSource = $true }
    CompileProofCaveat = 'SDK compiles the tiny capability; the current Roslyn 5.0.0 proof engine emits the exact historical-reference 120-source in-memory projection. Historical code is never runtime loaded or executed. Historical projection substitutes the explicit unsupported rejection, NOT a validated analysis fallback.'
    AnalysisFallbackGate = @{ Passed = $false; NullFallbackImplemented = $false; CurrentNativeGetterEquivalent = $true
        HistoricalReconstruction = 'Information exists internally at MemberSemanticModel.GetLowerBoundNode(await node) -> BoundAwaitExpression.AwaitableInfo.RuntimeAsyncAwaitCall.Method -> GetPublicSymbol; AwaitExpressionInfo and IAwaitOperation do not carry it. Exact reconstruction requires additional semantic-model/node context and compiler-internal APIs, outside the single-struct getter seam.'
        MinimalNextPackage = 'P5O2B1A2 - Missing RuntimeAwait Information / Fail-Closed Call-Site Contract Proof'
        Candidate = 'Distinguish NativeValue (including native null) from Unavailable; prove two explicit-await caller guards recording uncertainty before normal pattern analysis. A conservative result is not exact helper reconstruction; do not silently assume API absence means runtime-async absence.' }
    StrategyComparison = @(
        @{ Strategy = 'A bounded light-up'; Result = 'Current getter works, cached typed ref delegate; historical absence must be Unavailable, not null. Not ready as method-or-null alone.' },
        @{ Strategy = 'B common API denominator'; Result = 'Reject: removes valid current runtime helper targets and loses historical hidden targets.' },
        @{ Strategy = 'C compile-time shim'; Result = 'Same missing-information problem; native current plus historical null is unsafe. Availability-aware shim remains a bounded candidate.' },
        @{ Strategy = 'D four call-site #if'; Result = 'No semantic benefit; duplicates version branching and still needs missing-information policy. Reject as unnecessary.' },
        @{ Strategy = 'E source fork'; Result = 'No missing information restored by copying; violates same-source goal. Reject.' })
    SemanticsInheritedFromA6F = $b1.Validation.Semantics
    QualityGates = @{ Format = $true; Json = $true; Crlf = $true; StrictUtf8 = $true; XmlAndPowershellParse = $true; GitDiffCheck = $true }
    Logs = @($logs | ForEach-Object { @{ Path = $_; Sha256 = (Get-FileHash -LiteralPath $_).Hash } })
    Git = @{ ProtectedFiles = $b1.Git.ProtectedFiles; ProtectedHashesReverified = $true; Stashes = $stashes; StashesUnchanged = $true
        StatusShort = @(& git status --short); IgnoredNewProjectRules = @(& git check-ignore -v Evaluation/P5O2B1AProof/CurrentCapabilityProof.csproj Evaluation/P5O2B1AProof/HistoricalCapabilityProof.csproj)
        Commit = $false; Push = $false; StashMutation = $false; Reset = $false; IndexChange = $false }
}
$path = Join-Path $Root 'Evaluation/P5O2B1A-runtime-await-compatibility-proof-audit.json'
$json = ($audit | ConvertTo-Json -Depth 30).Replace("`r`n", "`n").Replace("`n", "`r`n") + "`r`n"
[IO.File]::WriteAllText($path, $json, [Text.UTF8Encoding]::new($false))
$null = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
if ($json -match '(?<!\r)\n|\r(?!\n)|(?m)[ \t]+\r?$') { throw 'Generated JSON whitespace/CRLF failed.' }
Write-Output "B1A evidence/gates PASS; $($proof.CurrentTests.Count) current checks, identical-source capability builds, 120-source projected historical 0/0 twice, exact-source/PDB and binary evidence; semantic fallback NOT READY. Protected files/stashes unchanged."
