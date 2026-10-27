param([ValidateSet('Start','Final')][string]$Phase='Start')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
function GitRead([string[]]$arguments){$out=@(& git.exe --no-optional-locks -C $repo @arguments);if($LASTEXITCODE -ne 0){throw 'Read-only Git failed'};$out}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash}
$head=@(GitRead @('rev-parse','HEAD'))[0]
$index=Hash (Join-Path $repo '.git/index');$ignore=Hash (Join-Path $repo '.gitignore')
$stashes=@(GitRead @('stash','list','--format=%H %gs'))
if(@(GitRead @('diff','--cached','--name-only')).Count){throw 'Unexpected staged changes'}
$startPath=Join-Path $PSScriptRoot 'start.json'
if($Phase -eq 'Start'){
    if(Test-Path -LiteralPath $startPath){throw 'Preserve initial review audit'}
    $historical=@(foreach($path in GitRead @('ls-files','--','Tools/XMLDocNormalizer/Evaluation')){[pscustomobject]@{Path=$path;Sha256=(Hash (Join-Path $repo $path))}})
    $sources=@(foreach($path in GitRead @('ls-files','--','Tools/XMLDocNormalizer/src','Tools/XMLDocNormalizer/Tests','Tools/XMLDocNormalizer/build','Tools/XMLDocNormalizer/README.md')){
        if($path -notmatch '(^|/)(bin|obj|artifacts)/'){[pscustomobject]@{Path=$path;Sha256=(Hash (Join-Path $repo $path))}}
    })
    $old=Get-Content (Join-Path $tool 'Evaluation/P5O2B25BProof/EvidenceClosure/start.json') -Raw|ConvertFrom-Json
    $core=@(foreach($file in $old.Core){[pscustomobject]@{Path=$file.Path;Sha256=(Hash $file.Path);PriorClosureEntrySha256=$file.ClosureEntrySha256}})
    $flow=Join-Path $tool 'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow'
    $groups=@(foreach($pattern in @('ExceptionFlowAnalyzer.*.cs','ExceptionFlowSummaryAnalysisSession*.cs','ExceptionFlowFiniteOrbit*.cs','ExceptionFlowSummaryGraphEvaluator.cs')){
        $files=@(Get-ChildItem -LiteralPath $flow -File -Filter $pattern)
        [pscustomobject]@{Pattern=$pattern;Files=$files.Count;PhysicalLines=(@($files|ForEach-Object{[IO.File]::ReadAllLines($_.FullName).Length})|Measure-Object -Sum).Sum;
            NonblankLines=(@($files|ForEach-Object{[IO.File]::ReadAllLines($_.FullName)|Where-Object{! [string]::IsNullOrWhiteSpace($_)}})|Measure-Object).Count;
            Largest=@($files|ForEach-Object{[pscustomobject]@{Path=$_.FullName.Substring($tool.Length+1).Replace('\','/');Lines=[IO.File]::ReadAllLines($_.FullName).Length}}|Sort-Object Lines -Descending|Select-Object -First 5)}
    })
    [ordered]@{CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=$head;Parent=@(GitRead @('rev-parse','HEAD^'))[0];Branch=@(GitRead @('branch','--show-current'))[0];
        Upstream=@(GitRead @('rev-parse','--abbrev-ref','@{upstream}'))[0];CachedUpstreamHead=@(GitRead @('rev-parse','@{upstream}'))[0];
        IndexSha256=$index;RootIgnoreSha256=$ignore;Stashes=$stashes;InitialGitStatus=@(GitRead @('status','--short'));Sources=$sources;HistoricalEvaluationFiles=$historical;Core=$core;
        StaticMetrics=$groups;ProductionEdits=0;TestEdits=0;ProductGateExecutions=0;Benchmarks=0;CrashReproductionAttempts=0} |
        ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $startPath -Encoding UTF8
    Write-Output ('Review start: '+$sources.Count+' source/build/docs; '+$historical.Count+' historical files; '+$core.Count+' physical Core files')
    exit 0
}
$start=Get-Content -LiteralPath $startPath -Raw|ConvertFrom-Json
$sourceDrift=@($start.Sources|Where-Object{(Hash (Join-Path $repo $_.Path)) -cne $_.Sha256})
$historyDrift=@($start.HistoricalEvaluationFiles|Where-Object{(Hash (Join-Path $repo $_.Path)) -cne $_.Sha256})
$coreDrift=@($start.Core|Where-Object{(Hash $_.Path) -cne $_.Sha256})
$passed=$head -ceq $start.Head -and $index -ceq $start.IndexSha256 -and $ignore -ceq $start.RootIgnoreSha256 -and
    ($stashes -join "`n") -ceq ($start.Stashes -join "`n") -and !$sourceDrift.Count -and !$historyDrift.Count -and !$coreDrift.Count
$destination=Join-Path $PSScriptRoot 'verification.json'
if(Test-Path -LiteralPath $destination){throw 'Preserve review verification'}
[ordered]@{Passed=$passed;CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=$head;IndexUnchanged=$index -ceq $start.IndexSha256;RootIgnoreUnchanged=$ignore -ceq $start.RootIgnoreSha256;
    StashesUnchanged=($stashes -join "`n") -ceq ($start.Stashes -join "`n");SourceFilesChecked=$start.Sources.Count;HistoricalFilesChecked=$start.HistoricalEvaluationFiles.Count;CoreFilesChecked=$start.Core.Count;
    SourceDrift=$sourceDrift;HistoricalDrift=$historyDrift;CoreDrift=$coreDrift;ProductGateExecutions=0;Benchmarks=0;CrashReproductionAttempts=0;GitStatus=@(GitRead @('status','--short'))} |
    ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $destination -Encoding UTF8
if(!$passed){throw 'Review protection drift; no repair attempted'}
Write-Output 'PASS: only new review files; source/test/history/Core/index/ignore/stashes unchanged'
