param([Parameter(Mandatory=$true)][ValidateSet('Targeted','Broad','Boundary','Dual','Regressions','Full')][string]$Phase)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $toolRoot 'artifacts/p5o2b24/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$recordRoot=Join-Path $toolRoot 'artifacts/pid-g1/runs'
[IO.Directory]::CreateDirectory($recordRoot) | Out-Null
$project=Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
$testRoot=Join-Path $validation 'artifacts/pid-g1/tests'
[IO.Directory]::CreateDirectory($testRoot) | Out-Null
function Require([bool]$Value,[string]$Message) { if(-not $Value) { throw $Message } }
function Invoke-Recorded([string]$Name,[string]$Exe,[string[]]$Arguments) {
    $record=Join-Path $recordRoot ($Name+'.json')
    Require (-not (Test-Path $record)) 'Preserve each attempt; do not overwrite or retry until green.'
    $started=[DateTime]::UtcNow.ToString('o')
    [ordered]@{Name=$Name;StartedUtc=$started;ExitCode=$null;State='Running or interrupted';Executable=$Exe;Arguments=$Arguments} |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $record -Encoding UTF8
    $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& $Exe @Arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    [IO.File]::WriteAllText((Join-Path $recordRoot ($Name+'.log')),$log,[Text.UTF8Encoding]::new($false)); Write-Host $log
    [IO.File]::WriteAllText($record,(([ordered]@{Name=$Name;StartedUtc=$started;FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$code;Executable=$Exe;Arguments=$Arguments} | ConvertTo-Json -Depth 6)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Require ($code -eq 0) "$Name failed; retained actual attempt. No automatic retry or exclusion."
}
function Require-Previous([string]$Name) {
    $path=Join-Path $recordRoot ($Name+'.json')
    Require (Test-Path $path) "Run prerequisite $Name first."
    $record=Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json
    Require ($null -ne $record.ExitCode -and $record.ExitCode -eq 0) "Prerequisite $Name did not pass."
}
Push-Location $toolRoot
try {
    if($Phase -eq 'Targeted') {
        Invoke-Recorded 'proof-helper' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Test-ProcessLifetime.ps1'),'-OutputPath',(Join-Path $recordRoot 'proof-helper-cases.json'))
        $exact='(FullyQualifiedName=XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalDeclaredReadinessTests.IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings&DisplayName~cover-exact)|(FullyQualifiedName=XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat&DisplayName~base)|(FullyQualifiedName=XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat&DisplayName~mixed)|(FullyQualifiedName=XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalReferenceIdentityTests.RealExternalMetadata_FullCanonicalIdentitySourcesUncertaintyGraphRepeatAndIsolation&DisplayName~1)'
        Invoke-Recorded 'isolated-four' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter',$exact,'--logger','trx;LogFileName=isolated-four.trx','--results-directory',$testRoot)
        $classes=@('HistoricalWorkerLifetimeTests','HistoricalWorkerClientTests','HistoricalWorkerProcessTests','ExceptionFlowAnalysisRouterTests','ExceptionFlowAnalysisDispatchTests','ExceptionFlowAnalyzerSelectionPolicyTests','ExceptionFlowProductionPipelineTests','ExceptionFlowHistoricalPayloadProjectionTests','ExceptionFlowHistoricalInputEquivalenceTests','ExceptionFlowHistoricalReferenceIdentityTests','ExceptionFlowHistoricalSourceContextTests','ExceptionFlowHistoricalScopeTests','ExceptionFlowHistoricalSolutionScopeTests','ExceptionFlowHistoricalDeclaredBoundaryTests','ExceptionFlowHistoricalDeclaredPolicyTests','ExceptionFlowHistoricalDeclaredReadinessTests','ExceptionFlowHistoricalFindingContractTests','ExceptionFlowHistoricalProductionAdmissionTests','ExceptionFlowHistoricalDocumentationAttestationTests','ExceptionFlowHistoricalDeclaredProductionFindingTests')
        $filter=($classes | ForEach-Object {'FullyQualifiedName~'+$_}) -join '|'
        Invoke-Recorded 'targeted' 'dotnet' @('test',$project,'--no-build','--no-restore','--filter',$filter,'--logger','trx;LogFileName=targeted.trx','--results-directory',$testRoot)
    }
    if($Phase -eq 'Broad') {
        Require-Previous 'targeted'
        Invoke-Recorded 'broad' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $toolRoot 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'),'-ValidationRoot',$validation,'-RepeatGroups','broad','-AdditionalFilter','FullyQualifiedName~HistoricalWorkerLifetimeTests')
    }
    if($Phase -eq 'Boundary') {
        Require-Previous 'broad'
        $line=Get-Content (Join-Path $validation 'build/Verify-DualVersionBuild.ps1') -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
        Require ($line -match "'--filter', '([^']+)'") 'Permanent boundary filter not found.'
        Invoke-Recorded 'boundary' 'dotnet' @('test',$project,'--no-build','--no-restore','-p:Configuration=Debug','--filter',$Matches[1],'--logger','trx;LogFileName=pid-g1-boundary.trx','--results-directory',$testRoot)
    }
    if($Phase -eq 'Dual') {
        Require-Previous 'boundary'
        Invoke-Recorded 'dual' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $validation 'build/Verify-DualVersionBuild.ps1'),'-RunMainBoundaryTests','-BuildTestProjectReferences')
    }
    if($Phase -eq 'Regressions') {
        Require-Previous 'dual'
        Invoke-Recorded 'regressions' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $toolRoot 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'),'-ValidationRoot',$validation,'-RepeatGroups','b24,b23,b22,b21,b20,b19,b18,b17,b16,b15,b14,b13,b12,b11,b10,b5-b9,productive,focused','-AdditionalFilter','FullyQualifiedName~HistoricalWorkerLifetimeTests')
    }
    if($Phase -eq 'Full') {
        Require-Previous 'regressions'
        Invoke-Recorded 'full' 'powershell' @('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $toolRoot 'Evaluation/P5O2B24Proof/Run-Regressions.ps1'),'-ValidationRoot',$validation,'-RepeatGroups','full')
    }
} finally { Pop-Location }
