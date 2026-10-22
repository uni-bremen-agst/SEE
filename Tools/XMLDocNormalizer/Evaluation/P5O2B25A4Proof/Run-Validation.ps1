param([Parameter(Mandatory=$true)][ValidateSet('Baseline','Model','RefinedModel','Regressions','Boundary','Dual')][string]$Phase)
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $tool 'artifacts/p5o2b25a4/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$records=Join-Path $tool 'artifacts/p5o2b25a4/runs'
[IO.Directory]::CreateDirectory($records) | Out-Null
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function Recorded([string]$Name,[string]$Exe,[string[]]$Arguments) {
    $record=Join-Path $records ($Name+'.json')
    Require (-not (Test-Path $record)) ('Attempt already exists: '+$Name+'; no automatic retry.')
    $started=[DateTime]::UtcNow.ToString('o')
    [IO.File]::WriteAllText($record,([ordered]@{Name=$Name;StartedUtc=$started;ExitCode=$null;State='Running or interrupted';Arguments=$Arguments} | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    $info=[Diagnostics.ProcessStartInfo]::new()
    $info.FileName=$Exe; $info.WorkingDirectory=$validation; $info.UseShellExecute=$false
    $info.Arguments=(@($Arguments | ForEach-Object { '"'+($_ -replace '(\\*)"','$1$1\"' -replace '(\\+)$','$1$1')+'"' }) -join ' ')
    $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
    $process=[Diagnostics.Process]::Start($info)
    try {
        $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
        $process.WaitForExit(); $code=$process.ExitCode
        $log=$stdout.Result+"`r`nSTDERR (retained separately):`r`n"+$stderr.Result
        [IO.File]::WriteAllText((Join-Path $records ($Name+'.log')),$log,[Text.UTF8Encoding]::new($false))
        Write-Host $log
        [IO.File]::WriteAllText($record,(([ordered]@{Name=$Name;StartedUtc=$started;FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$code;Executable=$Exe;Arguments=$Arguments;Stderr=$stderr.Result} | ConvertTo-Json -Depth 8)+"`r`n"),[Text.UTF8Encoding]::new($false))
    } finally { $process.Dispose() }
    Require ($code -eq 0) ($Name+' failed; preserve actual attempt, no automatic retry.')
}
Push-Location $validation
try {
    $project=Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
    if($Phase -eq 'Baseline') {
        Recorded 'baseline-build' 'dotnet' @('build',$project,'-warnaserror')
        Recorded 'baseline-current' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowProductionCurrentParityTests','--logger','trx;LogFileName=baseline-current.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a4/tests'))
        $destination=Join-Path $tool 'artifacts/p5o2b25a4/baseline'
        Require (-not (Test-Path $destination)) 'Baseline already captured.'
        [IO.Directory]::CreateDirectory($destination) | Out-Null
        Copy-Item 'artifacts/p5o2b9/current' (Join-Path $destination 'current') -Recurse
        $images=@(foreach($path in @('src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll','src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll')) {
            [ordered]@{Path=$path;Sha256=(Get-FileHash $path).Hash}
        })
        [IO.File]::WriteAllText((Join-Path $destination 'state.json'),(([ordered]@{Passed=$true;Images=$images;NoA4TestYet=$true;NoProductionChange=$true} | ConvertTo-Json -Depth 6)+"`r`n"),[Text.UTF8Encoding]::new($false))
    }
    if($Phase -in @('Model','RefinedModel')) {
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B23Proof/Sync-Validation.ps1') -ValidationRoot $validation
        Require ($LASTEXITCODE -eq 0) 'Overlay failed.'
        $name=if($Phase -eq 'Model') { 'model' } else { 'refined-model' }
        Recorded ($name+'-build') 'dotnet' @('build',$project,'--no-restore','-warnaserror')
        Recorded $name 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowBoundedGenericContextTests','--logger',('trx;LogFileName='+$name+'.trx'),'--results-directory',(Join-Path $validation 'artifacts/p5o2b25a4/tests'))
    }
    if($Phase -eq 'Regressions') {
        Recorded 'contexts' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~ExceptionFlowBoundedGenericContextTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests','--logger','trx;LogFileName=contexts.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a4/tests'))
        Recorded 'pid-g1' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter','FullyQualifiedName~HistoricalWorkerLifetimeTests','--logger','trx;LogFileName=pid-g1.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a4/tests'))
        Recorded 'pid-g1-proof' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/PIDG1Proof/Test-ProcessLifetime.ps1'),'-OutputPath',(Join-Path $records 'pid-g1-proof-cases.json'))
        Recorded 'regressions' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $tool 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'),'-ValidationRoot',$validation,'-AdditionalFilter','FullyQualifiedName~HistoricalWorkerLifetimeTests|FullyQualifiedName~ExceptionFlowHistoricalSolutionFindingTests|FullyQualifiedName~ExceptionFlowAssemblyQualifiedOwnershipTests|FullyQualifiedName~ExceptionFlowCanonicalExceptionIdentityTests|FullyQualifiedName~ExceptionFlowConstructedInvocationContextTests|FullyQualifiedName~ExceptionFlowBoundedGenericContextTests')
    }
    if($Phase -eq 'Boundary') {
        $line=Get-Content 'build/Verify-DualVersionBuild.ps1' -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
        Require ($line -match "'--filter', '([^']+)'") 'Permanent boundary filter missing.'
        Recorded 'boundary' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter',$Matches[1],'--logger','trx;LogFileName=boundary.trx','--results-directory',(Join-Path $validation 'artifacts/p5o2b25a4/tests'))
    }
    if($Phase -eq 'Dual') { Recorded 'dual' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $validation 'build/Verify-DualVersionBuild.ps1'),'-RunMainBoundaryTests','-BuildTestProjectReferences') }
} finally { Pop-Location }
