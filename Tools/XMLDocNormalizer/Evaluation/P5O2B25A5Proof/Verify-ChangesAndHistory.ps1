$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a5'
$start = Get-Content (Join-Path $records 'start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
function Require([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Same([string]$Path, [string]$Sha) { (Test-Path -LiteralPath $Path -PathType Leaf) -and (Get-FileHash -LiteralPath $Path).Hash -ceq $Sha }
function Digest([string]$Text) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($sha.ComputeHash([Text.UTF8Encoding]::new($false).GetBytes($Text))).Replace('-', '') }
    finally { $sha.Dispose() }
}
$exceptions = @('src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSummaryGraphEvaluator.cs', 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs', 'build/Verify-DualVersionBuild.ps1', 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md')
foreach ($file in $start.OriginalFiles) {
    if ($file.Path -notin $exceptions) { Require (Same (Join-Path $tool $file.Path) $file.Sha256) ('Inherited input changed: ' + $file.Path) }
}
$testPath = 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowBoundedGenericContextTests.cs'
$test = Get-Content -LiteralPath (Join-Path $tool $testPath) -Raw -Encoding UTF8
$old = $test.Replace('else { Assert.Equal("Fault", Assert.Single(root.ThrownExceptions).Name); }', 'else { Assert.Empty(root.ThrownExceptions); }')
$old = $old.Replace('else { Assert.Equal("Fault", Assert.Single(current.Entries).ExceptionType.MetadataName); }', 'else { Assert.Empty(current.Entries); }')
$old = $old.Replace('// A5-authorized correction of the caught-first cache-loss cases.', '// Characterize the existing defect. Root -> B -> A -> throw is')
$old = $old.Replace('// Historical A4 report/audit/witnesses retain the original defect.', '// reachable in both orders, but B is cached empty under active A.')
$old = $old.Replace('LostReachableExceptionWithoutUncertainty = false,', 'LostReachableExceptionWithoutUncertainty = !uncaughtFirst,')
$old = $old.Replace('ExistingEvaluatorCorrected = !uncaughtFirst,', 'ExistingEvaluatorCorrected = false,')
$old = $old.Replace('CorrectnessProven = !uncaughtFirst', 'CorrectnessProven = false')
$old = $old.Replace('}, b25a5: !uncaughtFirst);', '});')
$old = $old.Replace('Evidence(string name, object value, bool b25a5 = false)', 'Evidence(string name, object value)')
$old = $old.Replace('b25a5 ? "artifacts/p5o2b25a5/analysis" : "artifacts/p5o2b25a4/analysis"', '"artifacts/p5o2b25a4/analysis"')
Require ((Digest $old) -ceq ($start.OriginalFiles | Where-Object Path -ceq $testPath).Sha256) 'A4 test differs beyond the exact authorized changes.'
$gatePath = 'build/Verify-DualVersionBuild.ps1'
$gate = Get-Content -LiteralPath (Join-Path $tool $gatePath) -Raw -Encoding UTF8
$restoredGate = [regex]::Replace($gate, '(?m)^        # B25A5: completed recursive results retain their SCC ancestor-exclusion context\.\r?\n', '')
$restoredGate = [regex]::Replace($restoredGate, '(?m)^        Invoke-DotNet ''main-recursive-summary-closure-tests''[^\r\n]*\r?\n', '')
Require ((Digest $restoredGate) -ceq ($start.OriginalFiles | Where-Object Path -ceq $gatePath).Sha256) 'Shared gate differs beyond A5 additive two lines.'
$registerPath = 'Evaluation/OPEN-PIPELINE-BOUNDARIES.md'
$register = Get-Content -LiteralPath (Join-Path $tool $registerPath) -Raw -Encoding UTF8
$restoredRegister = [regex]::Replace($register, '(?m)^- P5O2B25A5 [^\r\n]*\r?\n', '')
Require ((Digest $restoredRegister) -ceq ($start.OriginalFiles | Where-Object Path -ceq $registerPath).Sha256) 'Boundary register differs beyond A5 additive paragraph.'
Require ((& git --no-optional-locks -C $repo rev-parse HEAD) -ceq $start.Head) 'External HEAD movement.'
& git --no-optional-locks -C $repo diff --cached --quiet
Require ($LASTEXITCODE -eq 0) 'Semantic index changed.'
Require (Same (Join-Path $repo '.gitignore') $start.IgnoreSha256) 'Protected ignore changed.'
Require (Same (Join-Path $repo '.git/index') $start.IndexSha256) 'Physical index hash changed; document, never repair.'
$stashes = @(& git --no-optional-locks -C $repo reflog show refs/stash --format='%H %gs')
Require (($stashes -join "`n") -ceq ($start.Stashes -join "`n")) 'Stash inventory changed.'
$core = @(Get-ChildItem (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -Recurse -Force -File)
Require ($core.Count -eq $start.Core.Count) 'Protected Core inventory changed.'
foreach ($file in $start.Core) { Require (Same $file.Path $file.Sha256) ('Core changed: ' + $file.Path) }

# Read-only verification of historical B25--A4 evidence. Resolve updated
# shared files from exact retained snapshots, NEVER rewrite old evidence.
$previous = Get-Content (Join-Path $tool 'artifacts/p5o2b25a4/historical-evidence-verification.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$checks = [Collections.Generic.List[object]]::new()
function Fingerprints($Value) {
    if ($null -eq $Value -or $Value -is [string] -or $Value.GetType().IsValueType) { return }
    if ($Value -is [Array]) { foreach ($v in $Value) { Fingerprints $v }; return }
    if ($Value.Path -and $Value.Sha256) {
        $original = $Value.Path; $actual = $original
        $relative = if ($original.StartsWith($tool + '\', [StringComparison]::OrdinalIgnoreCase)) { $original.Substring($tool.Length + 1).Replace('\', '/') } else { '' }
        $candidates = @($original)
        if ($relative) { $candidates += Join-Path $records ('inherited/' + $relative) }
        $candidates += @($previous.Fingerprints | Where-Object { $_.HistoricalPath -ceq $original -and $_.Sha256 -ceq $Value.Sha256 } | ForEach-Object CheckedPath)
        $verified = $false
        foreach ($candidate in $candidates) {
            if (Same $candidate $Value.Sha256) { $actual = $candidate; $verified = $true; break }
            if ($candidate -match '^([0-9a-f]{40}):(.+)$') {
                $content = & git --no-optional-locks -C $repo show ($Matches[1] + ':Tools/XMLDocNormalizer/' + $Matches[2]) | Out-String
                Require ($LASTEXITCODE -eq 0) 'Historical committed blob unavailable.'
                if ((Digest ([regex]::Replace($content, "`r?`n", "`r`n"))) -ceq $Value.Sha256) { $actual = $candidate; $verified = $true; break }
            }
        }
        Require $verified ('Historical evidence fingerprint changed: ' + $original)
        $checks.Add([ordered]@{ HistoricalPath = $original; CheckedPath = $actual; Sha256 = $Value.Sha256 })
    }
    foreach ($p in $Value.psobject.Properties) { Fingerprints $p.Value }
}
$historical = @(foreach ($name in @('P5O2B25-bounded-solution-finding-adoption-audit.json', 'P5O2B25A-assembly-qualified-type-ownership-audit.json', 'P5O2B25A2-canonical-exception-type-identity-audit.json', 'P5O2B25A3-constructed-generic-summary-context-audit.json', 'P5O2B25A4-bounded-generic-context-expansion-audit.json')) {
    $path = Join-Path $tool ('Evaluation/' + $name)
    $entry = $start.OriginalFiles | Where-Object Path -ceq ('Evaluation/' + $name)
    Require (Same $path $entry.Sha256) ('Historical audit changed: ' + $name)
    $audit = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    Fingerprints $audit
    [ordered]@{ File = $name; Sha256 = $entry.Sha256; Passed = $audit.Passed; ValidationPassed = $audit.ValidationPassed; EvidenceConsistent = $audit.EvidenceConsistent }
})
[IO.File]::WriteAllText((Join-Path $records 'changes-and-history.json'), (([ordered]@{
    Package = 'P5O2B25A5'; Passed = $true; GeneratedUtc = [DateTime]::UtcNow.ToString('o'); Head = $start.Head
    OriginalFiles = $start.OriginalFiles.Count; AuthorizedCharacterizationMethods = 2; AuthorizedCases = 4; OtherExistingTestsUnchanged = $true
    AdditiveGateOnly = $true; AdditiveRegisterOnly = $true; ProtectedIgnoreUnchanged = $true; CoreFilesUnchanged = $core.Count; StashesUnchanged = $stashes.Count; PhysicalAndSemanticIndexUnchanged = $true
    HistoricalAudits = $historical; HistoricalFingerprints = @($checks.ToArray()); HistoricalReportsAuditsWitnessesRewritten = $false
} | ConvertTo-Json -Depth 12) + "`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host ('PASS: exact authorized delta, protected inputs, ' + $checks.Count + ' historical evidence fingerprints, frozen B25--A4 outcomes.')
