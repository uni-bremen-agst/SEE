param([Parameter(Mandatory=$true)][ValidatePattern('^[0-9]{2}$')][string]$Revision)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$export = Join-Path $tool ('artifacts/b25a10-development-' + $Revision)
if (Test-Path -LiteralPath $export) { throw 'Fresh development export required; never overwrite a previous attempt.' }
$frozen = Get-Content (Join-Path $tool 'artifacts/p5o2b25a9/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$accepted = Get-Content (Join-Path $PSScriptRoot 'phase0-binding-closure-validation.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($file in $frozen.Core) {
    $expected = $file.Sha256
    $s1 = @($accepted.CoreRawFrozenHashDifferences | Where-Object { $_.Path -eq $file.Path })
    if ($s1.Count) { $expected = $s1[0].Actual }
    if ((Get-FileHash -LiteralPath $file.Path).Hash -cne $expected) { throw ('New protected drift: ' + $file.Path) }
}
if ((Get-FileHash (Join-Path $repo '.git/index')).Hash -cne 'CB38F3D3B845ABD54FEC4C2E111E77BFF7F31B80A73F438A657BAF03173F8158') { throw 'Index drift; do not repair.' }
if ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -cne '7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5') { throw 'Root ignore drift.' }
$paths = @(@(& git --no-optional-locks -C $repo ls-files -- Tools/XMLDocNormalizer) + @(& git --no-optional-locks -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer) | Sort-Object -Unique)
[IO.Directory]::CreateDirectory($export) | Out-Null
foreach ($path in $paths) {
    $source = Join-Path $repo $path
    $relative = $path.Substring('Tools/XMLDocNormalizer/'.Length)
    if ($relative -match '(^|/)(bin|obj|artifacts)/') { continue }
    $target = Join-Path $export $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::Copy($source, $target, $false)
}
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -ValidationRoot $export
if ($LASTEXITCODE -ne 0) { throw 'Offline preparation failed.' }
Write-Host ('Development export of actual WIP: ' + $export)
