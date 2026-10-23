param([ValidatePattern('^[0-9]{2}$')][string]$Revision = '15')
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = Join-Path $tool ('artifacts/b25a10-development-' + $Revision)
$runner = Join-Path $PSScriptRoot '../Run-Recorded.ps1'
$steps = @(
    @{ Name='test-build'; Args=@('build','Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj','-p:UseSharedCompilation=false','-warnaserror','-v:minimal') },
    @{ Name='worker-build'; Args=@('build','src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj','-p:UseSharedCompilation=false','-warnaserror','-v:minimal') },
    @{ Name='fixture-build'; Args=@('build','Evaluation/P5O2B7Proof/HistoricalPdbFixture/HistoricalPdbFixture.csproj','-p:UseSharedCompilation=false','-warnaserror','-v:minimal') },
    @{ Name='fixture-emit'; Args=@('artifacts/exception-flow-historical/HistoricalPdbFixture/bin/Debug/net8.0/HistoricalPdbFixture.dll','artifacts/p5o2b7/historical-pdb') },
    @{ Name='peer-build'; Args=@('build','Evaluation/P5O2B4Proof/TestProcess/BoundaryTestProcess.csproj','-p:UseSharedCompilation=false','-warnaserror','-v:minimal') }
)
foreach ($step in $steps) {
    $quoted = @($step.Args | ForEach-Object { "'" + $_.Replace("'", "''") + "'" }) -join ','
    $command = "& '$runner' -Name 'development-$Revision-$($step.Name)' -Executable 'dotnet' -WorkingDirectory '$validation' -Arguments @($quoted)"
    & powershell -NoProfile -ExecutionPolicy Bypass -Command $command
    if ($LASTEXITCODE -ne 0) { throw ('Prerequisite failed: ' + $step.Name + '; preserve this attempt.') }
}
