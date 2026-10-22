$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a6-r2'
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
Push-Location $tool
try {
    $output = & dotnet format whitespace . --folder --include Tests/XMLDocNormalizerTests/Worker/ExceptionFlowGenericExpansionBoundaryTests.cs --verify-no-changes --verbosity quiet 2>&1 | Out-String
    $code = $LASTEXITCODE
    [IO.File]::WriteAllText((Join-Path $records 'format-verify.log'), $output, [Text.UTF8Encoding]::new($false))
    Require ($code -eq 0) 'New witness formatting failed.'
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Format-Owned.ps1') -VerifyOnly
    Require ($LASTEXITCODE -eq 0) 'Owned UTF8/CRLF check failed.'
    $scripts = @(Get-ChildItem $PSScriptRoot -File -Filter '*.ps1') + @(Get-ChildItem (Join-Path $tool 'Evaluation/P5O2B25A7Proof') -File -Filter '*.ps1')
    foreach ($file in $scripts) {
        $errors = $null; $tokens = $null
        $null = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$errors)
        Require ($errors.Count -eq 0) ('PowerShell syntax: ' + $file.FullName)
    }
    foreach ($pattern in @('P5O2B25A6-*.json', 'P5O2B25A7-*.json')) { foreach ($file in Get-ChildItem Evaluation -File -Filter $pattern) { $null = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } }
    $tracked = @(& git --no-optional-locks -C $repo ls-files)
    $paths = $tracked + @(& git --no-optional-locks -C $repo ls-files --others --exclude-standard)
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($path in $paths) { Require ($seen.Add($path)) ('Git case collision: ' + $path) }
    $references = @(foreach ($project in @('Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj', 'src/XMLDocNormalizer/XMLDocNormalizer.csproj', 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj', 'src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj')) {
        Require ($tracked -ccontains ('Tools/XMLDocNormalizer/' + $project)) ('Untracked project: ' + $project)
        [xml]$xml = Get-Content -LiteralPath $project -Raw -Encoding UTF8
        foreach ($reference in $xml.SelectNodes('//ProjectReference')) {
            Require ($reference.Include -notmatch '\$|\*') 'Expected explicit project reference.'
            $full = [IO.Path]::GetFullPath((Join-Path (Split-Path (Join-Path $tool $project)) $reference.Include))
            Require ($full.StartsWith($repo + '\', [StringComparison]::OrdinalIgnoreCase)) 'Project outside repository.'
            $relative = $full.Substring($repo.Length + 1).Replace('\', '/')
            Require ($tracked -ccontains $relative) ('Project reference not exact-case tracked: ' + $relative)
            [ordered]@{ Project = $project; Reference = $relative }
        }
    })
    & git --no-optional-locks -C $repo diff --check
    Require ($LASTEXITCODE -eq 0) 'Inherited tracked whitespace check failed.'
    $result = [ordered]@{ Passed = $true; CSharpFormatExit = $code; Utf8NoBomCrlf = $true; PowerShellScripts = $scripts.Count; GitPaths = $paths.Count; CaseCollisions = 0; ProjectReferences = $references; ActualLinuxRun = $false }
    [IO.File]::WriteAllText((Join-Path $records 'static.json'), (($result | ConvertTo-Json -Depth 8) + "`r`n"), [Text.UTF8Encoding]::new($false))
} finally { Pop-Location }
