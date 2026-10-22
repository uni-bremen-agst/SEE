$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$report = Join-Path $tool 'Evaluation/P5O2B25A7-finite-parametric-generic-context-contract.md'
$text = Get-Content -LiteralPath $report -Raw -Encoding UTF8
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
Require ($text.Contains('Decision: **STOP - no production implementation GO**.')) 'Expected unambiguous decision.'
foreach ($name in @('Candidate A:', 'Candidate B:', 'Candidate C:', 'A+B:', 'A+C:', 'Substitution laws', 'Catch contract', 'Architecture impact matrix', 'Characterization preflight', 'Exact next package')) { Require ($text.Contains($name)) ('Missing contract section: ' + $name) }
foreach ($number in 1..14) { Require ($text -match ('(?m)^\| F' + $number + ' ')) ('Missing obligation: F' + $number) }
# This is a static consistency check, NOT a proof of an analyzer implementation.
# The correlated-family argument is a general finite-state argument in the report;
# no finite enumeration is relabeled as a proof of language non-regularity.
$a6Path = Join-Path $tool 'Evaluation/P5O2B25A6-bounded-generic-context-expansion-after-scc-audit.json'
$a6 = Get-Content -LiteralPath $a6Path -Raw -Encoding UTF8 | ConvertFrom-Json
Require ($a6.EvidenceConsistent -and $a6.Outcome -ceq 'BoundaryAudit') 'A6 evidence must be finalized first.'
Require ($a6.ProductionFilesChanged -eq 0 -and $a6.ExistingTestsChanged -eq 0) 'No new production/test implementation allowed.'
$inventoryPath = Join-Path $tool 'artifacts/p5o2b25a7/inventory.json'
$inventory = Get-Content -LiteralPath $inventoryPath -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($file in $inventory.Files) { Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Characterization source changed after capture: ' + $file.Path) }
$result = [ordered]@{
    Package = 'P5O2B25A7'; GeneratedUtc = [DateTime]::UtcNow.ToString('o'); Head = $a6.Head
    Decision = 'STOP'; ImplementationGo = $false; ContractAnalysisComplete = $true; ContractProvedForFullScope = $false
    ContractOnly = $true; ProductionFilesChanged = 0; ExistingTestsChanged = 0; ConditionalPermissionsExercised = 0; ReadyForB25B = $false; SolutionFindingAdmitted = $false
    RecommendedDirection = 'A+C with a proved observational catch oracle if useful; no full-scope implementation approval'
    Boundary = 'Correlated recursive exception-family closure, parametric catch residuals, generalized SCC completion and completeness-aware Worker/Main projection'
    ExactMaterializationRejected = $true; FiniteBodyGraphIsNotFiniteSolver = $true; A5CycleErasureNotTransferredToChangingSubstitutionCycles = $true
    FiniteFragment = 'Owner-scoped finite substitutions; identity/closing recurrence; linear regular family examples'
    FullScopeGaps = @('Correlated/duplicating family solver', 'Parametric catch entailment and residual representation', 'Generalized SCC/cache completion proof', 'Complete exact/family/unsupported output contract')
    Candidates = @(
        [ordered]@{ Name = 'A'; FiniteBodies = $true; FullClosureProved = $false; Reason = 'Finite equations alone do not terminate concrete unfolding' },
        [ordered]@{ Name = 'B'; FixedFiniteDomainTerminates = $true; FullClosureProved = $false; Reason = 'Substituted predicates/congruence and exact-source projection not proved finite; coarse sound approximation needs explicit precision policy' },
        [ordered]@{ Name = 'C'; RegularLinearFragment = $true; FullClosureProved = $false; Reason = 'Independent regular states do not encode shared-depth parameter correlation' },
        [ordered]@{ Name = 'A+B'; FullClosureProved = $false; Reason = 'Needs observation/concretization/completeness contract' },
        [ordered]@{ Name = 'A+C'; RecommendedResearchDirection = $true; FullClosureProved = $false; Reason = 'Needs correlated family, residual and SCC solver proof' })
    Obligations = @(foreach ($number in 1..14) { 'F' + $number })
    NextPackage = 'P5O2B25A8 - Correlated Recursive Type-Family Closure and Completeness Contract (contract/proof only, NOT production implementation)'
    CharacterizationInventory = 'Report section 11; exact assertion/case boundaries; existing permissions conditional, not exercised; later package must reaffirm applicability'
    RepositorySearch = [ordered]@{ Path = $inventoryPath; Sha256 = (Get-FileHash -LiteralPath $inventoryPath).Hash; Files = $inventory.Files.Count; LiteralMatches = $inventory.LiteralMatches.Count }
    StaticConsistencyPassed = $true; ScientificTheoremsProvedByScript = $false; FreshRuntimeTestsOfProposedContract = $false
    Report = [ordered]@{ Path = $report; Sha256 = (Get-FileHash -LiteralPath $report).Hash }
    A6Audit = [ordered]@{ Path = $a6Path; Sha256 = (Get-FileHash -LiteralPath $a6Path).Hash; ValidationPassed = $a6.ValidationPassed; EvidenceConsistent = $a6.EvidenceConsistent }
    SelfAnalysisFresh = $false; CanonicalFindingDiffFresh = $false; HistoricalEvidenceRewritten = $false
    FinalGitStatus = @(& git --no-optional-locks -C (Join-Path $tool '../..') status --short)
}
$path = Join-Path $tool 'Evaluation/P5O2B25A7-finite-parametric-generic-context-contract-audit.json'
[IO.File]::WriteAllText($path, (($result | ConvertTo-Json -Depth 20) + "`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host 'PASS: static A7 contract/report consistency; scientific/implementation decision is STOP, not a production GO.'
