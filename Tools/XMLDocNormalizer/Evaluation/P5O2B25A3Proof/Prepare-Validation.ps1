$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records=Join-Path $tool 'artifacts/p5o2b25a3'
if(Test-Path $records) { throw 'Preserve the first actual A3 capture/export; never overwrite it.' }
[IO.Directory]::CreateDirectory($records) | Out-Null
& git --no-optional-locks -C $repo diff --cached --quiet
if($LASTEXITCODE -ne 0) { throw 'Semantic index is not clean.' }
$head=& git --no-optional-locks -C $repo rev-parse HEAD
$files=foreach($path in @(& git --no-optional-locks -C $repo ls-files -- Tools/XMLDocNormalizer)) {
    $relative=$path.Substring('Tools/XMLDocNormalizer/'.Length)
    [ordered]@{Path=$relative;Sha256=(Get-FileHash -LiteralPath (Join-Path $repo $path)).Hash}
}
$capture=[ordered]@{Head=$head;CapturedUtc=[DateTime]::UtcNow.ToString('o');Status=@(& git --no-optional-locks -C $repo status --short);
    SemanticIndexClean=$true;IndexSha256=(Get-FileHash (Join-Path $repo '.git/index')).Hash;IgnoreSha256=(Get-FileHash (Join-Path $repo '.gitignore')).Hash;
    Stashes=@(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs');
    Core=@(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -File -Recurse | Sort-Object FullName | ForEach-Object { [ordered]@{Path=$_.FullName;Sha256=(Get-FileHash $_.FullName).Hash} });
    OriginalFiles=@($files)}
[IO.File]::WriteAllText((Join-Path $records 'start.json'),(($capture | ConvertTo-Json -Depth 8)+"`r`n"),[Text.UTF8Encoding]::new($false))
$destination=Join-Path $tool 'artifacts/b25a3-c777'
if(Test-Path $destination) { throw 'Fresh export required.' }
[IO.Directory]::CreateDirectory($destination) | Out-Null
$archive=Join-Path $records 'head.tar'
& git --no-optional-locks -C $repo archive --format=tar --output=$archive $head Tools/XMLDocNormalizer
if($LASTEXITCODE -ne 0) { throw 'Archive failed.' }
& tar -xf $archive -C $destination
if($LASTEXITCODE -ne 0) { throw 'Extraction failed.' }
$validation=Join-Path $destination 'Tools/XMLDocNormalizer'
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -ValidationRoot $validation
if($LASTEXITCODE -ne 0) { throw 'Offline preparation failed.' }
[ordered]@{Head=$head;ToolRoot=$validation;Archive=$archive;Network=$false;Baseline='Exact committed c777 HEAD; protected root ignore not overlaid'} |
    ConvertTo-Json | ForEach-Object { [IO.File]::WriteAllText((Join-Path $records 'export.json'),($_+"`r`n"),[Text.UTF8Encoding]::new($false)) }
Write-Host ('Captured '+$files.Count+' original tracked files, '+$capture.Core.Count+' Core files and '+$capture.Stashes.Count+' stashes; fresh offline export '+$validation)
