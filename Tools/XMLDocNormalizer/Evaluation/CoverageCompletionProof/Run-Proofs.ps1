$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export=(Get-Content (Join-Path $PSScriptRoot 'export.json') -Raw|ConvertFrom-Json).Export
$records=Join-Path $PSScriptRoot 'Results'
$rawRoot=Join-Path $tool 'artifacts/evaluation/CoverageCompletion/closure/proofs';$null=New-Item -ItemType Directory -Path $rawRoot -Force
$dual=Get-Content (Join-Path $records 'dual.json') -Raw|ConvertFrom-Json
if($dual.ExitCode -ne 0 -or $null -eq $dual.ExitCode){throw 'Dual must pass first'}
function Run([string]$name,[string[]]$arguments){
 $log=Join-Path $rawRoot ($name+'.log');if(Test-Path $log){throw 'Preserve proof attempt'}
 Push-Location $export
 try{$ErrorActionPreference='Continue';& dotnet @arguments *> $log;$code=$LASTEXITCODE}
 finally{$ErrorActionPreference='Stop';Pop-Location}
 if($code){throw ('Proof failed: '+$name+' / '+$code)}
}
$inputs=@(foreach($proof in @(@{Id='a8';Stem='FamilyProof';Source='Evaluation/P5O2B25A8Proof'},@{Id='a9';Stem='ProfileProof';Source='Evaluation/P5O2B25A9Proof'},@{Id='production';Stem='Validation';Source='Evaluation/P5O2B25A10Proof/FinalClosure'})){
 $directory=Join-Path $export ('artifacts/coverage/proofs/'+$proof.Id)
 if(Test-Path $directory){throw 'Fresh proof directory required'}
 $null=New-Item -ItemType Directory -Path $directory
 $files=if($proof.Id -eq 'production'){@('Program.cs','Validation.csproj')}else{@(($proof.Stem+'.cs'),($proof.Stem+'.csproj'))}
 $sources=@(foreach($file in $files){$original=Join-Path $tool ($proof.Source+'/'+$file);$copy=Join-Path $directory $file;Copy-Item -LiteralPath $original -Destination $copy;[ordered]@{Original=$original;Copied=$copy;Sha256=(Get-FileHash $copy).Hash}})
 Run ($proof.Id+'-proof-build') @('build',(Join-Path $directory ($proof.Stem+'.csproj')),'--configfile',(Join-Path $export 'NuGet.Config'),('-p:AnalyzerOutput='+(Join-Path $export 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0')),'-p:UseSharedCompilation=false','-warnaserror','-v:minimal')
 $image=Join-Path $directory ('bin/Debug/net8.0/'+$proof.Stem+'.dll');$result=Join-Path $rawRoot ($proof.Id+'-proof-detail.json')
 $arguments=if($proof.Id -eq 'production'){@($image,$export,$result)}else{@($image,$result)}
 Run ($proof.Id+'-proof') $arguments
 $detail=Get-Content $result -Raw|ConvertFrom-Json
 $summary=[ordered]@{Id=$proof.Id;Passed=(@($detail.Checks|Where-Object{!$_.Passed}).Count -eq 0 -and $detail.Checks.Count -gt 0);Checks=@($detail.Checks|Select-Object Name,Passed);ProductAnalyzerExecuted=$detail.ProductAnalyzerExecuted;SourceFiles=$sources;Command=$arguments;RawOutputTemporary=$true}
 if($detail.PSObject.Properties.Name -contains 'Passed'){$summary.Passed=$summary.Passed -and [bool]$detail.Passed}
 foreach($key in @('ContractOnly','ContractProofOnly','NoProductDependency','Profile','TerminationProofByFiniteEnumeration','GeneralUndecidabilityProvedByFiniteHarness','InfiniteRuntimeAnalysisExecuted','Entry','RawGraphsUsed','ExistingTestMethodsExecuted','SubmittedSourceExecuted','NewProductionFacade')){if($detail.PSObject.Properties.Name -contains $key){$summary[$key]=$detail.$key}}
 foreach($key in @('Growth','HardPattern','Reset')){if($detail.PSObject.Properties.Name -contains $key){$summary[$key]=$detail.$key|Select-Object Exact,ConcreteContextsMaterialized}}
 $compact=Join-Path $records ($proof.Id+'-proof.json');if(Test-Path $compact){throw 'Preserve compact proof result'}
 $summary|ConvertTo-Json -Depth 8|Set-Content $compact -Encoding UTF8
 [ordered]@{Id=$proof.Id;SourceFiles=$sources;Image=$image;ImageSha256=(Get-FileHash $image).Hash;Result=$compact;ResultSha256=(Get-FileHash $compact).Hash}
})
$inputs|ConvertTo-Json -Depth 8|Set-Content (Join-Path $records 'proof-inputs.json') -Encoding UTF8
'Original A8/A9 mathematical and A10 production-entry proofs executed against the final export'
