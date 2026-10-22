param([Parameter(Mandatory = $true)][ValidateSet('A8', 'A9')][string]$Harness)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a9'
$validation = (Get-Content (Join-Path $records 'export.json') -Raw | ConvertFrom-Json).ToolRoot
$directoryName = if ($Harness -eq 'A8') { 'finite-proof-r2' } else { 'contract-proof' }
$directory = Join-Path $records $directoryName
if (Test-Path -LiteralPath $directory) { throw 'Preserve proof attempts; no overwrite/retry.' }
[IO.Directory]::CreateDirectory($directory) | Out-Null
$stem = if ($Harness -eq 'A8') { 'FamilyProof' } else { 'ProfileProof' }
$sourceRoot = if ($Harness -eq 'A8') { Join-Path $tool 'Evaluation/P5O2B25A8Proof' } else { $PSScriptRoot }
foreach ($extension in @('.cs', '.csproj')) { [IO.File]::Copy((Join-Path $sourceRoot ($stem + $extension)), (Join-Path $directory ($stem + $extension)), $false) }
$buildName = if ($Harness -eq 'A8') { 'proof-build-r2' } else { 'a9-proof-build' }
$runName = if ($Harness -eq 'A8') { 'proof' } else { 'a9-proof' }
$resultName = if ($Harness -eq 'A8') { 'proof.json' } else { 'profile-proof.json' }
$analyzer = Join-Path $validation 'src/XMLDocNormalizer/bin/Debug/net8.0'
$buildArguments = @('build', (Join-Path $directory ($stem + '.csproj')), '-warnaserror', '--configfile', (Join-Path $validation 'NuGet.Config'))
if ($Harness -eq 'A8') { $buildArguments += '-p:AnalyzerOutput=' + $analyzer }
& (Join-Path $PSScriptRoot 'Run-Recorded.ps1') -Name $buildName -Executable dotnet -WorkingDirectory $directory -Arguments $buildArguments
if ($LASTEXITCODE -ne 0) { throw 'Retain actual proof compilation failure; no unchanged retry.' }
& (Join-Path $PSScriptRoot 'Run-Recorded.ps1') -Name $runName -Executable dotnet -WorkingDirectory $directory -Arguments @((Join-Path $directory ('bin/Debug/net8.0/' + $stem + '.dll')), (Join-Path $records $resultName))
if ($LASTEXITCODE -ne 0) { throw 'Retain actual proof failure; no unchanged retry.' }
$inputs = [ordered]@{ Source = (Get-FileHash (Join-Path $directory ($stem + '.cs'))).Hash; Project = (Get-FileHash (Join-Path $directory ($stem + '.csproj'))).Hash; Image = (Get-FileHash (Join-Path $directory ('bin/Debug/net8.0/' + $stem + '.dll'))).Hash }
if ($Harness -eq 'A8') {
    $inputs.Inputs = @(foreach ($name in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll', 'System.Collections.Immutable.dll', 'System.Reflection.Metadata.dll')) { $path = Join-Path $analyzer $name; [ordered]@{ Path = $path; Sha256 = (Get-FileHash $path).Hash } })
} else { $inputs.NoProductOrRoslynReference = $true }
$name = if ($Harness -eq 'A8') { 'proof-inputs.json' } else { 'profile-proof-inputs.json' }
[IO.File]::WriteAllText((Join-Path $records $name), (($inputs | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
