param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProcessLifetime.ps1')
$first=[pscustomobject]@{ProcessId=42;ExecutionId='11111111-1111-1111-1111-111111111111';HasExited=$true;ExitCode=0}
$replacement=[pscustomobject]@{ProcessId=42;ExecutionId='22222222-2222-2222-2222-222222222222';HasExited=$true;ExitCode=0}
$cases=@(
    @{Name='exited-original';Pass=$true;Run={Assert-OwnedProcessExit $first 42}},
    @{Name='same-pid-different-executions';Pass=$true;Run={Assert-OwnedProcessPair $first $replacement 42 42}},
    @{Name='live-original';Pass=$false;Run={Assert-OwnedProcessExit ([pscustomobject]@{ProcessId=42;ExecutionId=$first.ExecutionId;HasExited=$false;ExitCode=0}) 42}},
    @{Name='missing-observation';Pass=$false;Run={Assert-OwnedProcessExit $null 42}},
    @{Name='missing-id';Pass=$false;Run={Assert-OwnedProcessExit $first $null}},
    @{Name='wrong-owner';Pass=$false;Run={Assert-OwnedProcessExit $first 43}},
    @{Name='invalid-identity';Pass=$false;Run={Assert-OwnedProcessExit ([pscustomobject]@{ProcessId=42;ExecutionId='bad';HasExited=$true;ExitCode=0}) 42}},
    @{Name='missing-exit-code';Pass=$false;Run={Assert-OwnedProcessExit ([pscustomobject]@{ProcessId=42;ExecutionId=$first.ExecutionId;HasExited=$true}) 42}},
    @{Name='same-execution-not-fresh';Pass=$false;Run={Assert-OwnedProcessPair $first $first 42 42}}
)
$results=foreach($case in $cases) {
    $accepted=$true; $message=$null
    try { & $case.Run } catch { $accepted=$false; $message=$_.Exception.Message }
    if($accepted -ne $case.Pass) { throw ('Incorrect lifetime proof decision: '+$case.Name) }
    [ordered]@{Name=$case.Name;ExpectedAcceptance=$case.Pass;ActualAcceptance=$accepted;Diagnostic=$message;Passed=$true}
}
$full=[IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($full)) | Out-Null
[IO.File]::WriteAllText($full,(($results | ConvertTo-Json -Depth 6)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host 'PASS: nine deterministic serialized lifetime-proof decisions, including same PID / different execution.'
