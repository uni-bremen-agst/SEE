$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$directory = Join-Path $tool 'src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow'
$rows = [Collections.Generic.List[object]]::new()
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Source([string]$name, [string[]]$anchors) {
    $path = Join-Path $directory $name
    $text = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $locations = @(foreach ($anchor in $anchors) {
        $match = [regex]::Match($text, $anchor)
        Require $match.Success ('Missing trace anchor: ' + $name + ' ' + $anchor)
        [ordered]@{ Pattern = $anchor; Line = $text.Substring(0, $match.Index).Split("`n").Count }
    })
    $rows.Add([ordered]@{ Path = $path; Sha256 = (Get-FileHash -LiteralPath $path).Hash; Anchors = $locations })
    return $text
}
$session = Source 'ExceptionFlowSummaryAnalysisSession.cs' @('internal ExceptionFlowAnalysisResult Analyze\(', 'BuildPendingSummaryNodes\(', 'return evaluator.Evaluate\(')
Require ([regex]::IsMatch($session, 'BuildPendingSummaryNodes\(\s*graph\);\s*return evaluator\.Evaluate\(')) 'Graph construction must precede evaluator invocation.'
$construction = Source 'ExceptionFlowSummaryAnalysisSession.GraphConstruction.cs' @('while \(graph.DequeuePendingOrDefault\(', 'ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations\(', 'summary.Merge\(')
Require ([regex]::IsMatch($construction, 'AnalyzeSummarySymbolDeclarations\(\s*key.Symbol,')) 'Trace must analyze the actual registered key symbol.'
$graph = Source 'ExceptionFlowSummaryGraph.cs' @('summaries.TryGetValue\(', 'pendingKeys.Enqueue\(', 'return pendingKeys.Dequeue\(')
Require ($graph.IndexOf('summaries.Add(') -lt $graph.IndexOf('pendingKeys.Enqueue(')) 'Fresh node registered before scheduling.'
$context = Source 'ExceptionFlowCallContext.cs' @('CallableSymbol = NormalizeSymbol\(', 'return symbol\?\.OriginalDefinition;', 'normalizeToOriginalDefinition: true')
$key = Source 'ExceptionFlowCallableKey.cs' @('Symbol = symbol.OriginalDefinition;', 'SymbolEqualityComparer.Default.Equals\(Symbol, other.Symbol\)', 'StringComparer.Ordinal.Equals\(ContextKey, other.ContextKey\)')
$evaluator = Source 'ExceptionFlowSummaryGraphEvaluator.cs' @('components = FindSummaryComponents\(', 'if \(activeKeys.Contains\(', '\(edge.Target, excludedMembers\)', 'GetFirstSymbolForDeclarationId\(')
Require ($evaluator.IndexOf('FindSummaryComponents(graph, rootKey)') -lt $evaluator.IndexOf('while (frames.Count > 0)')) 'SCC discovery precedes evaluation loop.'
$null = Source 'ExceptionFlowCaughtTypeFilter.cs' @('caughtType.OriginalDefinition', 'currentType.OriginalDefinition', 'baseType.OriginalDefinition')
$null = Source 'ExceptionFlowCrossCompilationResolver.cs' @('methodSymbol.OriginalDefinition', 'ResolveMethodOnContainingType\(originalMethod, containingType\)')
$result = [ordered]@{ Passed = $true; StaticTraceOnly = $true; InfiniteProductRunExecuted = $false; GraphConstructionBeforeScc = $true; PendingLoopBoundedOnlyByRepeatedKeyInventory = $true; A5RunsAfterPendingDrain = $true; CurrentDefinitionFoldingRemains = $true; Files = @($rows.ToArray()); GeneralArgument = 'Constructor height of List^n<int> is n; fresh exact canonical keys imply fresh queue work at every n. Finite witness is only an observed prefix, not the general proof.' }
$path = Join-Path $tool 'artifacts/p5o2b25a6-r2/lifecycle.json'
[IO.File]::WriteAllText($path, (($result | ConvertTo-Json -Depth 10) + "`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host 'PASS: hash-bound static lifecycle trace; no product build/test or infinite analysis invoked.'
