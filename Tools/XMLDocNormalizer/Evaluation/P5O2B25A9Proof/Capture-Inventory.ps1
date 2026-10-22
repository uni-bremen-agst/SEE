$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = Get-Content (Join-Path $tool 'Evaluation/P5O2B25A7Proof/Capture-Inventory.ps1') -Raw -Encoding UTF8
$source = $source.Replace('artifacts/p5o2b25a7/inventory.json', 'artifacts/p5o2b25a9/characterization-inventory.json')
$source = $source.Replace('$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', ('$tool = ''' + $tool.Replace("'", "''") + "'"))
& ([scriptblock]::Create($source))
$destination = Join-Path $tool 'artifacts/p5o2b25a9/query-inventory.json'
if (Test-Path -LiteralPath $destination) { throw 'Preserve query capture.' }
Push-Location $tool
try {
    $pattern = 'OriginalDefinition|GetFirstSymbolForDeclarationId|ResolveType|Suppresses|SuppressCaughtException|SuppressAll|RegisterMethodTarget|GetOrAdd|DequeuePending|Uncertainties|PathsTruncated|UnsupportedContext|ValidCanonicalResult|EvidenceKind|RebindCallable'
    $roots = @('src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow', 'src/XMLDocNormalizer/Execution', 'src/XMLDocNormalizer/Models/DTO', 'src/XMLDocNormalizer.HistoricalWorker')
    $lines = @(& rg -n $pattern @roots --glob '*.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Query search failed.' }
    $paths = @(& rg -l $pattern @roots --glob '*.cs')
    $files = @(foreach ($path in $paths | Sort-Object -Unique) { [ordered]@{ Path = $path.Replace('\', '/'); Sha256 = (Get-FileHash -LiteralPath $path).Hash } })
    $result = [ordered]@{ CapturedUtc = [DateTime]::UtcNow.ToString('o'); Roots = $roots; Pattern = $pattern; Matches = $lines; Files = $files; ProductChanged = $false; Scope = 'A9 report query table, not proof of arbitrary C# reachability' }
    [IO.File]::WriteAllText($destination, (($result | ConvertTo-Json -Depth 10) + "`r`n"), [Text.UTF8Encoding]::new($false))
    Write-Host ('A9 query inventory: ' + $files.Count + ' files, ' + $lines.Count + ' literal matches.')
} finally { Pop-Location }
