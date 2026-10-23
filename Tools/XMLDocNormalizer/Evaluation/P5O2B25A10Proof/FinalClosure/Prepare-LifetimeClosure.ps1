param([ValidateSet('Initial','ShortPath')][string]$Attempt = 'Initial')
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$name = if ($Attempt -eq 'ShortPath') { 'a10-' + [Guid]::NewGuid().ToString('N').Substring(0,8) } else { 'XMLDocNormalizer-A10-Lifetime-' + [Guid]::NewGuid().ToString('N') }
$validation = Join-Path ([IO.Path]::GetTempPath()) $name
$protection = Get-Content (Join-Path $tool 'Evaluation/P5O2B25A10Proof/implementation-broad-root-cause-state.json') -Raw | ConvertFrom-Json
foreach ($file in $protection.Core) {
    if ((Get-FileHash $file.Path).Hash -cne $file.Actual) { throw ('Protected Core drift: ' + $file.Path) }
}
if ((Get-FileHash (Join-Path $repo '.git/index')).Hash -cne $protection.IndexSha256) { throw 'Index drift.' }
if ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -cne $protection.RootIgnoreSha256) { throw 'Root ignore drift.' }
$manifestName = if ($Attempt -eq 'ShortPath') { 'lifetime-validation-location-short.json' } else { 'lifetime-validation-location.json' }
$manifest = Join-Path $PSScriptRoot $manifestName
if (Test-Path $manifest) { throw 'Preserve existing preparation; do not overwrite.' }
[IO.Directory]::CreateDirectory($validation) | Out-Null
[IO.File]::WriteAllText($manifest, (([ordered]@{ ValidationRoot=$validation; PreparationUtc=[DateTime]::UtcNow.ToString('o'); Reason='D: export18 preparation failed before any build/test: insufficient disk space; preserve incomplete export18 and all historical evidence.' } | ConvertTo-Json) + "`r`n"), [Text.UTF8Encoding]::new($false))
$paths = @(@(& git --no-optional-locks -C $repo ls-files -- Tools/XMLDocNormalizer) + @(& git --no-optional-locks -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer) | Sort-Object -Unique)
foreach ($path in $paths) {
    $relative = $path.Substring('Tools/XMLDocNormalizer/'.Length)
    if ($relative -match '(^|/)(bin|obj|artifacts)/') { continue }
    $target = Join-Path $validation $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::Copy((Join-Path $repo $path), $target, $false)
}
# Run unchanged offline preparation with only its output-root guard rebound to
# this exact fresh temporary export. The real workspace/configuration is never written.
$offline = Get-Content (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -Raw
$offline = $offline.Replace("`$toolRoot=[IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../..'))", ("`$toolRoot='" + $tool + "'"))
$offline = $offline.Replace("if(-not `$validation.StartsWith((Join-Path `$toolRoot 'artifacts/'),[StringComparison]::OrdinalIgnoreCase))", ("if(`$validation -cne '" + $validation + "')"))
& ([scriptblock]::Create($offline)) -ValidationRoot $validation
if ($LASTEXITCODE -ge 8) { throw 'Offline preparation failed.' }
Write-Host ('Fresh temporary validation export: ' + $validation)
