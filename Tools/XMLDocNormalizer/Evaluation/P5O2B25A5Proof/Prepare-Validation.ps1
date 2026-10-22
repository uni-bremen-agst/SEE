$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a5'
if (Test-Path $records) { throw 'Preserve previous A5 capture; no overwrite.' }
& git --no-optional-locks -C $repo diff --cached --quiet
if ($LASTEXITCODE -ne 0) { throw 'Expected clean semantic index.' }
[IO.Directory]::CreateDirectory($records) | Out-Null
$head = & git --no-optional-locks -C $repo rev-parse HEAD
$tracked = @(& git --no-optional-locks -C $repo ls-files -- Tools/XMLDocNormalizer)
$untracked = @(& git --no-optional-locks -C $repo ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
$files = @(foreach ($path in @($tracked + $untracked | Sort-Object -Unique)) {
    $relative = $path.Substring('Tools/XMLDocNormalizer/'.Length)
    if ($relative -match 'P5O2B25A5|ExceptionFlowRecursiveSummaryClosureTests.cs$') { continue }
    [ordered]@{ Path = $relative; Sha256 = (Get-FileHash -LiteralPath (Join-Path $repo $path)).Hash }
})
$capture = [ordered]@{
    Head = $head; CapturedUtc = [DateTime]::UtcNow.ToString('o')
    Status = @(& git --no-optional-locks -C $repo status --short)
    IndexSha256 = (Get-FileHash -LiteralPath (Join-Path $repo '.git/index')).Hash
    IgnoreSha256 = (Get-FileHash -LiteralPath (Join-Path $repo '.gitignore')).Hash
    Stashes = @(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs')
    Core = @(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -Force -File -Recurse | Sort-Object FullName | ForEach-Object { [ordered]@{ Path = $_.FullName; Sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash } })
    OriginalFiles = $files
}
[IO.File]::WriteAllText((Join-Path $records 'start.json'), (($capture | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
foreach ($path in @(@(& git --no-optional-locks -C $repo diff --name-only HEAD -- Tools/XMLDocNormalizer) + $untracked | Sort-Object -Unique)) {
    $relative = $path.Substring('Tools/XMLDocNormalizer/'.Length)
    if ($relative -match 'P5O2B25A5|ExceptionFlowRecursiveSummaryClosureTests.cs$') { continue }
    $target = Join-Path $records ('inherited/' + $relative)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    [IO.File]::Copy((Join-Path $repo $path), $target, $false)
}
$destination = Join-Path $tool 'artifacts/b25a5-c777'
if (Test-Path $destination) { throw 'Fresh validation export required.' }
[IO.Directory]::CreateDirectory($destination) | Out-Null
$archive = Join-Path $records 'head.tar'
& git --no-optional-locks -C $repo archive --format=tar --output=$archive $head Tools/XMLDocNormalizer
if ($LASTEXITCODE -ne 0) { throw 'Archive failed.' }
& tar -xf (Join-Path $records 'head.tar') -C $destination
if ($LASTEXITCODE -ne 0) { throw 'Extraction failed.' }
$validation = Join-Path $destination 'Tools/XMLDocNormalizer'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B23Proof/Sync-Validation.ps1') -ValidationRoot $validation
if ($LASTEXITCODE -ne 0) { throw 'Inherited overlay failed.' }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -ValidationRoot $validation
if ($LASTEXITCODE -ne 0) { throw 'Offline preparation failed.' }
[IO.File]::WriteAllText((Join-Path $records 'export.json'), (([ordered]@{ Head = $head; ToolRoot = $validation; Network = $false } | ConvertTo-Json) + "`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host ('Captured ' + $files.Count + ' inherited files; disposable export: ' + $validation)
