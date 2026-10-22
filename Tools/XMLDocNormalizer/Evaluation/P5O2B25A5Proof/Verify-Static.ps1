$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
function Require([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
Push-Location $tool
try {
    $files = @('src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs', 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs', 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs')
    $log = & dotnet format whitespace . --folder --include @files --verify-no-changes --verbosity quiet 2>&1 | Out-String
    $code = $LASTEXITCODE
    [IO.File]::WriteAllText((Join-Path $tool 'artifacts/p5o2b25a5/format-verify.log'), $log, [Text.UTF8Encoding]::new($false))
    Require ($code -eq 0) 'A5 C# formatting verification failed.'
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B25A5Proof/Format-Owned.ps1 -VerifyOnly
    Require ($LASTEXITCODE -eq 0) 'A5 UTF8/no-BOM/CRLF verification failed.'
    $scripts = @(Get-ChildItem Evaluation/P5O2B25A5Proof -Filter '*.ps1') + @(Get-Item build/Verify-DualVersionBuild.ps1)
    foreach ($file in $scripts) {
        $errors = $null; $tokens = $null
        $null = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$errors)
        Require ($errors.Count -eq 0) ('PowerShell parse failed: ' + $file.FullName)
    }
    foreach ($file in @(Get-ChildItem Evaluation -Filter 'P5O2B25A5-*.json' -File)) {
        $null = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    $tracked = @(& git --no-optional-locks -C $repo ls-files)
    $paths = $tracked + @(& git --no-optional-locks -C $repo ls-files --others --exclude-standard)
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $paths) { Require ($seen.Add($path)) ('Case-colliding Git path: ' + $path) }
    $references = @(foreach ($project in @('Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj', 'src/XMLDocNormalizer/XMLDocNormalizer.csproj', 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj', 'src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj')) {
        Require ($tracked -ccontains ('Tools/XMLDocNormalizer/' + $project)) ('Project not exact-case tracked: ' + $project)
        [xml]$xml = Get-Content -LiteralPath $project -Raw -Encoding UTF8
        foreach ($reference in $xml.SelectNodes('//ProjectReference')) {
            Require ($reference.Include -notmatch '\$|\*') 'Expected explicit ProjectReference.'
            $full = [IO.Path]::GetFullPath((Join-Path (Split-Path (Join-Path $tool $project)) $reference.Include))
            Require ($full.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase)) 'Project reference escapes repository.'
            $relative = $full.Substring($repo.Length + 1).Replace('\', '/')
            Require ($tracked -ccontains $relative) ('Project reference is not exact-case tracked: ' + $relative)
            [ordered]@{ Project = $project; Reference = $relative }
        }
    })
    & git --no-optional-locks diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace check failed.'
    [IO.File]::WriteAllText((Join-Path $tool 'artifacts/p5o2b25a5/static-verification.json'), (([ordered]@{
        Package = 'P5O2B25A5'; Passed = $true; GeneratedUtc = [DateTime]::UtcNow.ToString('o'); CSharpWhitespaceExit = $code
        Utf8NoBomCrlf = $true; PowerShellScripts = $scripts.Count; GitPaths = $paths.Count; CaseCollisions = 0; ExplicitProjectReferences = $references; ActualLinuxExecution = $false
    } | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
    Write-Host 'PASS: format/JSON/UTF8/CRLF/PowerShell/Git case/tracked projects; not an actual Linux run.'
} finally { Pop-Location }
