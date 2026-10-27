$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$raw=Join-Path $tool 'artifacts/evaluation/CoverageCompletion/closure'
$detail=Join-Path $raw 'coverage-completion-gate-detail.json'
$output=Join-Path $PSScriptRoot 'Results/closure-gate.json'
if(Test-Path $output){throw 'Preserve previous closure result; no green retries'}
$source=Get-Content (Join-Path $PSScriptRoot 'Verify-Gates.ps1') -Raw
$old='$destination=Join-Path $records ''coverage-completion-gate.json'''
$source=$source.Replace($old,('$destination=' + "'$detail'"))
if(!$source.Contains("'$detail'")){throw 'Gate output binding failed'}
$source=$source.Replace('$PSScriptRoot',("'$PSScriptRoot'"))
$script=Join-Path $raw 'Verify-Gate-Paths.ps1'
$source|Set-Content $script -Encoding UTF8
$started=[DateTime]::UtcNow.ToString('o')
$ErrorActionPreference='Continue'
& powershell -NoProfile -ExecutionPolicy Bypass -File $script *> (Join-Path $raw 'closure-verification.log')
$code=$LASTEXITCODE;$ErrorActionPreference='Stop'
if(!(Test-Path $detail)){throw 'No gate result; preserve raw context'}
$result=Get-Content $detail -Raw|ConvertFrom-Json
$failed=@($result.Checks|Where-Object{!$_.Passed})
[ordered]@{Gate='Coverage Completion closure';Passed=($code -eq 0 -and $result.Passed -and !$failed.Count);StartedUtc=$started;CompletedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$code;Checks=$result.Checks.Count;PassedChecks=@($result.Checks|Where-Object Passed).Count;FailedChecks=$failed;Reproduction='powershell -NoProfile -File Evaluation/CoverageCompletionProof/Run-ClosureVerification.ps1';NewWireDto=$result.NewWireDto;ExistingTestAssertionsChanged=$result.ExistingTestAssertionsChanged;DetailedOutputTemporary=$true}|ConvertTo-Json -Depth 8|Set-Content $output -Encoding UTF8
Get-Content (Join-Path $raw 'closure-verification.log') -Tail 8
if($code -ne 0 -or !$result.Passed){exit 1}
