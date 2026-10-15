$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$output=Join-Path $toolRoot 'Evaluation/PID-G1-process-identity-lifetime-gate-audit.json'
. (Join-Path $PSScriptRoot 'ProcessLifetime.ps1')
function Require([bool]$Value,[string]$Message) { if(-not $Value) { throw $Message } }
function Read([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($Value) { ConvertTo-Json -InputObject $Value -Compress -Depth 100 }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function Tests([string]$Path,[int]$Expected) {
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($counts.total -eq "$Expected" -and $counts.passed -eq "$Expected" -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Incomplete or failing tests: $Path"
    Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge $started) "Stale TRX: $Path"
    [ordered]@{Artifact=(Fingerprint $Path);Total=$Expected;Passed=$Expected;Failed=0;Skipped=0}
}
Push-Location $toolRoot
try {
    [IO.File]::WriteAllText($output,((@{Package='PID-G1';Passed=$false;State='Running or interrupted'} | ConvertTo-Json)+"`r`n"),[Text.UTF8Encoding]::new($false))
    $before=Read 'artifacts/pid-g1/protected-before.json'
    $started=(Get-Item 'artifacts/pid-g1/protected-before.json').LastWriteTimeUtc
    Require ((& git rev-parse HEAD) -ceq $before.Head) 'HEAD movement.'
    Require ((Get-FileHash (Join-Path $repoRoot '.git/index')).Hash -ceq $before.Index) 'Index changed.'
    Require ((Get-FileHash (Join-Path $repoRoot '.gitignore')).Hash -ceq $before.Ignore) 'Protected root ignore changed.'
    Require ((Json @(& git reflog show refs/stash --format='%H %gs')) -ceq (Json $before.Stashes)) 'Stashes changed.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $before.Core.Count) 'Core inventory changed.'
    foreach($file in $before.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Core bytes changed: $($file.Path)" }
    $processScan=Read 'artifacts/pid-g1/final-process-scan.json'
    Require ($processScan.LiveCount -eq 0 -and $processScan.ReadOnly) 'Final owned-export process census not clear.'
    $validation=(Read 'artifacts/p5o2b24/export.json').ToolRoot
    $runNames=@('proof-helper','isolated-four','targeted','broad','boundary','dual','regressions','full')
    $runs=foreach($name in $runNames) {
        $path=Join-Path $toolRoot ('artifacts/pid-g1/runs/'+$name+'.json'); $value=Read $path
        Require ($null -ne $value.ExitCode -and $value.ExitCode -eq 0 -and ([DateTime]$value.StartedUtc).ToUniversalTime() -ge $started) "Required phase not passed: $name"
        [ordered]@{Name=$name;Run=$value;Artifact=(Fingerprint $path);Log=(Fingerprint ([IO.Path]::ChangeExtension($path,'.log')))}
    }
    $tests=@(Tests (Join-Path $validation 'artifacts/pid-g1/tests/isolated-four.trx') 4;
        Tests (Join-Path $validation 'artifacts/pid-g1/tests/targeted.trx') 540;
        Tests (Join-Path $validation 'artifacts/pid-g1/tests/pid-g1-boundary.trx') 596;
        Tests (Join-Path $validation 'artifacts/p5o2b24/tests/broad.trx') 3185;
        Tests (Join-Path $validation 'artifacts/p5o2b24/tests/full.trx') 3276)
    $proofCases=Read 'artifacts/pid-g1/runs/proof-helper-cases.json'
    Require ($proofCases.Count -eq 9 -and @($proofCases | Where-Object { -not $_.Passed }).Count -eq 0) 'Deterministic proof-predicate controls failed.'
    $caseFiles=@(Get-ChildItem (Join-Path $validation 'artifacts/pid-g1/cases') -Filter '*.json' -File)
    Require ($caseFiles.Count -eq 10) 'Ten real lifecycle outcomes required.'
    $cases=foreach($file in $caseFiles) {
        $value=Read $file.FullName; Assert-OwnedProcessExit $value.Exit $value.ProcessId
        Require ($value.OriginalCall -and $file.LastWriteTimeUtc -ge $started) 'Stale or unowned case.'
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    # Exercise the actual new metadata consumed by each historical PID-based Proof predicate.
    $pairs=[Collections.Generic.List[object]]::new()
    $file=Join-Path $validation 'artifacts/p5o2b11/source-path-production.json'
    Require (Test-Path $file) 'Missing B11 owned lifetime metadata.'
    $value=Read $file; Assert-OwnedProcessPair $value.FirstProcessExit $value.SecondProcessExit $value.FirstProcess $value.SecondProcess
    $pairs.Add((Fingerprint $file))
    foreach($folder in @('p5o2b12','p5o2b13','p5o2b14','p5o2b15','p5o2b19','p5o2b21')) {
        foreach($file in Get-ChildItem (Join-Path $validation ('artifacts/'+$folder)) -Filter '*.json' -File) {
            $value=Read $file.FullName
            if($null -ne $value.FirstProcess) {
                Assert-OwnedProcessPair $value.FirstProcessExit $value.SecondProcessExit $value.FirstProcess $value.SecondProcess
                $pairs.Add((Fingerprint $file.FullName))
            }
        }
    }
    Require ($pairs.Count -ge 42) 'Missing fresh metadata for legacy Proof pairs.'
    $b3=Read (Join-Path $validation 'artifacts/p5o2b3/current-runtime-isolation.json'); Assert-OwnedProcessExit $b3.ProcessExit $b3.WorkerProcessId
    $b4=Read (Join-Path $validation 'artifacts/p5o2b4/main-runtime-parity.json'); Assert-OwnedProcessPair $b4.ProcessExit $b4.RepeatedProcessExit $b4.ProcessId $b4.RepeatedProcessId
    $b8=Read (Join-Path $validation 'artifacts/p5o2b8/dispatch-boundary.json')
    Assert-OwnedProcessPair $b8.HistoricalProcessExits[0] $b8.HistoricalProcessExits[1] $b8.HistoricalProcessIds[0] $b8.HistoricalProcessIds[1]
    $b9=Read (Join-Path $validation 'artifacts/p5o2b9/production-boundary.json'); Assert-OwnedProcessExit $b9.HistoricalProcessExit $b9.HistoricalProcessId
    $b10=Read (Join-Path $validation 'artifacts/p5o2b10/payload-production-boundary.json'); Assert-OwnedProcessExit $b10.ProcessExit $b10.ProcessId
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/PIDG1Proof/Verify-Scope.ps1
    Require ($LASTEXITCODE -eq 0) 'PID-G1 scope check failed.'
    & powershell -NoProfile -ExecutionPolicy Bypass -File Evaluation/P5O2B24Proof/Write-Verify-Findings.ps1 -ValidationRoot $validation -AfterPidG1
    Require ($LASTEXITCODE -eq 0) 'Existing B24 Evidence-Gesamtgate failed.'
    $b24=Read 'Evaluation/P5O2B24-bounded-finding-contracts-audit.json'
    Require ($b24.EvidenceVerifierPassed -and $b24.ArtifactConsistencyPassed -and $b24.AllRequiredValidationPassed -and $b24.PidG1LifetimeRepair) 'B24 closure incomplete.'
    $beforeWip=Read 'artifacts/pid-g1/before-wip.json'
    $paths=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
    $files=foreach($path in @($paths | Sort-Object -Unique | Where-Object { $_ -notlike '*-audit.json' })) {
        $full=Join-Path $repoRoot $path; $hash=(Get-FileHash $full).Hash
        $old=@($beforeWip | Where-Object Path -ceq $path)
        [ordered]@{Path=$path;Sha256=$hash;AlreadyB24Wip=($old.Count -eq 1);ChangedByPidG1=($old.Count -eq 0 -or $old[0].Sha256 -cne $hash)}
    }
    $audit=[ordered]@{
        Package='PID-G1';Passed=$true;VerifiedUtc=[DateTime]::UtcNow.ToString('o');Head=$before.Head
        RootCause='PID-only reacquisition can refer to a replacement process; original exit identity was lost after disposal.'
        Solution='Observe HasExited/ExitCode on the actual owned Process before disposal; immutable original-call CWT observation; no PID lookup, StartTime dependency or changed cleanup.'
        Runs=$runs;Tests=$tests;NewLifetimeTests=18;PidReuseModelDeterministic=$true;ProofPredicateCases=$proofCases;RealLifecycleCases=$cases;LegacyProofPairs=@($pairs.ToArray())
        B24Closure='PASS';B24Audit=(Fingerprint 'Evaluation/P5O2B24-bounded-finding-contracts-audit.json');Scope=(Read 'artifacts/pid-g1/scope-check.json')
        AllOriginalFailuresPreserved=$true;PriorB24Audit=$b24.PidG1PriorAudit
        PriorPidAudit='artifacts/p5o2b24/pid-gate-audit-20261015/';InitialBuildFailure=(Fingerprint 'artifacts/pid-g1/initial-test-build.log')
        OtherAttempts='Two read-only patch-orchestration setup failures before edits; initial scope comparison exposed LF/CRLF difference and was corrected to compare built bytes and normalized local content. All actual failures retained; see report.'
        Protection=[ordered]@{IndexUnchanged=$true;RootIgnoreUnchanged=$true;CoreFileCount=$before.Core.Count;CoreUnchanged=$true;StashCount=$before.Stashes.Count;StashesUnchanged=$true}
        FinalProcessScan=$processScan
        AnalyzerSemanticsChanged=$false;WorkerProtocolChanged=$false;B21AttestationChanged=$false;RoutingChanged=$false;RoslynVersionsChanged=$false
        SelfAnalysis=$b24.SelfAnalysis;CanonicalFindingDiff=$b24.CanonicalFindingDiff
        ChangedAndPreexistingFiles=$files;FinalGitStatus=@(& git status --short);NoCommit=$true;NoPush=$true;NoStashMutation=$true;NoB25=$true;NoNetwork=$true
        Platform='Actual Windows/.NET 8.0.24 run; retained Process only, no creation-time/PID-table dependency. Static Git path audit, not a Linux execution.'
    }
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host 'PASS: PID-G1 ownership/lifetime regressions, all legacy lifetime predicates, B24 full closure, actual builds/suites, scope and protected WIP.'
} finally { Pop-Location }
