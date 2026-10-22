$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a8'
$validation = (Get-Content (Join-Path $records 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$directory = Join-Path $records 'finite-proof-r2'
if (Test-Path -LiteralPath $directory) { throw 'Preserve proof inputs/results; no repeat.' }
[IO.Directory]::CreateDirectory($directory) | Out-Null
foreach ($name in @('FamilyProof.csproj', 'FamilyProof.cs')) { [IO.File]::Copy((Join-Path $PSScriptRoot $name), (Join-Path $directory $name), $false) }
$analyzer = Join-Path $validation 'src/XMLDocNormalizer/bin/Debug/net8.0'
& (Join-Path $PSScriptRoot 'Run-Recorded.ps1') -Name proof-build-r2 -Executable dotnet -WorkingDirectory $directory -Arguments @('build', (Join-Path $directory 'FamilyProof.csproj'), '-warnaserror', '--configfile', (Join-Path $validation 'NuGet.Config'), ("-p:AnalyzerOutput=" + $analyzer))
if ($LASTEXITCODE -ne 0) { throw 'Proof compilation failed; retain evidence, do not retry unchanged.' }
& (Join-Path $PSScriptRoot 'Run-Recorded.ps1') -Name proof -Executable dotnet -WorkingDirectory $directory -Arguments @((Join-Path $directory 'bin/Debug/net8.0/FamilyProof.dll'), (Join-Path $records 'proof.json'))
if ($LASTEXITCODE -ne 0) { throw 'Finite proof harness failed; retain evidence.' }
$inputs = [ordered]@{ Source = (Get-FileHash (Join-Path $directory 'FamilyProof.cs')).Hash; Project = (Get-FileHash (Join-Path $directory 'FamilyProof.csproj')).Hash; Inputs = @(foreach ($name in @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll', 'System.Collections.Immutable.dll', 'System.Reflection.Metadata.dll')) { $path = Join-Path $analyzer $name; [ordered]@{ Path = $path; Sha256 = (Get-FileHash $path).Hash } }); Image = (Get-FileHash (Join-Path $directory 'bin/Debug/net8.0/FamilyProof.dll')).Hash }
[IO.File]::WriteAllText((Join-Path $records 'proof-inputs.json'), (($inputs | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
