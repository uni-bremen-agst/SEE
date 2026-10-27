param([ValidateSet('Start','Final')][string]$Phase='Start')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
function GitRead([string[]]$argsList){$value=@(& git --no-optional-locks -C $repo @argsList);if($LASTEXITCODE){throw 'Git audit failed'};$value}
function Hash([string]$file){(Get-FileHash -LiteralPath $file).Hash}
$startPath=Join-Path $PSScriptRoot 'start.json'
$index=Hash (Join-Path $repo '.git/index');$ignore=Hash (Join-Path $repo '.gitignore')
$head=@(GitRead @('rev-parse','HEAD'))[0];$stashes=@(GitRead @('stash','list','--format=%H %gs'))
if(@(GitRead @('diff','--cached','--name-only')).Count){throw 'Nonempty semantic index'}
if($Phase -eq 'Start'){
 if(Test-Path -LiteralPath $startPath){throw 'Start exists; preserve it'}
 $prior=Get-Content (Join-Path $tool 'Evaluation/P5O2B25BPostClosureReview/start.json') -Raw|ConvertFrom-Json
 $historical=@(foreach($path in GitRead @('ls-files','--','Tools/XMLDocNormalizer/Evaluation')){[pscustomobject]@{Path=$path;Sha256=(Hash (Join-Path $repo $path))}})
 $sources=@(foreach($path in GitRead @('ls-files','--','Tools/XMLDocNormalizer/src','Tools/XMLDocNormalizer/Tests','Tools/XMLDocNormalizer/build','Tools/XMLDocNormalizer/README.md')){if($path -notmatch '(^|/)(bin|obj|artifacts)/'){[pscustomobject]@{Path=$path;Sha256=(Hash (Join-Path $repo $path))}}})
 $core=@(foreach($file in $prior.Core){[pscustomobject]@{Path=$file.Path;Sha256=(Hash $file.Path)}})
 [ordered]@{Head=$head;CapturedUtc=[DateTime]::UtcNow.ToString('o');IndexSha256=$index;RootIgnoreSha256=$ignore;Stashes=$stashes;Historical=$historical;Sources=$sources;Core=$core;GitStatus=@(GitRead @('status','--short'))} | ConvertTo-Json -Depth 8|Set-Content -LiteralPath $startPath -Encoding UTF8
 'Protection start captured';exit 0
}
$start=Get-Content -LiteralPath $startPath -Raw|ConvertFrom-Json
$history=@($start.Historical|Where-Object{(Hash (Join-Path $repo $_.Path)) -cne $_.Sha256})
$coreDrift=@($start.Core|Where-Object{(Hash $_.Path) -cne $_.Sha256})
$changed=@($start.Sources|Where-Object{(Hash (Join-Path $repo $_.Path)) -cne $_.Sha256}|ForEach-Object{[pscustomobject]@{Path=$_.Path;Before=$_.Sha256;After=(Hash (Join-Path $repo $_.Path))}})
$passed=$head -ceq $start.Head -and $index -ceq $start.IndexSha256 -and $ignore -ceq $start.RootIgnoreSha256 -and ($stashes -join "`n") -ceq ($start.Stashes -join "`n") -and !$history.Count -and !$coreDrift.Count
$destination=Join-Path $PSScriptRoot ('protection-'+[DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'.json')
[ordered]@{Passed=$passed;Head=$head;IndexUnchanged=$index -ceq $start.IndexSha256;RootIgnoreUnchanged=$ignore -ceq $start.RootIgnoreSha256;StashesUnchanged=($stashes -join "`n") -ceq ($start.Stashes -join "`n");HistoricalFilesChecked=$start.Historical.Count;HistoricalDrift=$history;CoreFilesChecked=$start.Core.Count;CoreDrift=$coreDrift;SourceChanges=$changed;GitStatus=@(GitRead @('status','--short'))}|ConvertTo-Json -Depth 9|Set-Content -LiteralPath $destination -Encoding UTF8
if(!$passed){throw 'Protection drift; no repair attempted'}
'PASS: history/Core/index/ignore/stashes preserved'
