param([Parameter(Mandatory=$true)][ValidateSet('BaselineClose','Model','Build','New','Regressions','Boundary','Dual')][string]$Phase)
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $tool 'artifacts/p5o2b25a2/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$records=Join-Path $tool 'artifacts/p5o2b25a2/runs'
[IO.Directory]::CreateDirectory($records) | Out-Null
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Recorded([string]$Name,[string]$Exe,[string[]]$Arguments) {
    $record=Join-Path $records ($Name+'.json')
    Require (-not (Test-Path $record)) 'Preserve the actual attempt; no overwrite or automatic retry.'
    $started=[DateTime]::UtcNow.ToString('o')
    [ordered]@{Name=$Name;StartedUtc=$started;ExitCode=$null;State='Running or interrupted';Arguments=$Arguments} |
        ConvertTo-Json -Depth 8 | Set-Content $record -Encoding UTF8
    $preference=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& $Exe @Arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$preference }
    [IO.File]::WriteAllText((Join-Path $records ($Name+'.log')),$log,[Text.UTF8Encoding]::new($false))
    Write-Host $log
    [ordered]@{Name=$Name;StartedUtc=$started;FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$code;Executable=$Exe;Arguments=$Arguments} |
        ConvertTo-Json -Depth 8 | Set-Content $record -Encoding UTF8
    Require ($code -eq 0) "$Name failed; actual attempt retained. Inspect rather than retry."
}
Push-Location $validation
try {
    $project=Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
    if($Phase -eq 'BaselineClose') {
        $audit=Get-Content 'artifacts/dual-version-build/audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
        Require ($audit.Passed -and $audit.MainBoundaryExecuted -and $audit.RuntimeExecuted) 'Fresh captured-WIP baseline incomplete.'
        $destination=Join-Path $tool 'artifacts/p5o2b25a2/baseline'
        Require (-not (Test-Path $destination)) 'Baseline already captured.'
        [IO.Directory]::CreateDirectory($destination) | Out-Null
        Copy-Item 'artifacts/dual-version-build' (Join-Path $destination 'dual-version-build') -Recurse
        Copy-Item 'artifacts/p5o2b9/current' (Join-Path $destination 'current') -Recurse
        Copy-Item 'artifacts/p5o2b25/audit-cases' (Join-Path $destination 'b25-cases') -Recurse
        Require (@(Get-ChildItem (Join-Path $destination 'current') -Filter '*.json').Count -eq 8) 'Eight baseline files required.'
    }
    if($Phase -in @('Model','Build')) {
        Require (Test-Path (Join-Path $tool 'artifacts/p5o2b25a2/baseline/dual-version-build/audit.json')) 'Capture baseline before overlay.'
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B23Proof/Sync-Validation.ps1') -ValidationRoot $validation
        Require ($LASTEXITCODE -eq 0) 'Overlay failed.'
        if($Phase -eq 'Build') {
            Recorded 'current-clean' 'dotnet' @('clean',(Join-Path $validation 'src/XMLDocNormalizer/XMLDocNormalizer.csproj'),'-p:BuildProjectReferences=false','-warnaserror','-v:minimal')
            Recorded 'historical-clean' 'dotnet' @('clean',(Join-Path $validation 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj'),'-warnaserror','-v:minimal')
        }
        Recorded ($Phase.ToLowerInvariant()+'-tests-wae') 'dotnet' @('build',$project,'--no-restore','-warnaserror')
        if($Phase -eq 'Build') {
            Recorded 'historical-wae' 'dotnet' @('build',(Join-Path $validation 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj'),'--no-restore','-warnaserror')
            Recorded 'worker-wae' 'dotnet' @('build',(Join-Path $validation 'src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj'),'--no-restore','-p:BuildProjectReferences=false','-warnaserror')
            Recorded 'graph-probe-wae' 'dotnet' @('build',(Join-Path $validation 'Evaluation/P5O2B7Proof/HistoricalPdbFixture/HistoricalPdbFixture.csproj'),'--no-restore','-warnaserror')
        }
        if($Phase -eq 'Model') {
            Recorded 'compiler-model-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests','--logger','trx;LogFileName=compiler-model.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a2/tests'))
        }
    }
    if($Phase -eq 'New') {
        Recorded 'new-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests','--logger','trx;LogFileName=identity.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a2/tests'))
    }
    if($Phase -eq 'Regressions') {
        Recorded 'pid-g1-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~HistoricalWorkerLifetimeTests','--logger','trx;LogFileName=pid-g1.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a2/tests'))
        Recorded 'pid-g1-proof' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/PIDG1Proof/Test-ProcessLifetime.ps1'),'-OutputPath',(Join-Path $records 'pid-g1-proof-cases.json'))
        Recorded 'regressions' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'),'-ValidationRoot',$validation,'-AdditionalFilter','FullyQualifiedName~HistoricalWorkerLifetimeTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests')
    }
    if($Phase -eq 'Boundary') {
        $line=Get-Content 'build/Verify-DualVersionBuild.ps1' -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
        Require ($line -match "'--filter', '([^']+)'") 'Permanent boundary filter missing.'
        Recorded 'boundary' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter',$Matches[1],'--logger','trx;LogFileName=boundary.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a2/tests'))
    }
    if($Phase -eq 'Dual') {
        Recorded 'dual' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $validation 'build/Verify-DualVersionBuild.ps1'),'-RunMainBoundaryTests','-BuildTestProjectReferences')
    }
} finally { Pop-Location }
