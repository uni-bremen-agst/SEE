$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records=Join-Path $PSScriptRoot 'Results'
$export=(Get-Content (Join-Path $PSScriptRoot 'export.json') -Raw|ConvertFrom-Json).Export
function Json([string]$file){Get-Content -LiteralPath $file -Raw|ConvertFrom-Json}
$destination=Join-Path $records 'closure-summary.json'
if(Test-Path $destination){throw 'Preserve closure summary'}
$gate=Json (Join-Path $records 'closure-gate.json')
if(!$gate.Passed){throw 'Closure gate must pass'}
$source=Json (Join-Path $records 'source-identity.json')
$binding=Json (Join-Path $records 'FinalInputs/compiled-source-verification.json')
$source.PdbBinding.Passed=[bool]$binding.Passed;$source.PdbBinding.BoundImages=$binding.ImageBindings.Count;$source.PdbBinding.RawSourceDifferences=$binding.RawDifferences
$source|ConvertTo-Json -Depth 6|Set-Content (Join-Path $records 'source-identity.json') -Encoding UTF8
$dual=Json (Join-Path $export 'artifacts/dual-version-build/audit.json')
$testRuns=@(foreach($file in Get-ChildItem (Join-Path $export 'artifacts/dual-version-build') -Filter '*.trx' -File){
 [xml]$xml=Get-Content $file.FullName -Raw;$c=$xml.TestRun.ResultSummary.Counters
 [ordered]@{Suite=$file.BaseName;Total=[int]$c.total;Passed=[int]$c.passed;Failed=[int]$c.failed;Skipped=[int]$c.notExecuted}
})
$wae=@(foreach($name in @('0-current-build','1-historical-build','2-current-build','3-historical-build','4-historical-build','5-current-build','worker-0-build','worker-1-build','boundary-test-process-build','projection-pdb-fixture-build','main-boundary-tests-build')){
 $log=Get-Content (Join-Path $export ('artifacts/dual-version-build/'+$name+'.log')) -Raw
 [ordered]@{Step=$name;ZeroWarnings=($log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$');ZeroErrors=($log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$')}
})
$baseline=Join-Path $tool 'Evaluation/P5O2B25BProof/Results/Current-Baselines'
$baselines=@(foreach($file in Get-ChildItem $baseline -Filter '*.json' -File){[ordered]@{Name=$file.Name;ByteEqual=((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash (Join-Path $export ('artifacts/p5o2b9/current/'+$file.Name))).Hash)}})
$runs=@(foreach($name in @('targeted','targeted-prepared','targeted-final','projection-final','broad','boundary','full','dual')){
 $r=Json (Join-Path $records ($name+'.json'))
 [ordered]@{Gate=$name;StartedUtc=$r.StartedUtc;CompletedUtc=$r.CompletedUtc;ExitCode=$r.ExitCode;Counters=$r.Counters;Executable=$r.Executable;Command=$r.Arguments;Context=if($name -eq 'targeted'){'Known missing static fixture; no green retry'}elseif($name -in @('targeted-prepared','targeted-final')){'Documented intermediate revision'}else{'Final source revision'}}
})
$matrix=@(foreach($file in Get-ChildItem (Join-Path $export 'artifacts/coverage/cli-witnesses') -Filter '*.json' -File){
 $w=Json $file.FullName
 [ordered]@{Mode=$w.Mode;Scenario=$w.Scenario;ExitCode=$w.Exit;Analysis=$w.Report.Analysis;FindingCount=$w.Report.Findings.Count;FindingIds=@($w.Report.Findings|ForEach-Object SmellId|Sort-Object)}
})
$witness=Join-Path $PSScriptRoot 'witnesses';$null=New-Item -ItemType Directory -Path $witness -Force
$matrixPath=Join-Path $witness 'four-mode-cli.json'
[ordered]@{Producer='Full';ProducerSummary='Results/closure-summary.json';Reduction='Existing actual CLI reports reduced to status/IDs/counts; no reconstructed success claims';SourceDigest=$source.SourceDigest;Cases=$matrix}|ConvertTo-Json -Depth 8|Set-Content $matrixPath -Encoding UTF8
$proofs=@(foreach($id in @('a8','a9','production')){$p=Json (Join-Path $records ($id+'-proof.json'));[ordered]@{Id=$id;Checks=$p.Checks.Count;Passed=$p.Passed;Command=$p.Command;OriginalSources=$p.SourceFiles|Select-Object Original,Sha256}})
$protectionFile=Get-ChildItem $PSScriptRoot -Filter 'protection-*.json'|Sort-Object Name|Select-Object -Last 1
$protection=Json $protectionFile.FullName
$start=Json (Join-Path $PSScriptRoot 'start.json')
[ordered]@{BeforeHead=$start.Head;AfterHead=$protection.Head;Passed=$protection.Passed;IndexBefore=$start.IndexSha256;IndexAfter=(Get-FileHash (Join-Path ([IO.Path]::GetFullPath((Join-Path $tool '../..'))) '.git/index')).Hash;RootIgnoreSha256=$start.RootIgnoreSha256;IndexUnchanged=$protection.IndexUnchanged;RootIgnoreUnchanged=$protection.RootIgnoreUnchanged;StashesUnchanged=$protection.StashesUnchanged;StashIds=@($start.Stashes);HistoricalFilesChecked=$protection.HistoricalFilesChecked;HistoricalDrift=$protection.HistoricalDrift.Count;CoreFilesChecked=$protection.CoreFilesChecked;CoreDrift=$protection.CoreDrift.Count}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $records 'protection-summary.json') -Encoding UTF8
[ordered]@{Package='Coverage Completion';Passed=$true;BaseCommit=$source.BaseCommit;SourceDigest=$source.SourceDigest;Sdk=$dual.Sdk;HistoricalVersion=$dual.HistoricalVersion;CurrentRoslynVersion='5.0.0';RuntimeStatus='OPEN - recurrent, unattributed process failure';Runs=$runs;DualVersion=[ordered]@{Passed=$dual.Passed;Suites=$testRuns;Total=($testRuns|ForEach-Object Total|Measure-Object -Sum).Sum;Failed=($testRuns|ForEach-Object Failed|Measure-Object -Sum).Sum;Skipped=($testRuns|ForEach-Object Skipped|Measure-Object -Sum).Sum};Wae=$wae;CurrentBaselines=$baselines;Proofs=$proofs;Closure=$gate;Witness=[ordered]@{Path='witnesses/four-mode-cli.json';Sha256=(Get-FileHash $matrixPath).Hash;Bytes=(Get-Item $matrixPath).Length};Storage='Successful reproducible raw logs/TRX/inventories are temporary, not archived by default';NoBroadBoundaryFullRerun=$true;DualIncludesItsOwnIntegratedTests=$true}|ConvertTo-Json -Depth 12|Set-Content $destination -Encoding UTF8
'Compact closure and small actual CLI witness written; no raw output deleted'
