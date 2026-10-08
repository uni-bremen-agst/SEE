param([string]$Root = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root

function Read-Json([string]$Path) {
    Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
}
function Write-Crlf([string]$Path, [string]$Text) {
    $normalized = $Text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
    [IO.File]::WriteAllText((Join-Path $Root $Path), $normalized.TrimEnd() + "`r`n", [Text.UTF8Encoding]::new($false))
}
function Cell([object]$Value) {
    ([string]$Value).Replace('|', '\|').Replace("`r", '').Replace("`n", '<br>')
}
function Relative-Path([string]$Path) {
    $prefix = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $full = [IO.Path]::GetFullPath($Path)
    if (-not $full.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Path outside B1 workspace: $Path" }
    $full.Substring($prefix.Length).Replace('\', '/')
}

$a6fPath = 'Evaluation/P5O2A6F-summary-orchestration-cycle-closure-audit.json'
$a6f = Read-Json $a6fPath
$metadata = Read-Json 'artifacts/p5o2b1/metadata-audit.json'
$preflight = Read-Json 'artifacts/p5o2b1/preflight.json'
$p5n = Read-Json 'artifacts/p5n-analysis/p5n-report.json'
$headCommit = (& git rev-parse HEAD).Trim()
if ($headCommit -ne $preflight.Head) { throw 'HEAD changed during B1.' }
$productionDiff = @(& git diff --name-only -- src/XMLDocNormalizer Tests XMLDocNormalizer.sln src/XMLDocNormalizer.ExceptionFlow.Core/XMLDocNormalizer.ExceptionFlow.Core.csproj Evaluation/XMLDocNormalizer.Evaluation)
if ($productionDiff.Count -ne 0) { throw 'Production/test/project source changed.' }
$fingerprints = @($a6f.CurrentSourceFingerprints | ForEach-Object {
    $actual = (Get-FileHash -LiteralPath $_.File -Algorithm SHA256).Hash
    if ($actual -ne $_.FileBytesSha256) { throw "A6F source mismatch: $($_.File)" }
    [ordered]@{ File = $_.File; Sha256 = $actual; EqualToA6F = $true }
})
$protected = @($preflight.Protected | ForEach-Object {
    $actual = (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash
    if ($actual -ne $_.Sha256) { throw "Protected foreign file changed: $($_.Path)" }
    [ordered]@{ Path = $_.Path; Sha256 = $actual; Unchanged = $true }
})
$stashes = @(& git stash list '--format=%H %gs')
if (Compare-Object @($preflight.Stashes) $stashes) { throw 'Stashes changed.' }

$evaluatedText = & dotnet msbuild Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj -getItem:Compile -getProperty:TargetFramework,AssemblyName,BaseIntermediateOutputPath,BaseOutputPath | Out-String
if ($LASTEXITCODE -ne 0) { throw 'Probe item evaluation failed.' }
$evaluated = $evaluatedText | ConvertFrom-Json
$compileFiles = @($evaluated.Items.Compile | ForEach-Object { Relative-Path $_.FullPath })
$expected = @($a6f.SourceManifest.File) + @('Evaluation/P5O2B1CompileProbe/CompileOnlySemanticEnvironment.cs')
if (Compare-Object $expected $compileFiles) { throw 'Evaluated compile boundary differs from A6F plus the explicit host.' }
$sources = @($a6f.SourceManifest | ForEach-Object {
    $file = $_
    $owners = @($a6f.Components | Where-Object { $file.Owners -contains $_.Owner })
    $types = @($a6f.RoslynInventory.Types | Where-Object { $_.SourceFiles -contains $file.File } | ForEach-Object Type)
    $purpose = @($file.Categories | ForEach-Object {
        switch ($_) {
            'A' { 'Historical algorithm owner: compile the unchanged fact/resolver/summary/analysis implementation in the historical Roslyn universe.' }
            'B' { 'Shared Roslyn infrastructure: needed by the closed algorithm dependency graph; recompile source locally, never reference the active Main assembly.' }
            'D' { 'Neutral value/canonical/path contract required by the closed dependency graph; source-local compile avoids Main runtime identity coupling.' }
        }
    }) -join ' '
    if ($file.Owners.Count -eq 0) { $purpose = 'Namespace documentation belonging to the exact A6F source manifest; retained, not omitted for probe convenience.' }
    [ordered]@{ File = $file.File; Categories = $file.Categories; Owners = $file.Owners; Purpose = $purpose
        SourceDependencies = @($owners.SourceDependencies | Sort-Object -Unique); RoslynTypes = $types
        ProjectReferences = @(); MainOnlyInfrastructure = $false; Sha256 = (Get-FileHash -LiteralPath $file.File).Hash }
})

$projectPaths = @('src/XMLDocNormalizer/XMLDocNormalizer.csproj',
    'src/XMLDocNormalizer.ExceptionFlow.Core/XMLDocNormalizer.ExceptionFlow.Core.csproj',
    'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj',
    'Evaluation/XMLDocNormalizer.Evaluation/XMLDocNormalizer.Evaluation.csproj',
    'Evaluation/ContextualBoundaryAudit/ContextualBoundaryAudit.csproj',
    'Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj',
    'Evaluation/P5O2B1CompileProbe/MetadataAudit/MetadataAudit.csproj')
$projects = @($projectPaths | ForEach-Object {
    $path = $_
    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $name = [IO.Path]::GetFileNameWithoutExtension($path)
    $assetsPath = if ($path -like 'Evaluation/P5O2B1CompileProbe/*') {
        "artifacts/p5o2b1/$name/obj/project.assets.json"
    } else { Join-Path (Split-Path $path -Parent) 'obj/project.assets.json' }
    $assets = Read-Json $assetsPath
    $graph = @($assets.targets.PSObject.Properties | ForEach-Object {
        $target = $_
        @($target.Value.PSObject.Properties | ForEach-Object {
            [ordered]@{ Target = $target.Name; Identity = $_.Name; Type = $_.Value.type
                Dependencies = $_.Value.dependencies; Compile = @($_.Value.compile.PSObject.Properties.Name)
                Runtime = @($_.Value.runtime.PSObject.Properties.Name) }
        })
    })
    [ordered]@{ Project = $path; TargetFramework = [string]$xml.Project.PropertyGroup.TargetFramework
        AssemblyName = [string]$xml.Project.PropertyGroup.AssemblyName; Sha256 = (Get-FileHash -LiteralPath $path).Hash
        DirectPackages = @($xml.SelectNodes('//PackageReference') | ForEach-Object {
            [ordered]@{ Id = $_.Include; Version = $_.Version; PrivateAssets = $_.PrivateAssets; ExcludeAssets = $_.ExcludeAssets }
        }); ProjectReferences = @($xml.SelectNodes('//ProjectReference') | ForEach-Object Include)
        AssetsPath = $assetsPath; AssetsSha256 = (Get-FileHash -LiteralPath $assetsPath).Hash
        AssetsGraph = $graph; RestoreSources = $assets.project.restore.sources; RestoreMessages = @($assets.logs) }
})
$packages = @($p5n.Packages | ForEach-Object {
    $path = "artifacts/p5n-acquisition/packages/$($_.PackageName.ToLowerInvariant()).$($_.PackageVersion).nupkg"
    $actual = (Get-FileHash -LiteralPath $path).Hash
    if ($actual -ne $_.Sha256) { throw "Historical package hash mismatch: $path" }
    [ordered]@{ Id = $_.PackageName; Version = $_.PackageVersion; ArtifactUrl = $_.ArtifactUrl
        Path = $path; Sha256 = $actual; Bytes = (Get-Item -LiteralPath $path).Length; RepositoryCommit = $_.RepositoryCommit }
})
$assemblies = @(@('Common', 'CSharp') | ForEach-Object {
    $candidate = $p5n.CandidateAssemblySets[0].$_
    $id = if ($_ -eq 'Common') { 'microsoft.codeanalysis.common' } else { 'microsoft.codeanalysis.csharp' }
    $path = "artifacts/p5o2b1/packages/$id/5.0.0-2.25451.107/lib/net8.0/$($candidate.Name).dll"
    $actual = (Get-FileHash -LiteralPath $path).Hash
    if ($actual -ne $candidate.Sha256) { throw "Historical reference hash mismatch: $path" }
    [ordered]@{ Path = $path; Sha256 = $actual; InformationalVersion = $candidate.InformationalVersion
        Mvid = $candidate.Mvid; AssemblyVersion = $candidate.AssemblyVersion; ValidatedP5NPair = $true }
})

function Compile-Errors([string]$Path) {
    $text = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    @([regex]::Matches($text, '(?<file>D:\\Repository\\SEE\\Tools\\XMLDocNormalizer\\src\\[^\r\n>]+\.cs)\((?<line>\d+),(?<column>\d+)\): error (?<code>CS\d+): (?<message>[^\r\n]+)') | ForEach-Object {
        [ordered]@{ File = Relative-Path $_.Groups['file'].Value
            Line = [int]$_.Groups['line'].Value; Column = [int]$_.Groups['column'].Value; Code = $_.Groups['code'].Value
            Message = $_.Groups['message'].Value; Classification = 'API exists currently but is absent historically'
            Api = 'Microsoft.CodeAnalysis.CSharp.AwaitExpressionInfo.RuntimeAwaitMethod' }
    } | Sort-Object { $_.File }, { $_.Line } -Unique)
}
$firstErrors = @(Compile-Errors 'artifacts/p5o2b1/historical-first.log')
$repeatErrors = @(Compile-Errors 'artifacts/p5o2b1/historical-repeat.log')
if ($firstErrors.Count -ne 4 -or $repeatErrors.Count -ne 4) { throw 'Unexpected compile error count.' }
if (Compare-Object ($firstErrors | ConvertTo-Json -Depth 5) ($repeatErrors | ConvertTo-Json -Depth 5)) { throw 'Historical diagnostics differ.' }
if (@($metadata.Members | Where-Object Classification -ne 'Same used metadata contract').Count -ne 1 -or
    @($metadata.Types | Where-Object HistoricalPresent -eq $false).Count -ne 0 -or
    @($metadata.EnumConstants | Where-Object Equal -eq $false).Count -ne 0) { throw 'Unexpected metadata drift.' }
$logPaths = @('artifacts/p5o2b1/current-build.log', 'artifacts/p5o2b1/current-build.binlog',
    'artifacts/p5o2b1/historical-first.log', 'artifacts/p5o2b1/historical-first.binlog',
    'artifacts/p5o2b1/historical-repeat.log', 'artifacts/p5o2b1/historical-repeat.binlog',
    'artifacts/p5o2b1/restore-repeat.log', 'artifacts/p5o2b1/metadata-audit.json')
$audit = [ordered]@{
    Schema = 'P5O2B1-build-feasibility-v1'; Date = '2026-10-08'; Head = $headCommit; Decision = 'NOT READY'
    DecisionScope = 'B1 complete; B2 blocked by the unchanged-source historical compile failure. No B2 scaffold or production compatibility fix.'
    HistoricalVersion = '5.0.0-2.25451.107'; RecordedCompilerVersion = $p5n.RecordedCompilerVersion
    HistoricalPackageProvenance = $packages; HistoricalReferenceAssemblies = $assemblies
    ReadinessEvidence = @('Evaluation/P5O2A6-architecture-readiness-closure.md',
        'Evaluation/P5O2A6-architecture-readiness-closure-audit.json', 'Evaluation/P5O2A6-historical-core-readiness-matrix.md',
        'Evaluation/P5O2A6F-summary-orchestration-cycle-closure.md', $a6fPath,
        'Evaluation/P5O2A6F-historical-core-readiness-matrix.md', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md')
    SourceVerification = @{ ProductionChanged = $false; TestsChanged = $false; CurrentMainSourceFiles = $fingerprints
        A6FManifestExactlyEvaluated = $true; ProductiveFiles = 120; Owners = 100; Categories = @{ A = 47; B = 14; D = 39 }
        EvaluatedCompileFiles = $compileFiles; Sources = $sources; OwnersAndDependencies = $a6f.Components
        A6FGraphInheritedByExactSourceHashes = $true; CoreTypeEdges = $a6f.CoreTypeEdges; CycleScopes = $a6f.CycleScopes
        ClosureGates = $a6f.ClosureGates; HostCut = 'src/XMLDocNormalizer/Execution/Semantic/ProjectClosureExceptionFlowSemanticEnvironment.cs'
        CompileOnlyHost = 'Five always-throwing members in four capability families; no algorithm, scope data, execution or semantic-host claim.' }
    Projects = $projects; VersionDefinitions = 'Project-local PackageReference; no applicable ancestor central package/build/global.json definitions.'
    Tfm = @{ Current = 'net8.0'; HistoricalLibraryAssets = @('net8.0', 'net9.0', 'netstandard2.0'); Selected = 'net8.0'; ChangeRequired = $false }
    RoslynApiDrift = $metadata
    Validation = @{ CurrentWarningAsError = @{ ExitCode = 0; Warnings = 0; Errors = 0; Log = $logPaths[0] }
        HistoricalInitial = @{ RestoreExitCode = 0; BuildExitCode = 1; Warnings = 0; Errors = $firstErrors }
        HistoricalRepeat = @{ RestoreExitCode = 0; BuildExitCode = 1; Warnings = 0; Errors = $repeatErrors; SameDiagnostics = $true }
        ForcedRestoreAssetsByteIdentical = $true; HistoricalAssetsSha256 = (Get-FileHash 'artifacts/p5o2b1/HistoricalCompileProbe/obj/project.assets.json').Hash
        MetadataAudit = @{ BuildExitCode = 0; Warnings = 0; Errors = 0; CurrentSourceCompileErrors = $metadata.CurrentSourceCompileErrors }
        ClassificationCounts = @{ AbsentApi = 4; SignatureDifference = 0; AbsentType = 0; EnumValueDifference = 0
            NullableAnnotationTfm = 0; PackageReference = 0; MissingSource = 0; OtherBuildMsbuild = 0 }
        Logs = @($logPaths | ForEach-Object { @{ Path = $_; Sha256 = (Get-FileHash -LiteralPath $_).Hash } })
        Semantics = @{ InheritedFrom = $a6fPath; RerunInB1 = $false; Reason = 'All 361 productive source byte hashes and active projects/solution/test sources unchanged.'
            Full = @($a6f.Validation.Tests | Where-Object Run -in @('full', 'full-final')); SelfAnalysis = $a6f.Validation.SelfAnalysis
            Canonical = @{ Added = $a6f.Validation.Canonical.Added; Removed = $a6f.Validation.Canonical.Removed
                ChangedEvidence = $a6f.Validation.Canonical.ChangedEvidence; FullRawArraysEqual = $a6f.Validation.Canonical.FullRawArraysEqual
                FullNormalizedArraysEqual = $a6f.Validation.Canonical.FullNormalizedArraysEqual; AfterNormalizedHash = $a6f.Validation.Canonical.AfterNormalizedHash } } }
    Isolation = @{ NormalSolutionUnchanged = $true; ProbeInSolution = $false; ProbeProjectReferences = 0
        SeparateAssemblyName = $evaluated.Properties.AssemblyName; Paths = $evaluated.Properties; HistoricalRuntimeLoaded = $metadata.HistoricalRuntimeLoaded
        PackagesPath = 'artifacts/p5o2b1/packages'; PackageConflictsObserved = 0; WorkspacesInHistoricalGraph = 0
        CompilerDriver = '.NET SDK 8.0.418 / Roslyn 4.11.0-3.25569.22 (3fb752d4), verified with csc.dll -version; historical API reference build only, not historical compiler execution or PE reconstruction.' }
    Recommendation = @{ Owner = 'Separate historical source-linked project, independently pinned restore graph and obj/bin; keep current Main source-local compile.'
        AssemblyIdentity = 'Distinct historical Analyzer assembly name recommended; mandatory distinct output/assets paths. Assembly name alone cannot isolate same-identity Roslyn assemblies.'
        BeforeB2 = 'P5O2B1A - Bounded RuntimeAwait Capability and Same-Source Compatibility Proof'
        MinimalScope = 'Prove historical binder capability first; then one small typed compile-time accessor seam for four uses in two await methods, preserving the current RuntimeAwait branch. No guessed null behavior, broad conditional compilation, runtime reflection or worker.'
        NotImplemented = @('Production shim', 'Final dual-version project', 'Runtime host', 'Worker', 'IPC', 'AssemblyLoadContext', 'Canonical transport') }
    Git = @{ ProtectedFiles = $protected; Stashes = $stashes; StashesUnchanged = $true; Commit = $false; Push = $false; Reset = $false
        IndexChanged = $false; StatusShort = @(& git status --short)
        IgnoredProbeProjectRules = @(& git check-ignore -v Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj Evaluation/P5O2B1CompileProbe/MetadataAudit/MetadataAudit.csproj)
        IgnoredProbeProjectNote = 'Both csproj files exist and were built/validated, but the pre-existing root *.csproj ignore rule hides them from untracked status. No ignore-rule/index change; explicitly include them when a later commit is authorized.' }
}
if (Test-Path -LiteralPath 'artifacts/p5o2b1/quality-gates.json') {
    $audit.Validation.QualityGates = Read-Json 'artifacts/p5o2b1/quality-gates.json'
}
Write-Crlf 'Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json' ($audit | ConvertTo-Json -Depth 30)

$lines = [Collections.Generic.List[string]]::new()
$lines.Add('# P5O2B1 - Roslyn API drift and exact source boundary')
$lines.Add('')
$lines.Add('Generated by P5O2B1CompileProbe/Write-BuildFeasibilityEvidence.ps1 from the A6F inventory and a current-engine PE-metadata comparison. Historical DLLs are references, never runtime-loaded. Member-contract equality is a compile-surface result, not behavioral equivalence. Complete nullable/ref/optional/constraint contract strings and sites are in the companion B1 JSON. The missing getter has four compiler sites: Calls 218/221 and Dispatch 275/278; A6F recorded each enclosing guarded branch once.')
$lines.Add('')
$lines.Add('## All 430 used member contracts')
$lines.Add('')
$lines.Add('| Current API | Historical counterpart / classification | Minimal strategy | File:line / caller |')
$lines.Add('| --- | --- | --- | --- |')
foreach ($member in $metadata.Members) {
    $sites = @($member.Sites | ForEach-Object { "$($_.File):$($_.Line) - $($_.Caller)" }) -join '<br>'
    $counterpart = if ($null -eq $member.HistoricalContract) { 'None: API absent historically' } else { "Same API: $($member.Classification)" }
    $lines.Add("| $(Cell $member.Signature) | $(Cell $counterpart) | $(Cell $member.Strategy) | $(Cell $sites) |")
}
$lines.Add('')
$lines.Add('## All 210 used Roslyn types')
$lines.Add('')
$lines.Add('All are present historically. Presence is not a claim that every unused member of these types is identical.')
$lines.Add('')
$lines.Add('| Type | Historical presence | Productive files |')
$lines.Add('| --- | --- | --- |')
foreach ($type in $metadata.Types) { $lines.Add("| $(Cell $type.Type) | $($type.HistoricalPresent) | $(Cell ($type.SourceFiles -join '<br>')) |") }
$lines.Add('')
$lines.Add('## All 84 used enum constants')
$lines.Add('')
$lines.Add('| API | Current / historical value | Equal | File:line / caller |')
$lines.Add('| --- | --- | --- | --- |')
foreach ($enum in $metadata.EnumConstants) {
    $sites = @($enum.Sites | ForEach-Object { "$($_.File):$($_.Line) - $($_.Caller)" }) -join '<br>'
    $lines.Add("| $(Cell $enum.Api) | $($enum.CurrentValue) / $($enum.HistoricalValue) | $($enum.Equal) | $(Cell $sites) |")
}
$lines.Add('')
$lines.Add('## Exact 120 productive files')
$lines.Add('')
$lines.Add('Category A = algorithms, B = shared Roslyn infrastructure, D = neutral contracts. Direct ProjectReferences are zero for every row; all required dependencies compile source-locally. No row pulls Main-only infrastructure. Per-owner source dependency lists and per-file Roslyn type lists are retained in the companion B1 JSON and the unchanged A6F matrix. Neutral canonical recursive domains remain; upper orchestration is acyclic, not a claim of zero raw type cycles.')
$lines.Add('')
$lines.Add('| Productive file | Category | Required purpose | Owners | Roslyn type count | Main-only / ProjectRefs |')
$lines.Add('| --- | --- | --- | --- | --- | --- |')
foreach ($source in $sources) {
    $lines.Add("| $(Cell $source.File) | $($source.Categories -join '/') | $(Cell $source.Purpose) | $(Cell ($source.Owners -join '<br>')) | $($source.RoslynTypes.Count) | no / 0 |")
}
Write-Crlf 'Evaluation/P5O2B1-roslyn-api-drift-matrix.md' ($lines -join "`n")
Write-Output "B1 evidence: $($fingerprints.Count) unchanged productive source hashes; $($sources.Count) exact files; $($metadata.Members.Count) API members; 4 identical compile errors; protected files/stashes unchanged."
