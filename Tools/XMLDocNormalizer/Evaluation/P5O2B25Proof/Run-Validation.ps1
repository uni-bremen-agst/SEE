param([Parameter(Mandatory=$true)][ValidateSet('BaselineClose','Build','New','Regressions','Boundary','Dual')][string]$Phase,
    [string]$Attempt='first')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $tool 'artifacts/p5o2b25/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$records=Join-Path $tool 'artifacts/p5o2b25/runs'
[IO.Directory]::CreateDirectory($records) | Out-Null
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Recorded([string]$Name,[string]$Exe,[string[]]$Arguments) {
    $record=Join-Path $records ($Name+'-'+$Attempt+'.json')
    Require (-not (Test-Path $record)) 'Preserve attempts; no overwrite or automatic retry.'
    $started=[DateTime]::UtcNow.ToString('o')
    [ordered]@{Name=$Name;StartedUtc=$started;ExitCode=$null;State='Running or interrupted';Arguments=$Arguments} |
        ConvertTo-Json -Depth 8 | Set-Content $record -Encoding UTF8
    $preference=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& $Exe @Arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$preference }
    [IO.File]::WriteAllText((Join-Path $records ($Name+'-'+$Attempt+'.log')),$log,[Text.UTF8Encoding]::new($false))
    Write-Host $log
    [ordered]@{Name=$Name;StartedUtc=$started;FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$code;Executable=$Exe;Arguments=$Arguments} |
        ConvertTo-Json -Depth 8 | Set-Content $record -Encoding UTF8
    Require ($code -eq 0) "$Name failed. Actual attempt retained; inspect before any further decision."
}
Push-Location $validation
try {
    $project=Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
    if($Phase -eq 'BaselineClose') {
        $audit=Get-Content 'artifacts/dual-version-build/audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
        Require ($audit.Passed -and $audit.MainBoundaryExecuted -and $audit.RuntimeExecuted) 'Unmodified HEAD baseline incomplete.'
        $destination=Join-Path $tool 'artifacts/p5o2b25/baseline'
        Require (-not (Test-Path $destination)) 'Baseline already captured.'
        [IO.Directory]::CreateDirectory($destination) | Out-Null
        Copy-Item 'artifacts/dual-version-build' (Join-Path $destination 'dual-version-build') -Recurse
        Copy-Item 'artifacts/p5o2b9/current' (Join-Path $destination 'current') -Recurse
        Require (@(Get-ChildItem (Join-Path $destination 'current') -Filter '*.json').Count -eq 8) 'Eight baseline files required.'
        Write-Host 'Unmodified HEAD dual/boundary and eight Current baselines captured.'
    }
    if($Phase -eq 'Build') {
        Require (Test-Path (Join-Path $tool 'artifacts/p5o2b25/baseline/dual-version-build/audit.json')) 'Capture clean baseline before overlay.'
        # The stop rule withdrew exactly these own prototype edits. Restore the
        # original HEAD bytes in the disposable export too; git-diff overlays alone
        # cannot see a file that is no longer modified in the authoritative tree.
        $restored=@('src/XMLDocNormalizer.HistoricalWorker/HistoricalAnalysis.cs','src/XMLDocNormalizer.HistoricalWorker/WorkerProtocol.cs',
            'src/XMLDocNormalizer/Checks/XmlDocExceptionSemanticDetector.ExternalFindings.cs',
            'src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerDeclaredDocumentationProjection.cs',
            'src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerPayloadProjection.cs',
            'src/XMLDocNormalizer/Execution/Analysis/HistoricalWorkerSolutionProjection.cs',
            'src/XMLDocNormalizer/Execution/ExternalTargetExecution.cs',
            'src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerResponseValidation.cs')
        $archive=(Get-Content (Join-Path $tool 'artifacts/p5o2b25/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).Archive
        $exportRoot=[IO.Path]::GetFullPath((Join-Path $validation '../..'))
        Require ($exportRoot.StartsWith((Join-Path $tool 'artifacts/'),[StringComparison]::OrdinalIgnoreCase)) 'Only the disposable export may be restored.'
        & tar -xf $archive -C $exportRoot @($restored | ForEach-Object { 'Tools/XMLDocNormalizer/'+$_ })
        Require ($LASTEXITCODE -eq 0) 'Exact HEAD export restoration failed.'
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B23Proof/Sync-Validation.ps1') -ValidationRoot $validation
        Require ($LASTEXITCODE -eq 0) 'Overlay failed.'
        # Archive timestamps may predate prototype outputs. A clean is essential:
        # source restoration plus an incremental build can retain stale binaries.
        Recorded 'current-clean' 'dotnet' @('clean',(Join-Path $validation 'src/XMLDocNormalizer/XMLDocNormalizer.csproj'),'-p:BuildProjectReferences=false','-warnaserror','-v:minimal')
        Recorded 'worker-clean' 'dotnet' @('clean',(Join-Path $validation 'src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj'),'-p:BuildProjectReferences=false','-warnaserror','-v:minimal')
        Recorded 'tests-wae' 'dotnet' @('build',$project,'--no-restore','-warnaserror')
        Recorded 'worker-wae' 'dotnet' @('build',(Join-Path $validation 'src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj'),'--no-restore','-p:BuildProjectReferences=false','-warnaserror')
    }
    if($Phase -eq 'New') {
        Recorded 'new-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests','--logger',('trx;LogFileName=b25-'+$Attempt+'.trx'),'--results-directory',(Join-Path $validation 'artifacts/p5o2b25/tests'))
    }
    if($Phase -eq 'Regressions') {
        Recorded 'pid-g1-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~HistoricalWorkerLifetimeTests','--logger','trx;LogFileName=pid-g1.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25/tests'))
        Recorded 'pid-g1-proof' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/PIDG1Proof/Test-ProcessLifetime.ps1'),'-OutputPath',(Join-Path $records 'pid-g1-proof-cases.json'))
        Recorded 'regressions' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'),'-ValidationRoot',$validation,'-AdditionalFilter','FullyQualifiedName~HistoricalWorkerLifetimeTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests')
    }
    if($Phase -eq 'Boundary') {
        $line=Get-Content 'build/Verify-DualVersionBuild.ps1' -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
        Require ($line -match "'--filter', '([^']+)'") 'Permanent boundary filter missing.'
        Recorded 'boundary' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter',$Matches[1],'--logger','trx;LogFileName=boundary.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25/tests'))
    }
    if($Phase -eq 'Dual') {
        Recorded 'dual' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $validation 'build/Verify-DualVersionBuild.ps1'),'-RunMainBoundaryTests','-BuildTestProjectReferences')
    }
} finally { Pop-Location }
