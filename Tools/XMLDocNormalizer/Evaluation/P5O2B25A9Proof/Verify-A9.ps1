$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a9'
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Json([string]$path) { Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Hash([string]$path) { [ordered]@{ Path = [IO.Path]::GetFullPath($path); Sha256 = (Get-FileHash -LiteralPath $path).Hash } }
$start = Json (Join-Path $records 'start.json')
Require ($start.OriginalFiles.Count -eq 1092) 'Inherited A9 inventory mismatch.'
foreach ($file in $start.OriginalFiles) { Require ((Get-FileHash -LiteralPath (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Inherited file changed: ' + $file.Path) }
Require ((& git --no-optional-locks -C $repo rev-parse HEAD) -ceq $start.Head) 'External HEAD movement.'
Require ((Get-FileHash (Join-Path $repo '.git/index')).Hash -ceq $start.IndexSha256) 'Physical index changed; document, never repair.'
Require ((Get-FileHash (Join-Path $repo '.gitignore')).Hash -ceq $start.IgnoreSha256) 'Protected root ignore changed.'
& git --no-optional-locks -C $repo diff --cached --quiet
Require ($LASTEXITCODE -eq 0) 'Semantic index changed.'
$core = @(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -Force -Recurse -File)
Require ($core.Count -eq 23) 'Protected Core inventory changed.'
foreach ($file in $start.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Protected Core bytes changed.' }
$stashes = @(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs')
Require (($stashes -join "`n") -ceq ($start.Stashes -join "`n")) 'Stash inventory changed.'
$frozenA8 = Join-Path $tool 'Evaluation/P5O2B25A8-correlated-recursive-type-family-contract-audit.json'
Require ((Get-FileHash $frozenA8).Hash -ceq 'D79F6C2A07AE1D6CEC4C1C9336C40B0E010916DA4ACC030F88D8C32C8A86579C') 'Finalized A8 audit changed.'
Require ((Json (Join-Path $tool 'artifacts/p5o2b25a8/runs/a8-gate.json')).ExitCode -eq 0) 'Original A8 completed gate mismatch.'
$historical = [Collections.Generic.List[object]]::new()
function Fingerprints($value) {
    if ($null -eq $value -or $value -is [string] -or $value.GetType().IsValueType) { return }
    if ($value -is [Array]) { foreach ($entry in $value) { Fingerprints $entry }; return }
    if ($value.Path -and $value.Sha256) {
        Require ((Get-FileHash -LiteralPath $value.Path).Hash -ceq $value.Sha256) ('Frozen A8 artifact changed: ' + $value.Path)
        $historical.Add([ordered]@{ Path = $value.Path; Sha256 = $value.Sha256 })
    }
    foreach ($property in $value.psobject.Properties) { Fingerprints $property.Value }
}
Fingerprints (Json $frozenA8)
$a6 = Json (Join-Path $records 'a6-regression-audit.json')
$a7 = Json (Join-Path $records 'a7-consistency-audit.json')
$a8 = Json (Join-Path $records 'a8-consistency-audit.json')
Require ($a6.EvidenceConsistent -and $a6.ProductionFilesChanged -eq 0 -and $a6.ExistingTestsChanged -eq 0) 'Fresh A6 evidence inconsistent.'
Require ($a7.StaticConsistencyPassed -and $a7.Decision -ceq 'STOP') 'Historical A7 contract decision changed.'
Require ($a8.EvidenceConsistent -and $a8.StaticProofConsistencyPassed -and $a8.Decision -ceq 'STOP' -and !$a8.ImplementationGo) 'Historical A8 contract decision changed.'
$proof = Json (Join-Path $records 'profile-proof.json')
Require ($proof.ContractProofOnly -and $proof.NoProductDependency -and !$proof.ProductAnalyzerExecuted -and !$proof.ExistingTestsChanged) 'A9 proof must remain independent of production.'
Require ($proof.Profile -ceq 'A9-FiniteOrbit-v1' -and $proof.Checks.Count -eq 37 -and $proof.EnumeratedSchemas -eq 25) 'A9 proof inventory mismatch.'
Require (@($proof.Checks | Where-Object { !$_.Passed }).Count -eq 0 -and !$proof.TerminationProofByFiniteEnumeration) 'Finite tests are not the mathematical termination proof.'
$groups = [ordered]@{
    ObviousExact = 'obvious-identity-exact'; GrowthRejected = 'growing-projection-outside'; MembershipTerminates = 'membership-finite-schema-enumeration-terminates'
    Composition = 'composition-two-scopes'; ConcreteCatch = 'concrete-catch-exact'; FiniteProjection = 'finite-wrapped-source-projection'
    A8Hard = 'a8-pcp-shape-outside-before-expansion'; Correlation = 'finite-correlation-is-not-rejected'; DeterministicCertificate = 'repeated-query-same-classification'
    RepeatedQuery = 'repeated-query-same-classification'; CanonicalEquivalent = 'canonical-equivalent-order-same-classification'; OwnerSensitive = 'owner-sensitive-exact-projections-distinct'
}
foreach ($name in $groups.Values) { Require (@($proof.Checks | Where-Object { $_.Name -ceq $name -and $_.Passed }).Count -eq 1) ('Missing requested proof group: ' + $name) }
Require (!$proof.Growth.Exact -and $proof.Growth.ConcreteContextsMaterialized -eq 0 -and !$proof.HardPattern.Exact -and $proof.HardPattern.ConcreteContextsMaterialized -eq 0) 'Outside queries must not materialize ground contexts in membership.'
Require ($proof.HardPattern.Reasons -contains 'CorrelatedPredicateOutsideFiniteOrbit' -and $proof.Reset.Exact) 'Do not reject every correlation/call cycle or admit the A8 hard pattern.'
$inputs = Json (Join-Path $records 'profile-proof-inputs.json')
$directory = Join-Path $records 'contract-proof'
foreach ($name in @('ProfileProof.cs', 'ProfileProof.csproj')) {
    $expected = if ($name.EndsWith('.csproj')) { $inputs.Project } else { $inputs.Source }
    Require ((Get-FileHash (Join-Path $PSScriptRoot $name)).Hash -ceq $expected -and (Get-FileHash (Join-Path $directory $name)).Hash -ceq $expected) 'Executed A9 proof source/project changed.'
}
Require ((Get-FileHash (Join-Path $directory 'bin/Debug/net8.0/ProfileProof.dll')).Hash -ceq $inputs.Image -and $inputs.NoProductOrRoslynReference) 'Executed A9 proof image changed.'
$project = Get-Content (Join-Path $PSScriptRoot 'ProfileProof.csproj') -Raw
$deps = Get-Content (Join-Path $directory 'bin/Debug/net8.0/ProfileProof.deps.json') -Raw
Require ($project -notmatch '<(ProjectReference|PackageReference|Reference)\b' -and $deps -notmatch 'XMLDocNormalizer|Microsoft.CodeAnalysis') 'A9 proof has a product/compiler dependency.'
$reportPath = Join-Path $tool 'Evaluation/P5O2B25A9-decidable-query-profile.md'
$report = Get-Content $reportPath -Raw -Encoding UTF8
Require ($report.Contains('Decision: **GO - contract ready for a bounded implementation follow-up**.')) 'Expected one bounded contract decision.'
foreach ($required in @('IsWithinExactProfile', 'PositiveConstructorCycle', 'Incomplete Projection Certificate', 'No concrete call-context queue', 'A5 integration', 'Wire decision', 'Height invariant', 'B25B readiness matrix')) {
    Require ($report -match [regex]::Escape($required)) ('Missing contract obligation: ' + $required)
}
foreach ($number in 1..10) { Require ($report -match ('(?m)^\| Q' + $number + ' ')) ('Missing actual query class: Q' + $number) }
$architecture = [regex]::Match($report, '(?s)## 11\..*?(?=## 12\.)').Value
$readiness = [regex]::Match($report, '(?s)## 13\..*?(?=## 14\.)').Value
Require ([regex]::Matches($architecture, '(?m)^\| ').Count -eq 21) 'Twenty architecture components required.'
Require ([regex]::Matches($readiness, '(?m)^\| [^|]+\| (CLOSED|CONTRACT-DEFINED|OPEN) \|').Count -eq 18) 'Eighteen readiness requirements required.'
$inventories = @(foreach ($name in @('characterization-inventory.json', 'query-inventory.json')) {
    $path = Join-Path $records $name; $inventory = Json $path
    foreach ($file in $inventory.Files) { Require ((Get-FileHash (Join-Path $tool $file.Path)).Hash -ceq $file.Sha256) ('Inventoried source changed: ' + $file.Path) }
    [ordered]@{ Name = $name; Files = $inventory.Files.Count; Artifact = (Hash $path) }
})
$tablePath = Join-Path $PSScriptRoot 'Characterization-preflight.md'
$table = Get-Content $tablePath -Raw -Encoding UTF8
foreach ($number in 1..5) { Require ($table -match ('conflict ' + $number)) ('Missing named prior conditional A6 conflict: ' + $number) }
foreach ($case in @('ExactMaterialization_HasFreshSuccessorAtEveryWitnessStep', 'growing=true', 'NO A6 witness edits authorized')) {
    Require ($table.Contains($case)) ('Missing characterization boundary: ' + $case)
}
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Format-Owned.ps1') -VerifyOnly
Require ($LASTEXITCODE -eq 0) 'A9 UTF8/CRLF mismatch.'
$format = & dotnet format whitespace . --folder --include Evaluation/P5O2B25A9Proof/ProfileProof.cs --verify-no-changes --verbosity quiet 2>&1 | Out-String
Require ($LASTEXITCODE -eq 0) ('Proof formatting mismatch: ' + $format)
$parse = @(Get-ChildItem $PSScriptRoot -Filter '*.ps1' -File | ForEach-Object {
    $errors = $null; $tokens = $null; $null = [Management.Automation.Language.Parser]::ParseFile($_.FullName, [ref]$tokens, [ref]$errors)
    Require ($errors.Count -eq 0) ('PowerShell parse error: ' + $_.Name)
    Hash $_.FullName
})
$runs = @(foreach ($name in @('build', 'dual', 'witnesses', 'contexts', 'broad', 'full', 'boundary', 'proof-build-r2', 'proof', 'a9-proof-build', 'a9-proof', 'a6-gate', 'a7-gate', 'a8-gate')) {
    $path = Join-Path $records ('runs/' + $name + '.json'); $run = Json $path
    Require ($null -ne $run.ExitCode) ('Incomplete required attempt: ' + $name)
    [ordered]@{ Name = $name; ExitCode = $run.ExitCode; Artifact = (Hash $path); Log = (Hash (Join-Path $records ('runs/' + $name + '.log'))) }
})
$failures = @($runs | Where-Object ExitCode -ne 0)
$owned = @(Get-ChildItem $PSScriptRoot -File -Recurse | Sort-Object FullName | ForEach-Object { Hash $_.FullName })
$result = [ordered]@{
    Package = 'P5O2B25A9'; GeneratedUtc = [DateTime]::UtcNow.ToString('o'); Head = $start.Head
    Decision = 'GO'; GoScope = 'A9-FiniteOrbit-v1 plus total outside certificate, bounded implementation follow-up after scope/test permissions'
    ContractProofOnly = $true; ProductionImplemented = $false; ProductionFilesChanged = 0; ExistingTestsChanged = 0; ConditionalPermissionsExercised = 0
    ImplementationFollowupAllowedByContract = $true; AdditionalCharacterizationPermissionsRequired = $true; FindingAdmission = $false; ReadyForB25B = $false
    ExactProfile = 'Finite scoped slot dependency graph with no positive constructor edge inside an SCC; finite named terms/closed seeds; checked owner/target/fact grammar'
    TerminationArgument = 'Finite signature and bounded term height h0+W imply finite concrete semantic states; outside detection and certificate before constructed expansion'
    GeneralFamilySolver = $false; AllRuntimeCSharpReachabilityExact = $false; FalseNegativeCoveragePermitted = $true; FalsePositiveAdmissionPermitted = $false
    OutsidePolicy = 'Internal classified certificate; no successful partial result or Finding; existing UnsupportedContext failed envelope with neutral details'
    WireExtensionRequired = $false; CompilerVersionsChanged = $false; CurrentHistoricalRoutingChanged = $false
    ValidationPassed = ([bool]$a6.ValidationPassed -and [bool]$a8.ValidationPassed -and $failures.Count -eq 0); EvidenceConsistent = $true; StaticContractConsistencyPassed = $true; FailedRequiredRuns = $failures
    FreshTests = $a6.Tests; WaeBuilds = $a6.WaeBuilds; CurrentBaselines = $a6.CurrentBaselines; SourceBindings = $a6.SourceBindings; PdbDocuments = $a6.PdbDocuments
    A9ProofChecks = $proof.Checks.Count; A9ProofGroups = $groups; FiniteSchemasEnumerated = $proof.EnumeratedSchemas; FiniteEnumerationIsUniversalProof = $false
    A9ProofResult = (Hash (Join-Path $records 'profile-proof.json')); A9ProofInputs = (Hash (Join-Path $records 'profile-proof-inputs.json')); A8FreshProofChecks = $a8.FiniteProofChecks
    A6FreshGate = (Hash (Join-Path $records 'a6-regression-audit.json')); A7FreshGate = (Hash (Join-Path $records 'a7-consistency-audit.json')); A8FreshGate = (Hash (Join-Path $records 'a8-consistency-audit.json')); Runs = $runs
    InheritedFiles = $start.OriginalFiles.Count; ProtectedInputs = (Hash (Join-Path $records 'start.json')); ProtectedCoreFiles = $core.Count; ProtectedStashes = $stashes.Count
    PhysicalIndexUnchanged = $true; SemanticIndexUnchanged = $true; RootIgnoreUnchanged = $true; FrozenA8Audit = (Hash $frozenA8); FrozenA8Fingerprints = @($historical.ToArray()); EarlierHistoricalProtection = $a6.Protection; HistoricalEvidenceRewritten = $false
    InitialA8LauncherFailureReplayed = $false; NewA9FailedAttempts = $failures; NoSkipsOrGreenRetries = $true
    ActualQueryClasses = 10; ArchitectureComponents = 20; ReadinessRequirements = 18; Inventories = $inventories; CharacterizationTable = (Hash $tablePath)
    Report = (Hash $reportPath); OwnedFiles = $owned; PowerShellParseChecks = $parse; SelfAnalysisFresh = $false; CanonicalFindingDiffFresh = $false; ActualLinuxRun = $false
    NextPackage = 'P5O2B25A10 - Finite-Orbit Admission and Classified Projection Implementation (requires scope and additional test-change approvals)'
    FinalGitStatus = @(& git --no-optional-locks -C $repo status --short)
}
$path = Join-Path $tool 'Evaluation/P5O2B25A9-decidable-query-profile-audit.json'
[IO.File]::WriteAllText($path, (($result | ConvertTo-Json -Depth 100) + "`r`n"), [Text.UTF8Encoding]::new($false))
if (!$result.ValidationPassed) { Write-Host 'FAIL: actual validation failures retained; bounded contract decision recorded separately.'; exit 1 }
Write-Host 'PASS: fresh validation, independent A9 proof and contract/evidence consistency. Bounded contract GO; no production implementation or B25B activation.'
