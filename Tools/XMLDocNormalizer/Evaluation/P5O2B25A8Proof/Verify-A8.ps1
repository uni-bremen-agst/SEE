$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a8'
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Hash([string]$path) { [ordered]@{ Path = [IO.Path]::GetFullPath($path); Sha256 = (Get-FileHash -LiteralPath $path).Hash } }
$start = Json (Join-Path $records 'start.json')
foreach ($file in $start.OriginalFiles) { Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Inherited file changed: ' + $file.Path) }
Require ((& git --no-optional-locks -C $repo rev-parse HEAD) -ceq $start.Head) 'External HEAD movement.'
Require ((Get-FileHash (Join-Path $repo '.git/index')).Hash -ceq $start.IndexSha256) 'Physical index changed; document, never repair.'
Require ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -ceq $start.IgnoreSha256) 'Protected ignore changed.'
& git --no-optional-locks -C $repo diff --cached --quiet
Require ($LASTEXITCODE -eq 0) 'Semantic index changed.'
$historical = [Collections.Generic.List[object]]::new()
function Fingerprints($value) {
    if ($null -eq $value -or $value -is [string] -or $value.GetType().IsValueType) { return }
    if ($value -is [Array]) { foreach ($entry in $value) { Fingerprints $entry }; return }
    if ($value.Path -and $value.Sha256) {
        Require ((Get-FileHash -LiteralPath $value.Path).Hash -ceq $value.Sha256) ('Frozen A6/A7 artifact changed: ' + $value.Path)
        $historical.Add([ordered]@{ Path = $value.Path; Sha256 = $value.Sha256 })
    }
    foreach ($property in $value.psobject.Properties) { Fingerprints $property.Value }
}
foreach ($name in @('P5O2B25A6-bounded-generic-context-expansion-after-scc-audit.json', 'P5O2B25A7-finite-parametric-generic-context-contract-audit.json')) { Fingerprints (Json (Join-Path $tool ('Evaluation/' + $name))) }
foreach ($relative in @('artifacts/p5o2b25a6-r2/evidence-gate-process.json', 'artifacts/p5o2b25a7/contract-gate-process.json')) {
    $original = Json (Join-Path $tool $relative)
    $auditName = if ($relative.Contains('p5o2b25a6')) { 'Evaluation/P5O2B25A6-bounded-generic-context-expansion-after-scc-audit.json' } else { 'Evaluation/P5O2B25A7-finite-parametric-generic-context-contract-audit.json' }
    Require ($original.ExitCode -eq 0 -and (Get-FileHash (Join-Path $tool $auditName)).Hash -ceq $original.AuditSha256) 'Original A6/A7 completed gate changed.'
}
$a6 = Json (Join-Path $records 'a6-regression-audit.json')
$a7 = Json (Join-Path $records 'a7-consistency-audit.json')
Require ($a6.EvidenceConsistent -and $a6.ProductionFilesChanged -eq 0 -and $a6.ExistingTestsChanged -eq 0) 'Fresh inherited regression evidence inconsistent.'
Require ($a7.StaticConsistencyPassed -and $a7.Decision -ceq 'STOP' -and !$a7.ImplementationGo) 'Fresh historical A7 consistency gate mismatch.'
$proof = Json (Join-Path $records 'proof.json')
Require (@($proof.Checks).Count -eq 40 -and @($proof.Checks | Where-Object { !$_.Passed }).Count -eq 0) 'Forty finite proof checks required.'
Require (!$proof.ProductAnalyzerExecuted -and !$proof.GeneralUndecidabilityProvedByFiniteHarness -and !$proof.InfiniteRuntimeAnalysisExecuted) 'Do not confuse finite illustration with a product/general proof.'
$proofInputs = Json (Join-Path $records 'proof-inputs.json')
$proofDirectory = Join-Path $records 'finite-proof-r2'
Require ((Get-FileHash (Join-Path $PSScriptRoot 'FamilyProof.cs')).Hash -ceq $proofInputs.Source -and (Get-FileHash (Join-Path $proofDirectory 'FamilyProof.cs')).Hash -ceq $proofInputs.Source) 'Proof source binding changed.'
Require ((Get-FileHash (Join-Path $PSScriptRoot 'FamilyProof.csproj')).Hash -ceq $proofInputs.Project) 'Proof project changed.'
Require ((Get-FileHash (Join-Path $proofDirectory 'bin/Debug/net8.0/FamilyProof.dll')).Hash -ceq $proofInputs.Image) 'Proof image changed.'
foreach ($dll in $proofInputs.Inputs) {
    Require ((Get-FileHash -LiteralPath $dll.Path).Hash -ceq $dll.Sha256) 'Actual Current compiler input changed.'
    Require ((Get-FileHash -LiteralPath (Join-Path $proofDirectory ('bin/Debug/net8.0/' + [IO.Path]::GetFileName($dll.Path)))).Hash -ceq $dll.Sha256) 'Proof runtime uses a different compiler dependency.'
}
$failedLauncher = Json (Join-Path $records 'runs/proof-build.json')
Require ($failedLauncher.ExitCode -ne 0 -and @($failedLauncher.Arguments).Count -eq 1 -and $failedLauncher.Arguments[0].StartsWith('build,')) 'Preserve and classify initial pre-compilation argument-marshaling failure.'
foreach ($name in @('FamilyProof.cs', 'FamilyProof.csproj')) { Require ((Get-FileHash (Join-Path $records ('finite-proof/' + $name))).Hash -ceq (Get-FileHash (Join-Path $proofDirectory $name)).Hash) 'C# proof was changed rather than only the launcher.' }
$reportPath = Join-Path $tool 'Evaluation/P5O2B25A8-correlated-recursive-type-family-contract.md'
$report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8
Require ($report.Contains('Decision: **STOP - no production implementation GO**.')) 'Exactly one explicit A8 decision required.'
foreach ($section in @('PO1', 'PO2', 'PO3', 'PCP', 'B25B readiness matrix', 'Wire decision', 'Soundness')) { Require ($report -match [regex]::Escape($section)) ('Missing contract section: ' + $section) }
$architecture = [regex]::Match($report, '(?s)## 10\..*?(?=## 11\.)').Value
$readiness = [regex]::Match($report, '(?s)## 11\..*?(?=## 12\.)').Value
$architectureRows = [regex]::Matches($architecture, '(?m)^\| ').Count - 1
$readinessRows = [regex]::Matches($readiness, '(?m)^\| ').Count - 1
Require ($architectureRows -eq 20 -and $readinessRows -eq 15) 'Required architecture/readiness matrix inventory mismatch.'
Require ([regex]::Matches($readiness, '(?m)^\| [^|]+\| (CLOSED|CONTRACT-DEFINED|OPEN) \|').Count -eq 15) 'Readiness status vocabulary mismatch.'
$inventory = Json (Join-Path $records 'characterization-inventory.json')
foreach ($file in $inventory.Files) { Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Characterization source changed: ' + $file.Path) }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Format-Owned.ps1') -VerifyOnly
Require ($LASTEXITCODE -eq 0) 'A8 UTF8/CRLF mismatch.'
$format = & dotnet format whitespace . --folder --include Evaluation/P5O2B25A8Proof/FamilyProof.cs --verify-no-changes --verbosity quiet 2>&1 | Out-String
Require ($LASTEXITCODE -eq 0) ('Proof source format mismatch: ' + $format)
$parseChecks = @(Get-ChildItem $PSScriptRoot -Filter '*.ps1' -File | ForEach-Object {
    $errors = $null; $tokens = $null; $null = [Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$errors)
    Require ($errors.Count -eq 0) ('PowerShell syntax error: ' + $_.Name)
    Hash $_.FullName
})
$runs = @(foreach ($name in @('build', 'dual', 'witnesses', 'contexts', 'broad', 'full', 'boundary', 'proof-build-r2', 'proof', 'a6-gate', 'a7-gate')) {
    $path = Join-Path $records ('runs/' + $name + '.json'); $run = Json $path
    Require ($null -ne $run.ExitCode) ('Unfinished required attempt: ' + $name)
    [ordered]@{ Name = $name; ExitCode = $run.ExitCode; Artifact = (Hash $path); Log = (Hash (Join-Path $records ('runs/' + $name + '.log'))) }
})
$failures = @($runs | Where-Object ExitCode -ne 0)
$owned = @(Get-ChildItem $PSScriptRoot -File -Recurse | Sort-Object FullName | ForEach-Object { Hash $_.FullName })
$result = [ordered]@{
    Package = 'P5O2B25A8'; GeneratedUtc = [DateTime]::UtcNow.ToString('o'); Head = $start.Head
    Decision = 'STOP'; ImplementationGo = $false; ContractProofOnly = $true; ProductionFilesChanged = 0; ExistingTestsChanged = 0; ConditionalPermissionsExercised = 0; FindingAdmission = $false; ReadyForB25B = $false
    ProofObligations = @([ordered]@{ Name = 'PO1'; FiniteCorrelatedDenotation = $true; FullEffectiveOperationsClosed = $false }, [ordered]@{ Name = 'PO2'; GuardedRemainderDenotationClosed = $true; FullSemanticQueryClosed = $false }, [ordered]@{ Name = 'PO3'; FiniteStructuralConstruction = $true; FullSemanticClosureAndProjectionClosed = $false })
    Boundary = 'Exact correlated parametric-catch query/projected Witness membership embeds PCP; finite guarded syntax alone is not a total exact query solver'
    ConservativeAnalysisProvedImpossible = $false; GeneralUndecidabilityProvedByFiniteHarness = $false
    NextPackage = 'P5O2B25A9 - Decidable Query Profile and Incomplete Projection Certificate (contract/proof only)'
    ValidationPassed = ([bool]$a6.ValidationPassed -and $failures.Count -eq 0); EvidenceConsistent = $true; StaticProofConsistencyPassed = $true; FailedRequiredRuns = $failures
    FreshTests = $a6.Tests; Runs = $runs; WaeBuilds = $a6.WaeBuilds; CurrentBaselines = $a6.CurrentBaselines; SourceBindings = $a6.SourceBindings; PdbDocuments = $a6.PdbDocuments
    A6FreshGate = (Hash (Join-Path $records 'a6-regression-audit.json')); A7FreshGate = (Hash (Join-Path $records 'a7-consistency-audit.json')); ProofResult = (Hash (Join-Path $records 'proof.json')); ProofInputs = (Hash (Join-Path $records 'proof-inputs.json')); FiniteProofChecks = 40
    InitialLauncherFailure = [ordered]@{ BeforeCompilation = $true; CorrectedCommandOnly = $true; SourceAndAssertionsUnchanged = $true; Record = (Hash (Join-Path $records 'runs/proof-build.json')); Log = (Hash (Join-Path $records 'runs/proof-build.log')); UnchangedRetry = $false }
    ProtectedInputs = (Hash (Join-Path $records 'start.json')); InheritedFiles = $start.OriginalFiles.Count; HistoricalA6A7Fingerprints = @($historical.ToArray()); EarlierHistoricalProtection = $a6.Protection; HistoricalEvidenceRewritten = $false
    ArchitectureComponents = $architectureRows; ReadinessRequirements = $readinessRows; CharacterizationFiles = $inventory.Files.Count; CharacterizationMatches = $inventory.LiteralMatches.Count
    CharacterizationInventory = (Hash (Join-Path $records 'characterization-inventory.json')); CharacterizationTable = (Hash (Join-Path $PSScriptRoot 'Characterization-preflight.md')); Report = (Hash $reportPath); OwnedFiles = $owned; PowerShellParseChecks = $parseChecks
    SelfAnalysisFresh = $false; CanonicalFindingDiffFresh = $false; ActualLinuxRun = $false; CompilerVersionsChanged = $false; CurrentHistoricalRoutingChanged = $false; NoSkipsOrGreenRetries = $true
    FinalGitStatus = @(& git --no-optional-locks -C $repo status --short)
}
$path = Join-Path $tool 'Evaluation/P5O2B25A8-correlated-recursive-type-family-contract-audit.json'
[IO.File]::WriteAllText($path, (($result | ConvertTo-Json -Depth 100) + "`r`n"), [Text.UTF8Encoding]::new($false))
if (!$result.ValidationPassed) { Write-Host 'FAIL: required validation failures retained; A8 contract decision remains STOP.'; exit 1 }
Write-Host 'PASS: A8 finite-proof/evidence consistency and fresh validation; contract decision STOP, NO production GO.'
