param([Parameter(Mandatory = $true)][string]$Name, [Parameter(Mandatory = $true)][string]$Executable, [Parameter(Mandatory = $true)][string]$WorkingDirectory, [Parameter(Mandatory = $true)][string[]]$Arguments)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runs = Join-Path $tool 'artifacts/p5o2b25a10/runs'
$path = Join-Path $runs ($Name + '.json')
if (Test-Path -LiteralPath $path) { throw ('Attempt already exists: ' + $Name) }
[IO.Directory]::CreateDirectory($runs) | Out-Null
$started = [DateTime]::UtcNow.ToString('o')
[IO.File]::WriteAllText($path, ([ordered]@{ Name = $Name; StartedUtc = $started; ExitCode = $null; Arguments = $Arguments } | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
$info = [Diagnostics.ProcessStartInfo]::new()
$info.FileName = $Executable; $info.WorkingDirectory = $WorkingDirectory; $info.UseShellExecute = $false
$info.Arguments = (@($Arguments | ForEach-Object { '"' + ($_ -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"' }) -join ' ')
$info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
$process = [Diagnostics.Process]::Start($info)
try {
    $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
    $process.WaitForExit(); $code = $process.ExitCode
    [IO.File]::WriteAllText((Join-Path $runs ($Name + '.log')), ($stdout.Result + "`r`nSTDERR:`r`n" + $stderr.Result), [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($path, (([ordered]@{ Name = $Name; StartedUtc = $started; FinishedUtc = [DateTime]::UtcNow.ToString('o'); ExitCode = $code; Executable = $Executable; WorkingDirectory = $WorkingDirectory; Arguments = $Arguments } | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
    Write-Host $stdout.Result
    if ($stderr.Result) { Write-Host $stderr.Result }
    exit $code
} finally { $process.Dispose() }

