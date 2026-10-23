$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$worker = Join-Path $tool 'artifacts/b25a10-development-16/artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll'
$destination = Join-Path $PSScriptRoot 'await-details-diagnostic.json'
if (Test-Path -LiteralPath $destination) { throw 'Preserve the existing diagnostic.' }
$request = [ordered]@{ protocolVersion=2; operation='analyze'; payload=[ordered]@{
    source='public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }'
    typeMetadataName='Fixture'; methodName='Root'
} }
$start = [Diagnostics.ProcessStartInfo]::new()
$start.FileName = 'dotnet'; $start.Arguments = '"' + $worker + '"'
$start.UseShellExecute = $false; $start.CreateNoWindow = $true
$start.RedirectStandardInput = $true; $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
$process = [Diagnostics.Process]::Start($start)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
    $process.StandardInput.Write(($request | ConvertTo-Json -Depth 8 -Compress)); $process.StandardInput.Close()
    if (!$process.WaitForExit(30000)) { $process.Kill(); $process.WaitForExit(); throw 'Owned diagnostic worker exceeded30s.' }
    $record = [ordered]@{
        Kind='One genuine Worker diagnostic capture; no test/assertion retry and no success result'
        Request=$request; WorkerSha256=(Get-FileHash -LiteralPath $worker).Hash
        ProcessId=$process.Id; ExitCode=$process.ExitCode; Stdout=$stdout.Result; Stderr=$stderr.Result
        Response=($stdout.Result | ConvertFrom-Json); ExistingTestsExecuted=$false; ProductionOrTestChanges=$false
    }
    [IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 30)+"`r`n"),[Text.UTF8Encoding]::new($false))
    $record.Response.failure | ConvertTo-Json -Depth 8
} finally { $process.Dispose() }
