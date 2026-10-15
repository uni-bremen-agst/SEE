$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$root=Join-Path $toolRoot 'artifacts/p5o2b24/pid-gate-audit-20261015'
function Read([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path).Hash }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Hash $Path)} }
function Require([bool]$Value,[string]$Message) { if(-not $Value) { throw $Message } }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 30 -Compress }
Push-Location $toolRoot
try {
    $before=Read (Join-Path $root 'before.json')
    Require ((& git rev-parse HEAD) -ceq $before.Head) 'HEAD changed.'
    Require ((Hash (Join-Path $repoRoot '.git/index')) -ceq $before.Index) 'Index changed.'
    Require ((Hash (Join-Path $repoRoot '.gitignore')) -ceq $before.Ignore) 'Root ignore changed.'
    Require ((Json @(& git reflog show refs/stash --format='%H %gs')) -ceq (Json $before.Stashes)) 'Stashes changed.'
    foreach($file in $before.Files) { Require ((Hash (Join-Path $repoRoot $file.Path)) -ceq $file.Sha256) "Production/test/build input changed: $($file.Path)" }
    $core=@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File)
    Require ($core.Count -eq $before.Core.Count) 'Core inventory changed.'
    foreach($file in $before.Core) { Require ((Hash $file.Path) -ceq $file.Sha256) "Core changed: $($file.Path)" }
    $validation=(Read 'artifacts/p5o2b24/export.json').ToolRoot
    Require ((Hash (Join-Path $validation 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll')) -ceq $before.TestImage) 'Test image changed.'
    $manifest=Read (Join-Path $root 'preserved-artifacts.json')
    foreach($file in $manifest) { Require ((Hash $file.Path) -ceq $file.Sha256) "Preserved failed evidence changed: $($file.Path)" }
    $isolated=Read (Join-Path $root 'isolated.json'); $broad=Read (Join-Path $root 'broad.json')
    Require ($isolated.ExitCode -eq 0 -and $isolated.Counts.Passed -eq 3 -and $isolated.Counts.Skipped -eq 0) 'Unexpected isolated result.'
    Require ($broad.ExitCode -eq 1 -and $broad.Counts.Total -eq 3167 -and $broad.Counts.Failed -eq 1 -and $broad.Counts.Skipped -eq 0) 'Unexpected Broad result.'
    $failures=@($broad.Results | Where-Object Outcome -eq 'Failed')
    Require ($failures.Count -eq 1 -and $failures[0].Test -like '*ExceptionFlowHistoricalReferenceIdentityTests*version: 1*' -and $failures[0].Output -like '*Stopped(Int32 pid)*') 'Unexpected failure; inspect before documenting.'
    foreach($phase in @('boundary','dual','full','verify')) { Require (-not (Test-Path (Join-Path $root ($phase+'.json')))) "Stop rule violated: $phase executed." }
    $probe=Read (Join-Path $root 'pid-identity-probe.json')
    Require ($probe.Reproduced -and $probe.Original.Pid -eq $probe.Replacement.Pid -and $probe.Original.OwnedHasExited -and $probe.Original.ExitCode -eq 0 -and
        -not $probe.Replacement.LookupHasExitedWhileOriginalWasAlreadyExited -and $probe.Replacement.CompletedCleanup -and $probe.Replacement.ExitCode -eq 0 -and
        $probe.Original.CreatedUtc -cne $probe.Replacement.CreatedUtc -and $probe.Replacement.CreatedUtc -ceq $probe.Replacement.LookupCreatedUtc) 'PID identity control did not reproduce.'
    $processScan=Read (Join-Path $root 'final-process-scan.json')
    Require ($processScan.LiveCount -eq 0 -and $processScan.ReadOnly) 'Owned export process cleanup not confirmed.'
    $priorPath=Join-Path $root 'before/P5O2B24-bounded-finding-contracts-audit.json'
    $prior=Read $priorPath
    $originalFailures=foreach($group in $prior.Tests | Where-Object Failed -gt 0) {
        [ordered]@{Group=$group.Name;Passed=$group.Passed;Total=$group.Total;Failures=$group.Failures;ExpectedOriginalPid=$null;ObservedLookupPid=$null;PidIdentityRecorded=$false}
    }
    $isolatedEvidence=foreach($file in Get-ChildItem (Join-Path $root 'isolated-evidence') -File) {
        $item=Read $file.FullName
        $calls=if($file.Name -like 'p5o2b18-*') { @($item.first,$item.changed,$item.repeated) }
            else { @($item.historical.direct,$item.historical.summary,$item.repeated.direct,$item.repeated.summary,$item.projectControl) }
        Require ($item.ProcessesStopped -and @($calls | Where-Object { -not $_ -or $_.failure }).Count -eq 0) 'Invalid isolated cleanup evidence.'
        [ordered]@{Artifact=(Fingerprint $file.FullName);ProcessIds=@($calls | ForEach-Object { $_.processId });ProcessesStopped=$true;RawExitCodeSerialized=$false}
    }
    $owned=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
    $owned=@($owned | Sort-Object -Unique | Where-Object { $_ -notlike '*P5O2B24-bounded-finding-contracts-audit.json' })
    $artifacts=@(Get-ChildItem $root -File | Sort-Object FullName | ForEach-Object { Fingerprint $_.FullName })
    $audit=[ordered]@{
        Package='P5O2B24';ClosureAuditedUtc=[DateTime]::UtcNow.ToString('o');Head=$before.Head
        State='Functionally complete; validation closure blocked by reproducible existing PID-gate defect'
        EvidenceVerifierPassed=$false;AllRequiredValidationPassed=$false;ArtifactConsistencyPassed=$false
        ArtifactConsistencyStatus='Existing overall gate NOT rerun after stop. Prior true result belongs only to preserved PreviousAudit. ClosureAuditIntegrityPassed checks this addendum only.'
        ClosureAuditIntegrityPassed=$true;BoundaryReexecuted=$false;DualVersionBuildReexecuted=$false;EvidenceGateReexecuted=$false
        OriginalFailedChecks=$originalFailures
        IsolatedRepeat=[ordered]@{Run=$isolated;Evidence=$isolatedEvidence;CodeOrTestChanged=$false}
        BroadRepeat=[ordered]@{StartedUtc=$broad.StartedUtc;FinishedUtc=$broad.FinishedUtc;ExitCode=$broad.ExitCode;Counts=$broad.Counts;Executable=$broad.Executable;Arguments=$broad.Arguments;Failures=$failures;
            OriginalThreeCases=@($broad.Results | Where-Object { $_.Test -like '*IdenticalValidatedPolicyFacts*cover-exact*' -or $_.Test -like '*ProductiveDetector_GenuineAttestedFindings*base*' -or $_.Test -like '*ProductiveDetector_GenuineAttestedFindings*mixed*' });Trx=(Fingerprint (Join-Path $root 'broad.trx'))}
        RootCause=[ordered]@{Classification='Existing test-harness identity/lifetime race: PID-only reacquisition can name a replacement process';Probe=$probe;
            PastFailingPidRecorded=$false;ExactPastReplacementIdentityProven=$false;MechanismDirectlyReproduced=$true;OriginalClientAwaitsOwnedExitAndEof=$true;
            ActualWorkerLeakObserved=$false;Limit='Standalone experiment proves the helper defect, not a retrospective OS trace of an unlogged gate PID.';
            NextPackage='PID-G1: identity-bound cleanup harness and deterministic replacement-identity control; separate approval before implementation'}
        ProcessScan=$processScan;Artifacts=$artifacts;PreservedArtifactCount=@($manifest).Count
        PreviousAuditArtifact=(Fingerprint $priorPath);PreviousAudit=$prior
        Protection=[ordered]@{ComparedInputFiles=@($before.Files).Count;ProductTestBuildFilesUnchanged=$true;TestImageUnchanged=$true;IndexUnchanged=$true;RootIgnoreUnchanged=$true;CoreFileCount=$core.Count;CoreUnchanged=$true;StashCount=@($before.Stashes).Count;StashesUnchanged=$true;HeadUnchanged=$true}
        ChangedThisStep='B24 report/audit and audit-only Proof scripts; generated diagnostic files only under ignored artifacts. No product/test/build changes.'
        SelfAnalysis=$prior.SelfAnalysis;CanonicalFindingDiff=$prior.CanonicalFindingDiff
        OwnedFiles=@($owned | ForEach-Object { Fingerprint (Join-Path $repoRoot $_) });FinalGitStatus=@(& git status --short)
        NoCommit=$true;NoPush=$true;NoStashOperation=$true;NoB25=$true;NoNetwork=$true;NoTestOrProductionChange=$true
    }
    $output=Join-Path $toolRoot 'Evaluation/P5O2B24-bounded-finding-contracts-audit.json'
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host 'Documentary closure audit collected. STOP retained: global validation/evidence gates remain false; no gate was retried or relabelled PASS.'
} finally { Pop-Location }
