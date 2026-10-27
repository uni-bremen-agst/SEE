$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $PSScriptRoot 'implementation-export.json') -Raw | ConvertFrom-Json).Export
$records=Join-Path $PSScriptRoot 'Results'
$dual=Get-Content (Join-Path $records 'dual.json') -Raw | ConvertFrom-Json
if ($dual.ExitCode -ne 0 -or $null -eq $dual.ExitCode) { throw 'Dual must pass first' }
function Run([string]$name,[string[]]$arguments) {
    $log=Join-Path $records ($name + '.log')
    if (Test-Path -LiteralPath $log) { throw 'Preserve prior proof attempt' }
    Push-Location $validation
    try { $ErrorActionPreference='Continue'; & dotnet @arguments > $log 2>&1; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference='Stop'; Pop-Location }
    if ($code -ne 0) { throw "Proof $name failed: $code" }
}
$inputs=@(foreach($proof in @(@{Id='a8';Stem='FamilyProof';Source='Evaluation/P5O2B25A8Proof'},@{Id='a9';Stem='ProfileProof';Source='Evaluation/P5O2B25A9Proof'},@{Id='production';Stem='Validation';Source='Evaluation/P5O2B25A10Proof/FinalClosure'})) {
    $directory=Join-Path $validation ('artifacts/p5o2b25b/proofs/' + $proof.Id)
    if (Test-Path -LiteralPath $directory) { throw 'Fresh proof directory required' }
    New-Item -ItemType Directory -Path $directory | Out-Null
    $files=if($proof.Id -eq 'production') { @('Program.cs','Validation.csproj') } else { @(($proof.Stem + '.cs'),($proof.Stem + '.csproj')) }
    $sources=@(foreach($file in $files) {
        $original=Join-Path $tool ($proof.Source + '/' + $file); $copy=Join-Path $directory $file
        Copy-Item -LiteralPath $original -Destination $copy
        [ordered]@{Original=$original;Copied=$copy;Sha256=(Get-FileHash $copy).Hash}
    })
    Run ($proof.Id + '-proof-build') @('build',(Join-Path $directory ($proof.Stem + '.csproj')),'--configfile',(Join-Path $validation 'NuGet.Config'),('-p:AnalyzerOutput=' + (Join-Path $validation 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0')),'-p:UseSharedCompilation=false','-warnaserror','-v:minimal')
    $image=Join-Path $directory ('bin/Debug/net8.0/' + $proof.Stem + '.dll'); $result=Join-Path $records ($proof.Id + '-proof.json')
    $arguments=if($proof.Id -eq 'production') { @($image,$validation,$result) } else { @($image,$result) }
    Run ($proof.Id + '-proof') $arguments
    [ordered]@{Id=$proof.Id;SourceFiles=$sources;Image=$image;ImageSha256=(Get-FileHash $image).Hash;Result=$result;ResultSha256=(Get-FileHash $result).Hash}
})
$inputs | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $records 'proof-inputs.json') -Encoding UTF8
