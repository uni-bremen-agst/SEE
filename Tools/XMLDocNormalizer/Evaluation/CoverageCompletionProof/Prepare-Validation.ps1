param([string]$HistoricalCache='')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export=Join-Path $tool ('artifacts/coverage-completion-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
if(Test-Path (Join-Path $PSScriptRoot 'export.json')){throw 'Preserve existing export'}
$null=New-Item -ItemType Directory -Path $export
$paths=@(& git --no-optional-locks -C $tool ls-files -c -o --exclude-standard)|Sort-Object -Unique
$copied=@(foreach($path in $paths){
 if($path.StartsWith('../') -or $path -match '(^|/)(bin|obj|artifacts)/'){continue}
 $source=Join-Path $tool $path
 if(!(Test-Path -LiteralPath $source -PathType Leaf)){continue}
 if($path -match '^Evaluation/' -and $path -match '\.(gz|log|trx)$'){continue}
 $target=Join-Path $export $path
 $null=New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target))
 Copy-Item -LiteralPath $source -Destination $target
 [ordered]@{Path=$path;Sha256=(Get-FileHash -LiteralPath $source).Hash}
})
if($HistoricalCache){$cache=[IO.Path]::GetFullPath($HistoricalCache)}else{
 $prior=Get-Content (Join-Path $tool 'Evaluation/P5O2B25BProof/implementation-export.json') -Raw|ConvertFrom-Json
 $cache=Join-Path $prior.Export 'artifacts/exception-flow-historical/packages'
}
if(!(Test-Path $cache)){throw 'Pinned existing Historical cache missing'}
$offline=Get-Content (Join-Path $tool 'Evaluation/P5O2B24Proof/Prepare-OfflineValidation.ps1') -Raw
$offline=$offline.Replace("`$toolRoot=[IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../..'))", "`$toolRoot='$tool'")
$offline=$offline.Replace('$cache=[IO.Path]::GetFullPath((Join-Path $toolRoot $HistoricalCache))', '$cache=$HistoricalCache')
& ([scriptblock]::Create($offline)) -ValidationRoot $export -HistoricalCache $cache
if($LASTEXITCODE -ge 8){throw 'Offline export preparation failed'}
[ordered]@{Head=(& git --no-optional-locks -C $tool rev-parse HEAD);Export=$export;Sources=$copied;HistoricalCache=$cache;OfflineOnly=$true;CapturedUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $PSScriptRoot 'export.json') -Encoding UTF8
Write-Output $export
