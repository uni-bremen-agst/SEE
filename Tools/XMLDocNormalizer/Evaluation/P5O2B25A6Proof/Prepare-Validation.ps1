$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a6-r2'
if (Test-Path -LiteralPath $records) { throw 'Preserve existing A6 records; no overwrite.' }
& git --no-optional-locks -C $repo diff --cached --quiet
if ($LASTEXITCODE -ne 0) { throw 'Expected clean semantic index.' }
$a5 = Get-Content (Join-Path $tool 'artifacts/p5o2b25a5/start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
Require ((& git --no-optional-locks -C $repo rev-parse HEAD) -ceq $a5.Head) 'Unexpected HEAD movement.'
Require ((Hash (Join-Path $repo '.git/index')) -ceq $a5.IndexSha256) 'Physical index changed; do not repair.'
Require ((Hash (Join-Path $repo '.gitignore')) -ceq $a5.IgnoreSha256) 'Protected ignore changed.'
$core = @(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -Recurse -File -Force)
Require ($core.Count -eq 23) 'Expected protected Core inventory.'
foreach ($file in $a5.Core) { Require ((Hash $file.Path) -ceq $file.Sha256) ('Core changed: ' + $file.Path) }
$stashes = @(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs')
Require (($stashes -join "`n") -ceq ($a5.Stashes -join "`n")) 'Stash inventory changed.'
$paths = @(@(& git --no-optional-locks -C $repo ls-files -- Tools/XMLDocNormalizer) + @(& git --no-optional-locks -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer) | Sort-Object -Unique)
$original = @(foreach ($path in $paths) {
    $relative = $path.Substring('Tools/XMLDocNormalizer/'.Length)
    if ($relative -match '^Evaluation/P5O2B25A[67]|ExceptionFlowGenericExpansionBoundaryTests.cs$') { continue }
    [ordered]@{ Path = $relative; Sha256 = (Hash (Join-Path $repo $path)) }
})
[IO.Directory]::CreateDirectory($records) | Out-Null
$start = [ordered]@{ Head = $a5.Head; CapturedUtc = [DateTime]::UtcNow.ToString('o'); IndexSha256 = $a5.IndexSha256; IgnoreSha256 = $a5.IgnoreSha256; Core = $a5.Core; Stashes = $stashes; OriginalFiles = $original; Status = @(& git --no-optional-locks -C $repo status --short) }
[IO.File]::WriteAllText((Join-Path $records 'start.json'), (($start | ConvertTo-Json -Depth 10) + "`r`n"), [Text.UTF8Encoding]::new($false))
$destination = Join-Path $tool 'artifacts/b25a6-c777-r2'
Require (!(Test-Path -LiteralPath $destination)) 'Fresh export required.'
[IO.Directory]::CreateDirectory($destination) | Out-Null
$archive = Join-Path $records 'head.tar'
& git --no-optional-locks -C $repo archive --format=tar --output=$archive $a5.Head Tools/XMLDocNormalizer
Require ($LASTEXITCODE -eq 0) 'Archive failed.'
& tar -xf $archive -C $destination
Require ($LASTEXITCODE -eq 0) 'Extraction failed.'
$validation = Join-Path $destination 'Tools/XMLDocNormalizer'
$overlay = @(@(& git --no-optional-locks -C $repo diff --name-only HEAD -- Tools/XMLDocNormalizer) + @(& git --no-optional-locks -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer) | Sort-Object -Unique)
foreach ($path in $overlay) {
    $relative = $path.Substring('Tools/XMLDocNormalizer/'.Length)
    $target = Join-Path $validation $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::Copy((Join-Path $repo $path), $target, $true)
}
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -ValidationRoot $validation
Require ($LASTEXITCODE -eq 0) 'Offline export preparation failed.'
[IO.File]::WriteAllText((Join-Path $records 'export.json'), (([ordered]@{ Head = $a5.Head; ToolRoot = $validation; Network = $false } | ConvertTo-Json) + "`r`n"), [Text.UTF8Encoding]::new($false))
$baseline = Join-Path $records 'baseline/current'
[IO.Directory]::CreateDirectory($baseline) | Out-Null
foreach ($file in Get-ChildItem (Join-Path $tool 'artifacts/p5o2b25a5/baseline/current') -File -Filter '*.json') { [IO.File]::Copy($file.FullName, (Join-Path $baseline $file.Name), $false) }
Require (@(Get-ChildItem $baseline -File).Count -eq 8) 'Eight inherited baseline bytes required.'
Write-Host ('Prepared isolated A6 export, protected inputs verified, inherited files captured: ' + $original.Count)
