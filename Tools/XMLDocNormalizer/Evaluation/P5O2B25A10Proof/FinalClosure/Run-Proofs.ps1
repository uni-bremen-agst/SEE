param([ValidatePattern('^[0-9]{2}$')][string]$Revision = '17')
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = Join-Path $tool ('artifacts/b25a10-development-' + $Revision)
$records = Join-Path $tool ('artifacts/p5o2b25a10/final-r' + $Revision)
$runner = Join-Path $PSScriptRoot '../Run-Recorded.ps1'
function Run([string]$name, [string[]]$arguments) {
    $quoted = @($arguments | ForEach-Object { "'" + $_.Replace("'", "''") + "'" }) -join ','
    & powershell -NoProfile -ExecutionPolicy Bypass -Command "& '$runner' -Name 'development-$Revision-$name' -Executable 'dotnet' -WorkingDirectory '$validation' -Arguments @($quoted)"
    if ($LASTEXITCODE -ne 0) { throw ('STOP: ' + $name + '; preserve failure without a green retry.') }
}
foreach ($phase in @('full','boundary','dual')) {
    $record = Get-Content (Join-Path $tool ('artifacts/p5o2b25a10/runs/development-' + $Revision + '-' + $phase + '.json')) -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -eq $record.ExitCode) { throw ('Incomplete ' + $phase) }
    if ($phase -eq 'full' -and $record.ExitCode -ne 0) {
        [xml]$full = Get-Content (Join-Path $validation 'artifacts/a10-closure-results/full.trx') -Raw -Encoding UTF8
        $failed = @($full.TestRun.Results.UnitTestResult | Where-Object outcome -eq 'Failed')
        if ($failed.Count -ne 1 -or $failed[0].testName -cne 'XMLDocNormalizerTests.Execution.Semantic.ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed' -or
            $failed[0].Output.ErrorInfo.StackTrace -notmatch 'ExternalMetadataReferenceFactoryTests.cs:line 446') { throw 'Unexpected Full failure: STOP, no further proof execution.' }
        Write-Host 'Full remains FAILED at the documented unchanged foreign fixture; independent proofs do not waive it.'
    } elseif ($record.ExitCode -ne 0) { throw ('Cannot advance after failed ' + $phase) }
}
if ((Test-Path -LiteralPath $records) -and @(Get-ChildItem -LiteralPath $records -Recurse -File -Force).Count -ne 0) {
    throw 'Fresh proof/input directory required; preserve prior output.'
}
[IO.Directory]::CreateDirectory($records) | Out-Null
$inputs = @()
foreach ($proof in @(@{Id='a8';Stem='FamilyProof';Source='Evaluation/P5O2B25A8Proof'}, @{Id='a9';Stem='ProfileProof';Source='Evaluation/P5O2B25A9Proof'}, @{Id='production';Stem='Validation';Source='Evaluation/P5O2B25A10Proof/FinalClosure'})) {
    $directory = Join-Path $records $proof.Id
    [IO.Directory]::CreateDirectory($directory) | Out-Null
    $files = if ($proof.Id -eq 'production') { @('Program.cs','Validation.csproj') } else { @(($proof.Stem + '.cs'), ($proof.Stem + '.csproj')) }
    foreach ($file in $files) { [IO.File]::Copy((Join-Path $tool ($proof.Source + '/' + $file)), (Join-Path $directory $file), $false) }
    $analyzer = Join-Path $validation 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0'
    Run ($proof.Id + '-proof-build') @('build',(Join-Path $directory ($proof.Stem + '.csproj')),'--configfile',(Join-Path $validation 'NuGet.Config'),('-p:AnalyzerOutput=' + $analyzer),'-p:UseSharedCompilation=false','-warnaserror','-v:minimal')
    $image = Join-Path $directory ('bin/Debug/net8.0/' + $proof.Stem + '.dll')
    $result = Join-Path $records ($proof.Id + '-proof.json')
    $arguments = if ($proof.Id -eq 'production') { @($image,$validation,$result) } else { @($image,$result) }
    Run ($proof.Id + '-proof') $arguments
    $inputs += [ordered]@{ Id=$proof.Id; SourceFiles=@(foreach ($file in $files) {
        [ordered]@{ Original=(Join-Path $tool ($proof.Source + '/' + $file)); Copied=(Join-Path $directory $file); Sha256=(Get-FileHash (Join-Path $directory $file)).Hash }
    }); Image=$image; ImageSha256=(Get-FileHash $image).Hash; Result=$result; ResultSha256=(Get-FileHash $result).Hash }
}
[IO.File]::WriteAllText((Join-Path $records 'proof-inputs.json'),(($inputs | ConvertTo-Json -Depth 10)+"`r`n"),[Text.UTF8Encoding]::new($false))
