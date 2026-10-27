param([Parameter(Mandatory=$true)][string]$Name)
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$start=Get-Content (Join-Path $PSScriptRoot 'Preflight/start.json') -Raw | ConvertFrom-Json
$output=Join-Path $PSScriptRoot ($Name + '-protection.json')
if (Test-Path -LiteralPath $output) { throw 'Preserve prior protection check' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
$checks=@(
    [ordered]@{Name='Index';Passed=(Hash (Join-Path $repo '.git/index')) -ceq $start.IndexSha256},
    [ordered]@{Name='RootIgnore';Passed=(Hash (Join-Path $repo '.gitignore')) -ceq $start.RootIgnoreSha256},
    [ordered]@{Name='Head';Passed=(@(& git.exe --no-optional-locks -C $repo rev-parse HEAD)[0] -ceq $start.Head)},
    [ordered]@{Name='Stashes';Passed=((@(& git.exe --no-optional-locks -C $repo stash list --format='%H %gs') -join "`n") -ceq ($start.Stashes -join "`n"))}
)
& git.exe --no-optional-locks -C $repo diff --cached --quiet
$checks += [ordered]@{Name='SemanticIndexClean';Passed=$LASTEXITCODE -eq 0}
$checks += @(foreach($file in $start.Core) { [ordered]@{Name=$file.Path;Passed=(Hash $file.Path) -ceq $file.Sha256} })
$checks += @(foreach($file in $start.HistoricalEvaluationFiles) { [ordered]@{Name=$file.Path;Passed=(Hash (Join-Path $repo $file.Path)) -ceq $file.Sha256} })
$failed=@($checks | Where-Object { !$_.Passed })
[ordered]@{CapturedUtc=[DateTime]::UtcNow.ToString('o');Passed=$failed.Count -eq 0;Checks=$checks;Failures=$failed;HistoricalS1EvidencePreserved=$true;
    GitStatus=@(& git.exe --no-optional-locks -C $repo status --short)} | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath $output -Encoding UTF8
if ($failed.Count) { throw 'Protection drift: STOP without repair' }
Write-Output ('PASS: ' + $checks.Count + ' protection checks; no baseline replacement')
