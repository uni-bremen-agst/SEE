param([Parameter(Mandatory=$true)][ValidateSet('Build','Isolated','Full')][string]$Phase)
$ErrorActionPreference = 'Stop'
$location = Get-Content (Join-Path $PSScriptRoot 'lifetime-validation-location-short.json') -Raw | ConvertFrom-Json
$validation = $location.ValidationRoot
$runner = Join-Path $validation 'Evaluation/P5O2B25A10Proof/Run-Recorded.ps1'
if ($Phase -eq 'Build') {
    $source = Get-Content (Join-Path $PSScriptRoot '../BroadClosure/Build-Prerequisites.ps1') -Raw
    $source = $source.Replace("`$tool = [IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../../..'))", ("`$tool = '" + $validation + "'"))
    $source = $source.Replace("`$validation = Join-Path `$tool ('artifacts/b25a10-development-' + `$Revision)", ("`$validation = '" + $validation + "'"))
    $source = $source.Replace("`$runner = Join-Path `$PSScriptRoot '../Run-Recorded.ps1'", ("`$runner = '" + $runner + "'"))
    & ([scriptblock]::Create($source)) -Revision '18'
    exit $LASTEXITCODE
}
if ($Phase -eq 'Full') {
    $isolatedRecord = Get-Content (Join-Path $validation 'artifacts/p5o2b25a10/runs/development-18-multimodule-first.json') -Raw | ConvertFrom-Json
    if ($isolatedRecord.ExitCode -ne 0 -or $null -eq $isolatedRecord.ExitCode) { throw 'Isolated test must pass first.' }
}
$arguments = @('test','Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj','--no-build','--no-restore')
$name = 'full'
if ($Phase -eq 'Isolated') {
    $name = 'multimodule-first'
    $arguments += @('--filter','FullyQualifiedName~ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed')
}
$arguments += @('--blame','--logger',('trx;LogFileName=' + $name + '.trx'),'--results-directory','artifacts/a10-closure-results','-v:minimal')
& $runner -Name ('development-18-' + $name) -Executable 'dotnet' -WorkingDirectory $validation -Arguments $arguments
exit $LASTEXITCODE
