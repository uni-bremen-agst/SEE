param([Parameter(Mandatory=$true)][ValidateSet('BaselineClose','Model','Build','New','Regressions','Boundary','Dual')][string]$Phase)
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $tool 'artifacts/p5o2b25a3/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$records=Join-Path $tool 'artifacts/p5o2b25a3/runs'
[IO.Directory]::CreateDirectory($records) | Out-Null
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Recorded([string]$Name,[string]$Exe,[string[]]$Arguments) {
    $record=Join-Path $records ($Name+'.json')
    Require (-not (Test-Path $record)) 'Preserve actual attempts; never overwrite or automatically retry.'
    $started=[DateTime]::UtcNow.ToString('o')
    [ordered]@{Name=$Name;StartedUtc=$started;ExitCode=$null;State='Running or interrupted';Arguments=$Arguments} | ConvertTo-Json -Depth 8 | Set-Content $record -Encoding UTF8
    $info=[Diagnostics.ProcessStartInfo]::new()
    $info.FileName=$Exe; $info.WorkingDirectory=$validation; $info.UseShellExecute=$false
    # PowerShell 5 has no ArgumentList. All validation arguments are explicit;
    # quote complete arguments for the Windows command-line parser.
    $info.Arguments=(@($Arguments | ForEach-Object { '"'+($_ -replace '(\\*)"','$1$1\"' -replace '(\\+)$','$1$1')+'"' }) -join ' ')
    $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
    $process=[Diagnostics.Process]::Start($info)
    try {
        $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
        $process.WaitForExit(); $code=$process.ExitCode
        $log=$stdout.Result+"`r`nSTDERR (retained separately):`r`n"+$stderr.Result
        [IO.File]::WriteAllText((Join-Path $records ($Name+'.log')),$log,[Text.UTF8Encoding]::new($false))
        Write-Host $log
        [ordered]@{Name=$Name;StartedUtc=$started;FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$code;Executable=$Exe;Arguments=$Arguments;Stderr=$stderr.Result} | ConvertTo-Json -Depth 8 | Set-Content $record -Encoding UTF8
    } finally { $process.Dispose() }
    Require ($code -eq 0) "$Name failed; retain and inspect the actual attempt, no automatic retry."
}
Push-Location $validation
try {
    $project=Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
    if($Phase -eq 'BaselineClose') {
        $audit=Get-Content 'artifacts/dual-version-build/audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
        # A failed baseline is evidence, never a reason to replay until green.
        # Preserve it and capture the already built unchanged images before any overlay.
        $destination=Join-Path $tool 'artifacts/p5o2b25a3/baseline'
        Require (-not (Test-Path $destination)) 'Baseline already captured.'
        [IO.Directory]::CreateDirectory($destination) | Out-Null
        Copy-Item 'artifacts/dual-version-build' (Join-Path $destination 'dual-version-build') -Recurse
        Copy-Item 'artifacts/p5o2b9/current' (Join-Path $destination 'current') -Recurse
        Require (@(Get-ChildItem (Join-Path $destination 'current') -Filter '*.json').Count -eq 8) 'Eight baseline files required.'
        $images=foreach($entry in @(@('current','src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll'),
            @('historical','artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll'),
            @('worker-host','artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll'))) {
            [ordered]@{Owner=$entry[0];Path=$entry[1];Sha256=(Get-FileHash (Join-Path $validation $entry[1])).Hash}
        }
        $evaluated=(& dotnet msbuild src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj -getItem:Compile -nologo | Out-String) | ConvertFrom-Json
        Require ($LASTEXITCODE -eq 0) 'Could not read the exact baseline shared inventory.'
        $sourceRoot=Join-Path $validation 'src/XMLDocNormalizer/'
        $shared=@($evaluated.Items.Compile | Where-Object { $_.FullPath.StartsWith($sourceRoot,[StringComparison]::OrdinalIgnoreCase) } | Sort-Object FullPath | ForEach-Object {
            [ordered]@{Path=$_.FullPath.Substring($validation.Length+1).Replace('\','/');Sha256=(Get-FileHash $_.FullPath).Hash}
        })
        Require ($shared.Count -eq 121) 'Expected exactly 121 shared baseline sources.'
        [xml]$bounded=Get-Content 'artifacts/dual-version-build/bounded-findings.trx' -Raw -Encoding UTF8
        $errors=@($bounded.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]') | ForEach-Object { [ordered]@{Test=$_.testName;Message=$_.Output.ErrorInfo.Message;Stack=$_.Output.ErrorInfo.StackTrace;Duration=$_.duration} })
        [ordered]@{Passed=[bool]$audit.Passed;ExitCode=$(if($audit.Passed){0}else{1});CapturedUtc=[DateTime]::UtcNow.ToString('o');Images=$images;SharedSourceFingerprints=$shared;Failures=$errors;NoOverlayYet=$true;NoRetry=$true} |
            ConvertTo-Json -Depth 12 | Set-Content (Join-Path $destination 'state.json') -Encoding UTF8
    }
    if($Phase -in @('Model','Build')) {
        Require (Test-Path (Join-Path $tool 'artifacts/p5o2b25a3/baseline/state.json')) 'Capture the actual baseline before overlay.'
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
        if($Phase -eq 'Model') { Recorded 'model-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests','--logger','trx;LogFileName=model.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a3/tests')) }
    }
    if($Phase -eq 'New') {
        Recorded 'new-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests','--logger','trx;LogFileName=contexts.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a3/tests'))
    }
    if($Phase -eq 'Regressions') {
        Recorded 'pid-g1-tests' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~HistoricalWorkerLifetimeTests','--logger','trx;LogFileName=pid-g1.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a3/tests'))
        Recorded 'pid-g1-proof' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/PIDG1Proof/Test-ProcessLifetime.ps1'),'-OutputPath',(Join-Path $records 'pid-g1-proof-cases.json'))
        Recorded 'regressions' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'),'-ValidationRoot',$validation,'-AdditionalFilter','FullyQualifiedName~HistoricalWorkerLifetimeTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests')
    }
    if($Phase -eq 'Boundary') {
        $line=Get-Content 'build/Verify-DualVersionBuild.ps1' -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
        Require ($line -match "'--filter', '([^']+)'") 'Permanent boundary filter missing.'
        Recorded 'boundary' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter',$Matches[1],'--logger','trx;LogFileName=boundary.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a3/tests'))
    }
    if($Phase -eq 'Dual') { Recorded 'dual' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $validation 'build/Verify-DualVersionBuild.ps1'),'-RunMainBoundaryTests','-BuildTestProjectReferences') }
} finally { Pop-Location }
