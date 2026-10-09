param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$RunMainBoundaryTests, [switch]$BuildTestProjectReferences)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceRoot = Join-Path $repoRoot 'artifacts/dual-version-build'
New-Item -ItemType Directory -Path $evidenceRoot -Force | Out-Null
$startedUtc = [DateTime]::UtcNow.ToString('o')
# A failed rerun must not leave an old success audit masquerading as this run.
[ordered]@{ StartedUtc = $startedUtc; Passed = $false; State = 'Running or interrupted' } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidenceRoot 'audit.json') -Encoding UTF8
$current = Join-Path $repoRoot 'src/XMLDocNormalizer/XMLDocNormalizer.csproj'
$historical = Join-Path $repoRoot 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj'
$worker = Join-Path $repoRoot 'src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj'
$steps = [Collections.Generic.List[object]]::new()

function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Invoke-DotNet([string]$Name, [string[]]$Arguments) {
    # PS5 wraps native stderr as ErrorRecords; retain failing test diagnostics before checking the exit code.
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & dotnet @Arguments 2>&1 | Out-String
        $exitCode = $LASTEXITCODE
    } finally { $ErrorActionPreference = $previousPreference }
    $output | Set-Content -LiteralPath (Join-Path $evidenceRoot ($Name + '.log')) -Encoding UTF8
    Write-Host $output
    Require ($exitCode -eq 0) "$Name failed with exit code $exitCode."
    $steps.Add([ordered]@{ Name = $Name; Arguments = $Arguments; ExitCode = $exitCode })
}

function Get-Project([string]$Project) {
    $json = & dotnet msbuild $Project '-getItem:Compile,ProjectReference' '-getProperty:TargetFramework,AssemblyName,TargetPath,ProjectAssetsFile,RestorePackagesPath,MSBuildProjectExtensionsPath,BaseOutputPath' "-p:Configuration=$Configuration" -nologo
    Require ($LASTEXITCODE -eq 0) "Could not evaluate $Project."
    return ($json -join "`n" | ConvertFrom-Json)
}

function Get-PackageGraph([string]$AssetsPath, [bool]$AllowHistoricalProject = $false) {
    $assets = Get-Content -LiteralPath $AssetsPath -Raw | ConvertFrom-Json
    return @($assets.libraries.PSObject.Properties | ForEach-Object {
        if ($_.Value.type -eq 'project') {
            Require ($AllowHistoricalProject -and $_.Name -eq 'XMLDocNormalizer.ExceptionFlow.Historical/1.0.0') 'Unexpected worker project dependency.'
            return
        }
        Require ($_.Value.type -eq 'package') "Unexpected non-package dependency: $($_.Name)"
        [ordered]@{ Identity = $_.Name; Sha512 = $_.Value.sha512; Path = $_.Value.path }
    })
}

function Invoke-Worker([string]$Name, [string]$WorkerPath, [string]$Request) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = 'dotnet'
    $start.Arguments = '"' + $WorkerPath + '"'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardInput = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        Require ($process.Start()) 'Could not start the owned Historical Worker.'
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        $process.StandardInput.Write($Request)
        $process.StandardInput.Close()
        if (-not $process.WaitForExit(30000)) {
            $process.Kill()
            throw 'Owned Historical Worker exceeded its deadline.'
        }
        $text = $stdout.Result.Trim()
        $errors = $stderr.Result.Trim()
        $text | Set-Content -LiteralPath (Join-Path $evidenceRoot ($Name + '.json')) -Encoding UTF8
        $errors | Set-Content -LiteralPath (Join-Path $evidenceRoot ($Name + '.stderr.log')) -Encoding UTF8
        return [ordered]@{ Name = $Name; ExitCode = $process.ExitCode; Response = ($text | ConvertFrom-Json); Stdout = $text; Stderr = $errors }
    } finally {
        $process.Dispose()
    }
}

function Get-RoslynReferences([string]$Project, [string]$ExpectedVersion) {
    $json = & dotnet msbuild $Project '-t:ResolveReferences' '-getItem:ReferencePath' "-p:Configuration=$Configuration" -warnaserror -nologo -v:quiet
    Require ($LASTEXITCODE -eq 0) "Could not resolve compiler references for $Project."
    $references = ($json -join "`n" | ConvertFrom-Json).Items.ReferencePath
    return @($references | Where-Object { $_.NuGetPackageId -in @('Microsoft.CodeAnalysis.Common', 'Microsoft.CodeAnalysis.CSharp') } | ForEach-Object {
        Require ($_.NuGetPackageVersion -eq $ExpectedVersion) "Unexpected Roslyn reference version: $($_.FullPath)"
        [ordered]@{ Package = $_.NuGetPackageId; Version = $_.NuGetPackageVersion; Path = $_.FullPath; Sha256 = (Get-FileHash -LiteralPath $_.FullPath -Algorithm SHA256).Hash }
    })
}

function Get-Image([string]$Path) {
    Require (Test-Path -LiteralPath $Path -PathType Leaf) "Missing image $Path."
    # GetAssemblyName reads metadata; never Assembly.Load/LoadFrom or historical execution.
    return [ordered]@{
        Path = $Path
        Identity = [Reflection.AssemblyName]::GetAssemblyName($Path).FullName
        Sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
        PdbSha256 = (Get-FileHash -LiteralPath ([IO.Path]::ChangeExtension($Path, '.pdb')) -Algorithm SHA256).Hash
    }
}

function Get-TreeFingerprint([string]$Directory) {
    return (@(Get-ChildItem -LiteralPath $Directory -File -Recurse | Sort-Object FullName | ForEach-Object {
        $_.FullName + ':' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }) -join "`n")
}

Push-Location $repoRoot
try {
    Invoke-DotNet 'restore-current' @('restore', $current, '-warnaserror')
    Invoke-DotNet 'restore-historical' @('restore', $historical, '-warnaserror')
    Invoke-DotNet 'restore-worker' @('restore', $worker, '-warnaserror')
    $c = Get-Project $current
    $h = Get-Project $historical
    $w = Get-Project $worker
    if (-not $c.Properties.RestorePackagesPath) {
        $currentAssets = Get-Content -LiteralPath $c.Properties.ProjectAssetsFile -Raw | ConvertFrom-Json
        $c.Properties.RestorePackagesPath = @($currentAssets.packageFolders.PSObject.Properties.Name)[0]
    }
    Require ($c.Properties.TargetFramework -eq 'net8.0' -and $h.Properties.TargetFramework -eq 'net8.0') 'Unexpected TFM.'
    Require ($h.Properties.AssemblyName -eq 'XMLDocNormalizer.ExceptionFlow.Historical') 'Historical identity changed.'
    Require ($c.Properties.AssemblyName -ne $h.Properties.AssemblyName) 'Assembly collision.'
    Require (@($c.Items.ProjectReference).Count -eq 0 -and @($h.Items.ProjectReference).Count -eq 0) 'Main/Historical must not have runtime project references.'
    $expectedRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts/exception-flow-historical')) + [IO.Path]::DirectorySeparatorChar
    foreach ($property in @('TargetPath', 'ProjectAssetsFile', 'RestorePackagesPath', 'MSBuildProjectExtensionsPath', 'BaseOutputPath')) {
        $full = [IO.Path]::GetFullPath($h.Properties.$property)
        Require ($full.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) "Historical $property is not isolated."
        Require ($full -ne [IO.Path]::GetFullPath($c.Properties.$property)) "$property collision."
    }
    $mainSourceRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'src/XMLDocNormalizer')) + [IO.Path]::DirectorySeparatorChar
    $shared = @($h.Items.Compile | Where-Object { $_.FullPath.StartsWith($mainSourceRoot, [StringComparison]::OrdinalIgnoreCase) })
    Require ($shared.Count -eq 121 -and @($h.Items.Compile).Count -eq 122) 'Expected original 120 + capability + one compile-local semantic host.'
    Require (@($h.Items.Compile | Where-Object { $_.Identity -eq 'HistoricalSemanticEnvironment.cs' }).Count -eq 1) 'Executable Historical host missing.'
    Require (@($shared.FullPath | Sort-Object -Unique).Count -eq 121) 'Duplicate shared sources.'
    Require (@($shared | Where-Object { $_.FullPath.EndsWith('ExceptionFlowRuntimeAwaitCapability.cs') }).Count -eq 1) 'Shared capability missing.'
    $fingerprints = @($shared | Sort-Object FullPath | ForEach-Object {
        Require ($c.Items.Compile.FullPath -contains $_.FullPath) "Not shared with Current: $($_.FullPath)"
        [ordered]@{ Path = $_.FullPath.Substring($repoRoot.Length + 1).Replace('\', '/'); Sha256 = (Get-FileHash -LiteralPath $_.FullPath -Algorithm SHA256).Hash }
    })
    $cg = Get-PackageGraph $c.Properties.ProjectAssetsFile
    $hg = Get-PackageGraph $h.Properties.ProjectAssetsFile
    $wg = Get-PackageGraph $w.Properties.ProjectAssetsFile $true
    $expectedHistorical = @('Microsoft.CodeAnalysis.Analyzers/3.11.0', 'Microsoft.CodeAnalysis.Common/5.0.0-2.25451.107', 'Microsoft.CodeAnalysis.CSharp/5.0.0-2.25451.107', 'System.Collections.Immutable/9.0.0', 'System.Reflection.Metadata/9.0.0')
    Require ((@($hg.Identity | Sort-Object) -join ';') -eq (($expectedHistorical | Sort-Object) -join ';')) 'Historical package graph drift.'
    $expectedWorker = @($expectedHistorical | Where-Object { $_ -notlike 'Microsoft.CodeAnalysis.Analyzers/*' })
    Require ((@($wg.Identity | Sort-Object) -join ';') -eq (($expectedWorker | Sort-Object) -join ';')) 'Worker package graph drift.'
    Require (@($w.Items.ProjectReference).Count -eq 1 -and $w.Items.ProjectReference[0].FullPath -eq $historical) 'Worker must reference only Historical.'
    Require ($w.Properties.TargetFramework -eq 'net8.0' -and $w.Properties.AssemblyName -eq 'XMLDocNormalizer.HistoricalWorker') 'Worker identity/TFM drift.'
    foreach ($property in @('TargetPath', 'ProjectAssetsFile', 'MSBuildProjectExtensionsPath', 'BaseOutputPath')) {
        $path = [IO.Path]::GetFullPath($w.Properties.$property)
        Require ($path.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) "Worker $property not isolated."
        Require ($path -ne [IO.Path]::GetFullPath($h.Properties.$property) -and $path -ne [IO.Path]::GetFullPath($c.Properties.$property)) "Worker $property collision."
    }
    foreach ($package in @('Microsoft.CodeAnalysis.Common/5.0.0', 'Microsoft.CodeAnalysis.CSharp/5.0.0')) {
        Require ($cg.Identity -contains $package) "Current Roslyn version changed: $package"
    }
    Require (@($cg.Identity | Where-Object { $_ -match '5\.0\.0-2\.25451\.107' }).Count -eq 0) 'Historical package leaked into Current.'
    $provenance = @($hg | ForEach-Object {
        $metadata = Get-Content -LiteralPath (Join-Path (Join-Path $h.Properties.RestorePackagesPath $_.Path) '.nupkg.metadata') -Raw | ConvertFrom-Json
        [ordered]@{ Identity = $_.Identity; Source = $metadata.source; ContentHash = $metadata.contentHash }
    })
    $currentReferences = Get-RoslynReferences $current '5.0.0'
    $historicalReferences = Get-RoslynReferences $historical '5.0.0-2.25451.107'
    Require ($currentReferences.Count -eq 2 -and $historicalReferences.Count -eq 2) 'Expected both compiler API assemblies.'
    foreach ($reference in $historicalReferences) {
        Require ([IO.Path]::GetFullPath($reference.Path).StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) 'Historical reference came from Current/global cache.'
    }
    $images = [Collections.Generic.List[object]]::new()
    # Both orders, forced compilation each time; clean one target never touches the other.
    $order = @('current', 'historical', 'current', 'historical', 'historical', 'current')
    for ($index = 0; $index -lt $order.Count; $index++) {
        $owner = $order[$index]
        $project = $current
        $evaluation = $c
        $other = $h
        if ($owner -eq 'historical') { $project = $historical; $evaluation = $h; $other = $c }
        $otherBin = Split-Path $other.Properties.TargetPath -Parent
        $otherBefore = ''
        if (Test-Path -LiteralPath $otherBin) { $otherBefore = Get-TreeFingerprint $otherBin }
        $otherAssets = (Get-FileHash -LiteralPath $other.Properties.ProjectAssetsFile -Algorithm SHA256).Hash
        Invoke-DotNet "$index-$owner-clean" @('clean', $project, "-p:Configuration=$Configuration", '-warnaserror', '-v:minimal')
        Require (-not (Test-Path -LiteralPath $evaluation.Properties.TargetPath)) "$owner clean did not remove its own image; compilation would not be forced."
        Invoke-DotNet "$index-$owner-build" @('build', $project, '--no-restore', "-p:Configuration=$Configuration", '-warnaserror')
        if ($otherBefore) { Require ((Get-TreeFingerprint $otherBin) -eq $otherBefore) "$owner changed the other target's bin." }
        Require ((Get-FileHash -LiteralPath $other.Properties.ProjectAssetsFile -Algorithm SHA256).Hash -eq $otherAssets) "$owner changed the other assets graph."
        $images.Add([ordered]@{ Step = $index; Owner = $owner; Image = (Get-Image $evaluation.Properties.TargetPath); OtherBinAndAssetsUntouched = $true })
    }
    foreach ($owner in @('current', 'historical')) {
        $ownerImages = @($images | Where-Object { $_.Owner -eq $owner })
        Require (@($ownerImages.Image.Sha256 | Sort-Object -Unique).Count -eq 1) "$owner DLL is not repeatable."
        Require (@($ownerImages.Image.PdbSha256 | Sort-Object -Unique).Count -eq 1) "$owner PDB is not repeatable."
    }
    $workerImages = @()
    $currentBin = Split-Path $c.Properties.TargetPath -Parent
    $historicalBin = Split-Path $h.Properties.TargetPath -Parent
    $currentBeforeWorker = Get-TreeFingerprint $currentBin
    $historicalBeforeWorker = Get-TreeFingerprint $historicalBin
    for ($index = 0; $index -lt 2; $index++) {
        Invoke-DotNet "worker-$index-clean" @('clean', $worker, "-p:Configuration=$Configuration", '-p:BuildProjectReferences=false', '-warnaserror', '-v:minimal')
        Require (-not (Test-Path -LiteralPath $w.Properties.TargetPath)) 'Worker clean failed to remove its image.'
        Invoke-DotNet "worker-$index-build" @('build', $worker, '--no-restore', "-p:Configuration=$Configuration", '-p:BuildProjectReferences=false', '-warnaserror')
        $workerImages += Get-Image $w.Properties.TargetPath
        Require ((Get-TreeFingerprint $currentBin) -eq $currentBeforeWorker) 'Worker build changed Current bin.'
        Require ((Get-TreeFingerprint $historicalBin) -eq $historicalBeforeWorker) 'Worker-only build changed Historical library bin.'
    }
    Require (@($workerImages.Sha256 | Sort-Object -Unique).Count -eq 1 -and @($workerImages.PdbSha256 | Sort-Object -Unique).Count -eq 1) 'Worker DLL/PDB not repeatable.'
    $workerBin = Split-Path $w.Properties.TargetPath -Parent
    foreach ($reference in $historicalReferences) {
        $copy = Join-Path $workerBin ([IO.Path]::GetFileName($reference.Path))
        Require ((Get-FileHash -LiteralPath $copy).Hash -eq $reference.Sha256) 'Worker dependency copy does not match Historical Roslyn.'
    }
    Require ((Get-FileHash -LiteralPath (Join-Path $workerBin 'XMLDocNormalizer.ExceptionFlow.Historical.dll')).Hash -eq $images[1].Image.Sha256) 'Worker historical-library copy differs.'
    $identityRun = Invoke-Worker 'worker-identity' $w.Properties.TargetPath '{"protocolVersion":2,"operation":"identity"}'
    Require ($identityRun.ExitCode -eq 0 -and $identityRun.Response.Success -and -not $identityRun.Stderr) 'Worker identity failed.'
    $engines = @($identityRun.Response.Identity.LoadedRoslyn)
    Require ($engines.Count -eq 2 -and -not $identityRun.Response.Identity.RuntimeAwaitInformationAvailable) 'Historical capability/universe mismatch.'
    foreach ($engine in $engines) {
        Require ($engine.InformationalVersion.StartsWith('5.0.0-2.25451.107+', [StringComparison]::Ordinal)) 'Worker loaded non-historical Roslyn.'
        Require ($historicalReferences.Sha256 -contains $engine.Sha256) 'Runtime image differs from historical compile references.'
    }
    $request = '{"protocolVersion":2,"operation":"analyze","payload":{"source":"public static class Fixture { public static void Root() { Thrower(); } static void Thrower() { throw null; } }","typeMetadataName":"Fixture","methodName":"Root"}}'
    $firstSmoke = Invoke-Worker 'worker-smoke-first' $w.Properties.TargetPath $request
    $secondSmoke = Invoke-Worker 'worker-smoke-repeat' $w.Properties.TargetPath $request
    Require ($firstSmoke.ExitCode -eq 0 -and $secondSmoke.ExitCode -eq 0 -and $firstSmoke.Response.Success -and $secondSmoke.Response.Success) 'Real Historical analysis smoke failed.'
    Require (-not $firstSmoke.Stderr -and -not $secondSmoke.Stderr -and $firstSmoke.Stdout -eq $secondSmoke.Stdout) 'Worker response not clean/deterministic.'
    Require (@($firstSmoke.Response.Result.Entries).Count -eq 1 -and $firstSmoke.Response.Result.Entries[0].ExceptionType.MetadataName -eq 'NullReferenceException') 'Real exception flow missing.'
    Require (@($firstSmoke.Response.Result.Uncertainties).Count -eq 0 -and @($firstSmoke.Response.Result.Entries[0].Paths[0].Steps).Count -eq 2) 'Expected complete transitive call/throw path.'
    $invalidVersion = Invoke-Worker 'worker-invalid-version' $w.Properties.TargetPath '{"protocolVersion":99,"operation":"identity"}'
    Require ($invalidVersion.ExitCode -eq 1 -and -not $invalidVersion.Response.Success -and $invalidVersion.Response.Failure.Code -eq 'unsupportedProtocolVersion') 'Worker failed-open protocol version.'
    # One existing gate/CI owner; this fixture is only an adversarial pipe peer, never another Analyzer.
    Invoke-DotNet 'boundary-test-process-build' @('build', (Join-Path $repoRoot 'Evaluation/P5O2B4Proof/TestProcess/BoundaryTestProcess.csproj'), "-p:Configuration=$Configuration", '-warnaserror')
    $mainBoundaryExecuted = $false
    if ($RunMainBoundaryTests) {
        # Test fixture emits a real PDB with the loaded Historical compiler, not the SDK that built the Analyzer DLL.
        $pdbFixture = Join-Path $repoRoot 'Evaluation/P5O2B7Proof/HistoricalPdbFixture/HistoricalPdbFixture.csproj'
        Invoke-DotNet 'projection-pdb-fixture-restore' @('restore', $pdbFixture, '--configfile', (Join-Path $repoRoot 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config'))
        Invoke-DotNet 'projection-pdb-fixture-build' @('build', $pdbFixture, '--no-restore', "-p:Configuration=$Configuration", '-warnaserror', '-p:BuildProjectReferences=false')
        $pdbFixtureDll = Join-Path $repoRoot "artifacts/exception-flow-historical/HistoricalPdbFixture/bin/$Configuration/net8.0/HistoricalPdbFixture.dll"
        Invoke-DotNet 'projection-pdb-fixture-emit' @($pdbFixtureDll, (Join-Path $repoRoot 'artifacts/p5o2b7/historical-pdb'))
        $testProject = Join-Path $repoRoot 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
        $testBuild = @('build', $testProject, "-p:Configuration=$Configuration", '-warnaserror')
        if (-not $BuildTestProjectReferences) { $testBuild += @('--no-restore', '-p:BuildProjectReferences=false') }
        Invoke-DotNet 'main-boundary-tests-build' $testBuild
        Invoke-DotNet 'main-boundary-tests' @('test', $testProject, '--no-build', '--no-restore', "-p:Configuration=$Configuration", '--filter', 'FullyQualifiedName~HistoricalWorker|FullyQualifiedName~HistoricalAnalyzerBuildProjectTests|FullyQualifiedName~ExceptionFlowAnalysisRouterTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionPolicyTests|FullyQualifiedName~ExceptionFlowAnalyzerSelectionProjectionTests', '--logger', 'trx;LogFileName=main-boundary.trx', '--results-directory', $evidenceRoot)
        $mainBoundaryExecuted = $true
    }
    $result = [ordered]@{
        StartedUtc = $startedUtc; CompletedUtc = [DateTime]::UtcNow.ToString('o')
        Configuration = $Configuration; Sdk = (& dotnet --version); HistoricalVersion = '5.0.0-2.25451.107'
        CurrentProperties = $c.Properties; HistoricalProperties = $h.Properties
        WorkerProperties = $w.Properties; WorkerPackages = $wg; WorkerBuildImages = $workerImages
        WorkerRuntimeChecks = @($identityRun, $firstSmoke, $secondSmoke, $invalidVersion)
        CurrentPackages = $cg; HistoricalPackages = $hg; HistoricalPackageSources = $provenance
        CurrentCompilerReferences = $currentReferences; HistoricalCompilerReferences = $historicalReferences
        SharedSourceFingerprints = $fingerprints; SharedCount = 121; HostCount = 1
        Steps = @($steps.ToArray()); BuildImages = @($images.ToArray()); Passed = $true
        RuntimeExecuted = $true
        MainBoundaryExecuted = $mainBoundaryExecuted
    }
    $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'audit.json') -Encoding UTF8
    Write-Host 'PASS: Current/Historical/Worker build, exact runtime identities, real deterministic isolated analysis and fail-closed protocol.'
} finally {
    Pop-Location
}
