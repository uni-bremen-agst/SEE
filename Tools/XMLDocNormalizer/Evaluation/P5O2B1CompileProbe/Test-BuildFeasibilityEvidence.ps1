param([string]$Root = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $Root
$files = @(Get-ChildItem -LiteralPath 'Evaluation/P5O2B1CompileProbe' -Recurse -File | ForEach-Object FullName) + @(
    (Join-Path $Root 'Evaluation/P5O2B1-historical-roslyn-build-feasibility.md'),
    (Join-Path $Root 'Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json'),
    (Join-Path $Root 'Evaluation/P5O2B1-roslyn-api-drift-matrix.md'),
    (Join-Path $Root 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
foreach ($path in $files) {
    $text = [Text.UTF8Encoding]::new($false, $true).GetString([IO.File]::ReadAllBytes($path))
    if ($text -match '(?<!\r)\n|\r(?!\n)') { throw "Non-CRLF line ending: $path" }
    if ($text -match '(?m)[ \t]+\r?$') { throw "Trailing whitespace: $path" }
    if (-not $text.EndsWith("`r`n")) { throw "Missing final newline: $path" }
    switch ([IO.Path]::GetExtension($path)) {
        '.json' { $null = $text | ConvertFrom-Json }
        '.csproj' { $null = [xml]$text }
        '.props' { $null = [xml]$text }
        '.ps1' {
            $parseTokens = $null
            $parseErrors = $null
            $null = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$parseTokens, [ref]$parseErrors)
            if ($parseErrors.Count -ne 0) { throw "PowerShell parse errors: $path" }
        }
    }
}
$formatCases = @(
    @{ Project = 'Evaluation/P5O2B1CompileProbe/MetadataAudit/MetadataAudit.csproj'; File = 'Evaluation/P5O2B1CompileProbe/MetadataAudit/Program.cs' },
    @{ Project = 'Evaluation/P5O2B1CompileProbe/HistoricalCompileProbe.csproj'; File = 'Evaluation/P5O2B1CompileProbe/CompileOnlySemanticEnvironment.cs' })
foreach ($case in $formatCases) {
    & dotnet format $case.Project --include $case.File --no-restore --verify-no-changes --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "B1-owned C# format gate failed: $($case.File)" }
}
& git diff --check
if ($LASTEXITCODE -ne 0) { throw 'git diff --check failed.' }
$audit = Get-Content -LiteralPath 'Evaluation/P5O2B1-historical-roslyn-build-feasibility-audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
if ($audit.SourceVerification.ProductiveFiles -ne 120 -or $audit.SourceVerification.CurrentMainSourceFiles.Count -ne 361 -or
    $audit.Validation.HistoricalInitial.Errors.Count -ne 4 -or $audit.Validation.HistoricalRepeat.Errors.Count -ne 4 -or
    $audit.RoslynApiDrift.Members.Count -ne 430 -or $audit.RoslynApiDrift.Types.Count -ne 210 -or
    $audit.RoslynApiDrift.EnumConstants.Count -ne 84 -or $audit.Isolation.HistoricalRuntimeLoaded) {
    throw 'B1 evidence invariant failed.'
}
$gate = [ordered]@{
    Passed = $true; Scope = 'B1-owned experimental/evidence files and the modified boundary register; no productive-source or foreign formatting changes.'
    Files = @($files | ForEach-Object { $_.Substring($Root.TrimEnd('\', '/').Length + 1).Replace('\', '/') } | Sort-Object)
    StrictUtf8 = $true; CrlfAndFinalNewline = $true; NoTrailingWhitespace = $true
    JsonXmlPowershellParse = $true; FullScopedCSharpFormat = $true; GitDiffCheck = $true; EvidenceInvariants = $true
}
$json = ($gate | ConvertTo-Json -Depth 5).Replace("`r`n", "`n").Replace("`n", "`r`n")
[IO.File]::WriteAllText((Join-Path $Root 'artifacts/p5o2b1/quality-gates.json'), $json + "`r`n", [Text.UTF8Encoding]::new($false))
Write-Output "B1 quality gates PASS: $($files.Count) files; strict UTF8/CRLF/final-newline/whitespace, JSON/XML/PowerShell parse, scoped full C# format, evidence invariants and git diff --check."
