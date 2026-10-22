$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$destination = Join-Path $tool 'artifacts/p5o2b25a7/inventory.json'
if (Test-Path -LiteralPath $destination) { throw 'Preserve the original A7 source search capture.' }
$pattern = 'OriginalDefinition|constructed|TypeArguments|TypeParameters|ConstructedFrom|canonical.?context|summary.?context|summary.?key|same.?key|same.?target|different.?constructions|generic.?recurs|List<T>|recursive.?substitution|containing.?type|nested.?generic'
Push-Location $tool
try {
    $matches = @(& rg -n -i $pattern Tests --glob '*.cs' --glob '!**/bin/**' --glob '!**/obj/**')
    if ($LASTEXITCODE -ne 0) { throw 'Repository-wide characterization search failed.' }
    $paths = @(& rg -l -i $pattern Tests --glob '*.cs' --glob '!**/bin/**' --glob '!**/obj/**')
    if ($LASTEXITCODE -ne 0) { throw 'Characterization file inventory failed.' }
    # A5's explicit finite SCC tests must be considered even without a literal
    # matching the generic search. Their complete assertions are preserved.
    $paths += 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs'
    $files = @(foreach ($path in @($paths | Sort-Object -Unique)) { [ordered]@{ Path = $path.Replace('\', '/'); Sha256 = (Get-FileHash -LiteralPath $path).Hash } })
    $result = [ordered]@{ Head = (& git --no-optional-locks rev-parse HEAD); CapturedUtc = [DateTime]::UtcNow.ToString('o'); Pattern = $pattern; SearchRoot = 'Tests'; Globs = @('*.cs', '!**/bin/**', '!**/obj/**'); LiteralMatches = $matches; Files = $files; ExistingAssertionsChanged = $false; NewA6WitnessIncluded = $true; ExplicitA5SuiteIncluded = $true; Classification = 'A7 report section 11; literal hit is not automatically a characterization conflict' }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    [IO.File]::WriteAllText($destination, (($result | ConvertTo-Json -Depth 10) + "`r`n"), [Text.UTF8Encoding]::new($false))
    Write-Host ('Captured A7 source inventory: ' + $files.Count + ' files, ' + $matches.Count + ' literal matches; no tests changed.')
} finally { Pop-Location }
