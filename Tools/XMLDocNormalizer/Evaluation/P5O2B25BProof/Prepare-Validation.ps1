param([string]$Destination = (Join-Path $env:TEMP ('b25b-' + [Guid]::NewGuid().ToString('N').Substring(0,8))))
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'implementation-export.json')) { throw 'Preserve existing export evidence; create a separately named validation revision instead' }
$toolRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$start = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Preflight/start.json') -Raw | ConvertFrom-Json
foreach ($file in $start.Core) {
    if ((Get-FileHash -LiteralPath $file.Path).Hash -ne $file.Sha256) { throw "Protected Core drift: $($file.Path)" }
}
if ((Get-FileHash -LiteralPath (Join-Path $repo '.git/index')).Hash -ne $start.IndexSha256) { throw 'Index drift' }
if ((Get-FileHash -LiteralPath (Join-Path $repo '.gitignore')).Hash -ne $start.RootIgnoreSha256) { throw 'Ignore drift' }
if (Test-Path -LiteralPath $Destination) { throw 'Destination must be fresh' }
New-Item -ItemType Directory -Path $Destination | Out-Null
$paths = @(& git.exe --no-optional-locks -C $toolRoot ls-files -c -o --exclude-standard) | Sort-Object -Unique
$copied = @(foreach ($path in $paths) {
    if ($path -match '(^|/)(bin|obj|artifacts)(/|$)' -or $path.StartsWith('../')) { continue }
    $source = Join-Path $toolRoot $path
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue }
    $target = Join-Path $Destination $path
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $target
    [ordered]@{ Path = $path; Sha256 = (Get-FileHash -LiteralPath $source).Hash }
})
# Restore uses only the already installed packages; no version or package-source edits in the workspace.
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration><packageSources><clear/><add key="local" value="C:\Users\Krause\.nuget\packages"/></packageSources><packageSourceMapping><clear/></packageSourceMapping></configuration>
'@ | Set-Content -LiteralPath (Join-Path $Destination 'NuGet.Config') -Encoding UTF8
$record = [ordered]@{ Export = $Destination; Head = (& git.exe --no-optional-locks -C $toolRoot rev-parse HEAD); Sources = $copied; CapturedUtc = [DateTime]::UtcNow.ToString('o') }
$record | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'implementation-export.json') -Encoding UTF8
Write-Output $Destination
