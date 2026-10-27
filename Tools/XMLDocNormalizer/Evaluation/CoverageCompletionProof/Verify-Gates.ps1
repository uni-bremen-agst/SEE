$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'));$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$export=(Get-Content (Join-Path $PSScriptRoot 'export.json') -Raw|ConvertFrom-Json).Export
$records=Join-Path $PSScriptRoot 'Results';$destination=Join-Path $records 'coverage-completion-gate.json'
if(Test-Path $destination){throw 'Preserve gate result'}
$checks=[Collections.Generic.List[object]]::new()
function Check([string]$name,[bool]$passed){$checks.Add([ordered]@{Name=$name;Passed=$passed})}
function Json([string]$path){Get-Content -LiteralPath $path -Raw|ConvertFrom-Json}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash}
try{
 foreach($name in @('projection-final','broad','boundary','full','dual')){
  $run=Json (Join-Path $records ($name+'.json'));Check ('runtime:'+ $name) ($null -ne $run.ExitCode -and $run.ExitCode -eq 0)
  if($name -ne 'dual'){Check ('no-skips:'+ $name) ($run.Counters.failed -eq '0' -and $run.Counters.notExecuted -eq '0' -and [int]$run.Counters.total -gt 0)}
 }
 [xml]$full=Get-Content (Join-Path $records 'full.trx') -Raw;$results=@($full.TestRun.Results.UnitTestResult)
 foreach($entry in @(@('CoverageCompletionTests',92),@('ExceptionFlowGenericExpansionBoundaryTests',8),@('ExceptionFlowRecursiveSummaryClosureTests',33),@('ExceptionFlowFiniteOrbitAdmissionTests',19),@('ExceptionFlowHistoricalSolutionFindingTests',27),@('ExceptionFlowHistoricalBoundedFindingTests',42),@('ExceptionFlowHistoricalSolutionAdoptionTests',20))){
  $members=@($results|Where-Object testName -like ('*.'+$entry[0]+'.*'))
  Check ('full-class:'+ $entry[0]) ($members.Count -eq $entry[1] -and @($members|Where-Object outcome -ne 'Passed').Count -eq 0)
 }
 $binding=Json (Join-Path $records 'FinalInputs/compiled-source-verification.json')
 Check 'actual-Current-Historical-Test-PDB-bindings' ($binding.Passed -and $binding.ImageBindings.Count -eq 3 -and $binding.RawDifferences -eq 0)
 $inputs=Json (Join-Path $records 'FinalInputs/validation-inputs.json')
 foreach($file in @($inputs.Sources)+@($inputs.MainSources)+@($inputs.HistoricalSources)){
  Check ('final-input:'+ $file.Path) ((Hash (Join-Path $tool $file.Path)) -ceq $file.WorkspaceSha256 -and (Hash (Join-Path $export $file.Path)) -ceq $file.ExportSha256)
 }
 foreach($image in $inputs.Images){Check ('final-image:'+ $image.Path) ((Hash (Join-Path $export $image.Path)) -ceq $image.Sha256)}
 $start=Json (Join-Path $PSScriptRoot 'start.json')
 foreach($file in $start.Sources|Where-Object Path -match '/Tests/'){Check ('existing-test-unchanged:'+ $file.Path) ((Hash (Join-Path $repo $file.Path)) -ceq $file.Sha256)}
 foreach($file in $inputs.SharedSources){
  $baseline=@($start.Sources|Where-Object Path -ceq ('Tools/XMLDocNormalizer/'+$file.Path))
  Check ('shared125-unchanged:'+ $file.Path) ($baseline.Count -eq 1 -and $baseline[0].Sha256 -ceq $file.WorkspaceSha256)
 }
 Check 'Shared125-count' ($inputs.SharedSources.Count -eq 125)
 foreach($file in $start.Sources|Where-Object{$_.Path -match 'XMLDocNormalizer\.HistoricalWorker/|ExceptionFlow\.Historical/|HistoricalWorkerResponseValidation\.cs$|HistoricalWorkerClient\.cs$|HistoricalSolutionFindingOperation\.cs$|HistoricalWorkerDeclaredDocumentationProjection\.cs$|ExceptionFlowAnalyzerSelection|HistoricalSources\.props'}){
  Check ('wire-routing-trust-unchanged:'+ $file.Path) ((Hash (Join-Path $repo $file.Path)) -ceq $file.Sha256)
 }
 $dual=Json (Join-Path $export 'artifacts/dual-version-build/audit.json');Check 'original-Dual-Version-gate' ([bool]$dual.Passed)
 $dualTotal=0
 foreach($file in Get-ChildItem (Join-Path $export 'artifacts/dual-version-build') -Filter '*.trx' -File){
  [xml]$trx=Get-Content $file.FullName -Raw;$counter=$trx.TestRun.ResultSummary.Counters;$dualTotal+=[int]$counter.total
  Check ('Dual-no-skips:'+ $file.Name) ([int]$counter.total -gt 0 -and $counter.failed -eq '0' -and $counter.notExecuted -eq '0')
 }
 Check 'Dual-all-800-tests' ($dualTotal -eq 800)
 foreach($name in @('0-current-build','1-historical-build','2-current-build','3-historical-build','4-historical-build','5-current-build','worker-0-build','worker-1-build','boundary-test-process-build','projection-pdb-fixture-build','main-boundary-tests-build')){
  $log=Get-Content (Join-Path $export ('artifacts/dual-version-build/'+$name+'.log')) -Raw
  Check ('WAE:'+ $name) ($log -match '(?m)^\s+0 (Warnung\(en\)|Warning\(s\))\s*$' -and $log -match '(?m)^\s+0 (Fehler|Error\(s\))\s*$')
 }
 $baseline=Join-Path $tool 'Evaluation/P5O2B25BProof/Results/Current-Baselines';$files=@(Get-ChildItem $baseline -Filter '*.json' -File)
 Check 'eight-Current-baselines' ($files.Count -eq 8)
 foreach($file in $files){Check ('Current-byte-equal:'+ $file.Name) ((Hash $file.FullName) -ceq (Hash (Join-Path $export ('artifacts/p5o2b9/current/'+$file.Name))))}
 $a9=Json (Join-Path $records 'a9-proof.json');Check 'original-A9-profile-proof' ($a9.Checks.Count -eq 37 -and @($a9.Checks|Where-Object{!$_.Passed}).Count -eq 0 -and $a9.Profile -ceq 'A9-FiniteOrbit-v1' -and $a9.NoProductDependency -and !$a9.ProductAnalyzerExecuted -and !$a9.TerminationProofByFiniteEnumeration)
 Check 'A9-no-unbounded-expansion' (!$a9.Growth.Exact -and $a9.Growth.ConcreteContextsMaterialized -eq 0 -and !$a9.HardPattern.Exact -and $a9.HardPattern.ConcreteContextsMaterialized -eq 0 -and $a9.Reset.Exact)
 $a10=Json (Join-Path $records 'production-proof.json');Check 'actual-A10-Session-entry-proof' ($a10.Passed -and $a10.Checks.Count -eq 30 -and @($a10.Checks|Where-Object{!$_.Passed}).Count -eq 0 -and $a10.Entry -ceq 'ExceptionFlowSummaryAnalysisSession.Analyze' -and !$a10.RawGraphsUsed -and !$a10.NewProductionFacade -and !$a10.ExistingTestMethodsExecuted -and !$a10.SubmittedSourceExecuted)
 $a7=Get-Content (Join-Path $tool 'Evaluation/P5O2B25A7-finite-parametric-generic-context-contract.md') -Raw
 Check 'A7-contract-consistent' ($a7.Contains('Decision: **STOP - no production implementation GO**.'))
 $a8=Json (Join-Path $records 'a8-proof.json');Check 'A8-original-mathematical-proof' ($a8.Checks.Count -eq 40 -and @($a8.Checks|Where-Object{!$_.Passed}).Count -eq 0 -and !$a8.ProductAnalyzerExecuted -and !$a8.GeneralUndecidabilityProvedByFiniteHarness -and !$a8.InfiniteRuntimeAnalysisExecuted)
 foreach($proof in Json (Join-Path $records 'proof-inputs.json')){
  foreach($file in $proof.SourceFiles){Check ('original-proof-source:'+ $file.Original) ((Hash $file.Original) -ceq $file.Sha256 -and (Hash $file.Copied) -ceq $file.Sha256)}
 }
 $protection=Get-ChildItem $PSScriptRoot -Filter 'protection-*.json'|Sort-Object Name|Select-Object -Last 1
 Check 'current-workspace-protection' ([bool](Json $protection.FullName).Passed)
 $witness=Join-Path $export 'artifacts/coverage/cli-witnesses';$cli=@(Get-ChildItem $witness -Filter '*.json' -File)
 Check 'sixteen-actual-four-mode-CLI-witnesses' ($cli.Count -eq 16)
 foreach($file in $cli){$row=Json $file.FullName;Check ('CLI:'+ $file.Name) ($row.Report.Analysis.Execution -ceq 'Succeeded' -and $row.Report.Analysis.Completion -ceq $(if($row.Scenario -in @('missing','diagnostic')){'Incomplete'}else{'Complete'}))}
}catch{$checks.Add([ordered]@{Name='gate-evaluation';Passed=$false;Error=$_.Exception.Message})}
$passed=@($checks|Where-Object{!$_.Passed}).Count -eq 0
[ordered]@{Package='Analysis Coverage / Completion';Passed=$passed;CapturedUtc=[DateTime]::UtcNow.ToString('o');Checks=$checks;NewWireDto=$false;ExistingTestAssertionsChanged=0;RuntimeStatus='OPEN - recurrent, unattributed process failure';Scope='Fresh final product gates, original A9/A10 proof programs, bounded B25B witnesses; historical checkpoint-only gates remain immutable'}|ConvertTo-Json -Depth 9|Set-Content $destination -Encoding UTF8
Write-Output ('Coverage gate: '+$checks.Count+' checks; Passed='+$passed)
if(!$passed){$checks|Where-Object{!$_.Passed}|ConvertTo-Json -Depth 5;exit 1}
