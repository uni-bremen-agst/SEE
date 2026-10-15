# Serialized test/proof diagnostics, not B21 analysis authority. Never re-query a saved PID.
function Assert-OwnedProcessExit($Observation, $ExpectedProcessId) {
    if($null -eq $Observation -or $null -eq $ExpectedProcessId -or $ExpectedProcessId -le 0 -or
        $Observation.ProcessId -ne $ExpectedProcessId -or $Observation.HasExited -ne $true -or
        $null -eq $Observation.ExitCode) { throw 'Missing/invalid original owned-process exit evidence.' }
    $execution=[Guid]::Empty
    if(-not [Guid]::TryParse([string]$Observation.ExecutionId,[ref]$execution) -or $execution -eq [Guid]::Empty) {
        throw 'Missing/invalid original execution identity.'
    }
}
function Assert-OwnedProcessPair($First,$Second,$FirstId,$SecondId) {
    Assert-OwnedProcessExit $First $FirstId
    Assert-OwnedProcessExit $Second $SecondId
    if($First.ExecutionId -eq $Second.ExecutionId) { throw 'Repeated execution reused the same owned observation.' }
}
