$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$output=Join-Path $tool 'artifacts/p5o2b25a4/static-verification.json'
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
Push-Location $tool
try {
    $preference=$ErrorActionPreference
    try {
        $ErrorActionPreference='Continue'
        $log=& dotnet format whitespace . --folder --include Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs --verify-no-changes --verbosity quiet 2>&1 | Out-String
        $code=$LASTEXITCODE
    } finally { $ErrorActionPreference=$preference }
    [IO.File]::WriteAllText((Join-Path $tool 'artifacts/p5o2b25a4/format-verify.log'),$log,[Text.UTF8Encoding]::new($false))
    Require ($code -eq 0) 'New A4 C# whitespace format verification failed.'
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A4Proof/Format-Owned.ps1 -VerifyOnly
    Require ($LASTEXITCODE -eq 0) 'A4 UTF8/no-BOM/CRLF failed.'
    $scripts=@(Get-ChildItem Evaluation/P5O2B25A4Proof -Filter '*.ps1')+@(Get-Item build/Verify-DualVersionBuild.ps1)
    foreach($file in $scripts) {
        $errors=$null; $tokens=$null
        $null=[Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$errors)
        Require ($errors.Count -eq 0) ('PowerShell parser: '+$file.FullName)
    }
    $tracked=@(& git --no-optional-locks -C $repo ls-files)
    $all=$tracked+@(& git --no-optional-locks -C $repo ls-files --others --exclude-standard)
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach($path in $all) { Require ($seen.Add($path)) ('Case-colliding Git path: '+$path) }
    $refs=foreach($project in @('Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj','src/XMLDocNormalizer/XMLDocNormalizer.csproj','src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj','src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj')) {
        Require ($tracked -ccontains ('Tools/XMLDocNormalizer/'+$project)) ('Project not tracked with exact case: '+$project)
        [xml]$xml=Get-Content $project -Raw -Encoding UTF8
        foreach($reference in $xml.SelectNodes('//ProjectReference')) {
            Require ($reference.Include -notmatch '\$|\*') 'Explicit project reference expected.'
            $full=[IO.Path]::GetFullPath((Join-Path (Split-Path (Join-Path $tool $project)) $reference.Include))
            Require ($full.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)) 'Reference outside repository.'
            $relative=$full.Substring($repo.Length+1).Replace('\','/')
            Require ($tracked -ccontains $relative) ('Referenced project not tracked with exact case: '+$relative)
            [ordered]@{Project=$project;Reference=$relative;ExactCaseTracked=$true}
        }
    }
    & git --no-optional-locks diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    [ordered]@{Package='P5O2B25A4';Passed=$true;GeneratedUtc=[DateTime]::UtcNow.ToString('o');DotnetWhitespaceExitCode=$code;Utf8NoBomCrlf=$true;PowerShellScripts=$scripts.Count;GitPathCount=$all.Count;CaseCollisions=0;ExplicitProjectReferences=$refs;ActualLinuxExecution=$false} |
        ConvertTo-Json -Depth 8 | ForEach-Object { [IO.File]::WriteAllText($output,($_+"`r`n"),[Text.UTF8Encoding]::new($false)) }
    Write-Host 'PASS: C# format, UTF8/CRLF, PowerShell syntax, Git whitespace/case and exact tracked project references (not a Linux execution).'
} finally { Pop-Location }
