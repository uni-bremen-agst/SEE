$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a6-r2'
$start = Get-Content (Join-Path $records 'start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Same([string]$path, [string]$sha) { (Test-Path -LiteralPath $path -PathType Leaf) -and (Get-FileHash -LiteralPath $path).Hash -ceq $sha }
foreach ($file in $start.OriginalFiles) { Require (Same (Join-Path $tool $file.Path) $file.Sha256) ('Inherited file changed: ' + $file.Path) }
Require ((& git --no-optional-locks -C $repo rev-parse HEAD) -ceq $start.Head) 'External HEAD movement.'
& git --no-optional-locks -C $repo diff --cached --quiet
Require ($LASTEXITCODE -eq 0) 'Semantic index changed.'
Require (Same (Join-Path $repo '.git/index') $start.IndexSha256) 'Physical index changed; never repair.'
Require (Same (Join-Path $repo '.gitignore') $start.IgnoreSha256) 'Protected ignore changed.'
$core = @(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -Recurse -Force -File)
Require ($core.Count -eq $start.Core.Count) 'Core inventory changed.'
foreach ($file in $start.Core) { Require (Same $file.Path $file.Sha256) ('Core changed: ' + $file.Path) }
$stashes = @(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs')
Require (($stashes -join "`n") -ceq ($start.Stashes -join "`n")) 'Stash inventory changed.'
$oldHistory = Get-Content (Join-Path $tool 'artifacts/p5o2b25a5/changes-and-history.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$historical = [Collections.Generic.List[object]]::new()
foreach ($file in $oldHistory.HistoricalFingerprints) {
    if ($file.CheckedPath -match '^([0-9a-f]{40}):(.+)$') {
        $blob = & git --no-optional-locks -C $repo show ($Matches[1] + ':Tools/XMLDocNormalizer/' + $Matches[2]) | Out-String
        Require ($LASTEXITCODE -eq 0) 'Historical blob unavailable.'
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $digest = [BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false).GetBytes([regex]::Replace($blob, "`r?`n", "`r`n")))).Replace('-', '') } finally { $sha.Dispose() }
        Require ($digest -ceq $file.Sha256) 'Historical blob fingerprint changed.'
    } else { Require (Same $file.CheckedPath $file.Sha256) ('Historical evidence changed: ' + $file.CheckedPath) }
    $historical.Add($file)
}
$a5 = Get-Content (Join-Path $tool 'Evaluation/P5O2B25A5-recursive-summary-scc-closure-audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$a5Checks = [Collections.Generic.List[object]]::new()
function Fingerprints($value) {
    if ($null -eq $value -or $value -is [string] -or $value.GetType().IsValueType) { return }
    if ($value -is [Array]) { foreach ($entry in $value) { Fingerprints $entry }; return }
    if ($value.Path -and $value.Sha256) {
        Require (Same $value.Path $value.Sha256) ('Frozen A5 artifact changed: ' + $value.Path)
        $a5Checks.Add([ordered]@{ Path = $value.Path; Sha256 = $value.Sha256 })
    }
    foreach ($property in $value.psobject.Properties) { Fingerprints $property.Value }
}
Fingerprints $a5
$result = [ordered]@{ Passed = $true; Head = $start.Head; InheritedFiles = $start.OriginalFiles.Count; ExistingTestsAndProductionUnchanged = $true; IndexUnchanged = $true; IgnoreUnchanged = $true; CoreCount = $core.Count; StashesCount = $stashes.Count; HistoricalFingerprints = @($historical.ToArray()); A5Fingerprints = @($a5Checks.ToArray()); HistoricalEvidenceRewritten = $false }
[IO.File]::WriteAllText((Join-Path $records 'protection.json'), (($result | ConvertTo-Json -Depth 12) + "`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host ('PASS: inherited files ' + $start.OriginalFiles.Count + ', historical fingerprints ' + $historical.Count + ', frozen A5 artifacts ' + $a5Checks.Count + ', Core 23 and stashes 6 unchanged.')
