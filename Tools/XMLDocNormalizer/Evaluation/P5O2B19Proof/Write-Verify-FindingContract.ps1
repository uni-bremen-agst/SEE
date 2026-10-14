param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Push-Location $toolRoot
try {
    function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
    function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
    function Equal($Left,$Right,[string]$Message) { Require ((Json $Left) -ceq (Json $Right)) $Message }
    function Fingerprint([string]$Path) { [ordered]@{Path=$Path;Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
    function Tests([string]$Name,[string]$Path,[int]$Expected,[bool]$Fresh=$true) {
        [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
        $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
        Require ($trx.SelectSingleNode('//*[local-name()="ResultSummary"]').outcome -eq 'Completed') "Incomplete/failed: $Name"
        Require ($counts.total -eq "$Expected" -and $counts.passed -eq "$Expected" -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Census/failure/skips: $Name"
        if($Fresh) { Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale: $Name" }
        [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
    }
    $initial=ReadJson 'artifacts/p5o2b19/protected-before.json'; $export=ReadJson 'artifacts/p5o2b19/export.json'
    $validation=[IO.Path]::GetFullPath($ValidationRoot)
    Require ($validation -ceq [IO.Path]::GetFullPath($export.ToolRoot) -and $export.NoInitialArtifacts -and $export.NoInitialBinObj) 'Clean recorded HEAD archive required.'
    Require ((Get-FileHash artifacts/p5o2b19/head.tar).Hash -ceq $export.ArchiveSha256) 'Archive changed.'
    Require ((git rev-parse HEAD) -ceq $initial.Head -and (git branch --show-current) -ceq $initial.Branch) 'External HEAD movement: audit before continuing.'
    Require ((Get-FileHash ../../.gitignore).Hash -ceq $initial.RootIgnore) 'Protected ignore changed.'
    Equal @(git stash list --format='%H %s') $initial.Stashes 'Stashes changed.'
    Equal @(git diff --cached --name-only) $initial.Index 'Index changed.'
    [string[]]$built=ReadJson 'artifacts/p5o2b19/owned-build-inputs.json'
    Require ($built.Count -eq 13) 'Thirteen exact production/test/gate build inputs required.'
    $allowed=@($built | ForEach-Object {[IO.Path]::GetFullPath($_)})+@([IO.Path]::GetFullPath('Evaluation/OPEN-PIPELINE-BOUNDARIES.md'))
    foreach($file in $initial.TrackedFiles) { if($allowed -notcontains $file.Path) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Unrelated/shared/Current/B5-B18 source changed: $($file.Path)" } }
    Require ($initial.TrackedFiles.Count -eq 914 -and $initial.CoreFiles.Count -eq 23 -and $initial.Stashes.Count -eq 6) 'Unexpected protected inventory.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq 23) 'Core inventory changed.'
    foreach($file in $initial.CoreFiles) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) 'Core byte change.' }
    foreach($file in $built) { Require ((Get-FileHash $file).Hash -ceq (Get-FileHash (Join-Path $validation $file)).Hash) "Final built overlay mismatch: $file" }
    $gateRoot=Join-Path $validation 'artifacts/dual-version-build'; $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parent=ReadJson (Join-Path $export.ExportRoot 'parent-dual/audit.json')
    Require ($gate.Passed -and $parent.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual/runtime/boundary incomplete.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1 -and $gate.BuildImages.Count -eq 6 -and $gate.WorkerBuildImages.Count -eq 2) 'Analyzer build boundary changed.'
    Equal $gate.SharedSourceFingerprints $parent.SharedSourceFingerprints 'Shared analyzer sources changed.'
    foreach($image in @($gate.BuildImages | Where-Object Owner -eq 'historical')) {
        $before=@($parent.BuildImages | Where-Object {$_.Step -ceq $image.Step})
        Require ($before.Count -eq 1 -and $image.Image.Sha256 -ceq $before[0].Image.Sha256 -and $image.Image.PdbSha256 -ceq $before[0].Image.PdbSha256) 'Historical analyzer DLL/PDB changed.'
    }
    $logs=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed gate step: $($step.Name)"
        if($step.Name.EndsWith('-build')) {
            $path=Join-Path $gateRoot ($step.Name+'.log'); $value=Get-Content $path -Raw -Encoding UTF8
            Require ($value -match '0 Warnung\(en\)' -and $value -match '0 Fehler') "Not clean WAE: $($step.Name)"
            Fingerprint $path
        }
    }
    Require (@($logs).Count -eq 11) 'Eleven WAE builds required.'
    $expected=[ordered]@{b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=681;focused=706;broad=3007;full=3098}
    $census=foreach($name in $expected.Keys) { Tests $name (Join-Path $validation ('artifacts/p5o2b19/tests/'+$name+'.trx')) $expected[$name] }
    $census+=@(Tests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 496)
    $census+=@(Tests 'parent-boundary' (Join-Path $export.ExportRoot 'parent-dual/main-boundary.trx') 457 $false)
    $baselines=foreach($file in Get-ChildItem (Join-Path $export.ExportRoot 'parent-current') -File -Filter '*.json') {
        $after=Join-Path $validation ('artifacts/p5o2b9/current/'+$file.Name)
        Require ((Get-FileHash $file.FullName).Hash -ceq (Get-FileHash $after).Hash) "Current baseline changed: $($file.Name)"
        [ordered]@{Name=$file.Name;Before=(Fingerprint $file.FullName);After=(Fingerprint $after);ByteEqual=$true}
    }
    Require (@($baselines).Count -eq 8) 'Eight complete native Current baselines required.'
    $cases=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b19') -File -Filter 'case-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.CompleteMemberContractParity -and $value.GuardInputEquivalent -and $value.GuardExecutionDeferred -and $value.DeclaredStillUnsupported -and
            $value.NoFindingImport -and $value.CloneRejected -and $value.ImmutableReceipt -and $value.NoFallback -and $value.ProcessesStopped -and $value.NoHistoricalLoadedInMain) 'Contract/activation/isolation boundary wrong.'
        Equal $value.NativeDocument $value.HistoricalDocument 'Full bound document facts differ.'
        Equal $value.HistoricalDocument $value.RepeatedDocument 'Complete document serialization differs on repeat.'
        Equal $value.Native.Summary $value.HistoricalSummary 'Complete canonical summary/flow/source parity differs.'
        Equal $value.Native.Direct $value.HistoricalPolicy.DirectResult 'Independent Direct differs.'
        Equal $value.Native.Declarations $value.HistoricalPolicy.Declarations 'Complete declaration/ownership differs.'
        Require ($value.Native.HasDeclaredTypes -eq ($value.HistoricalPolicy.Declarations.Count -ne 0)) 'Guard input is not actual complete declaration evidence.'
        Require ($value.HistoricalDocument.Profile -ceq 'single-root-declared-documentation-v1' -and
            $value.HistoricalDocument.Mode -ceq 'projectTransitiveDeclaredExceptions' -and $value.Provenance.AnalysisMode -ceq 'project-transitive') 'Wrong mode/profile or engine alias.'
        Equal $value.HistoricalDocument.TypeDisplays $value.Contract.TypeDisplays 'Immutable type names differ.'
        Require ($value.Contract.HasDocumentation -eq $value.HistoricalDocument.Syntax.HasDocumentation -and
            $value.Contract.HasInheritdoc -eq $value.HistoricalDocument.Syntax.HasInheritdoc -and
            $value.Contract.Member.Position -eq $value.HistoricalDocument.Syntax.Anchor -and
            $value.Contract.Tags.Count -eq $value.HistoricalDocument.Bindings.Count) 'Incomplete document/target facts.'
        for($index=0;$index -lt $value.Contract.Tags.Count;$index++) {
            $tag=$value.Contract.Tags[$index]; $binding=$value.HistoricalDocument.Bindings[$index]
            Equal $tag.Syntax $binding.Syntax 'Immutable tag syntax differs.'
            Equal $tag.Type $binding.Type 'Immutable bound type differs.'
            Require ($tag.Kind -ceq $binding.Kind -and $tag.IsException -eq $binding.IsException -and
                $tag.Anchor.LogicalPath -ceq $value.HistoricalDocument.Syntax.LogicalPath -and $tag.Anchor.Line -gt 0 -and $tag.Anchor.Column -gt 0) 'Incomplete tag ownership/binding/anchor.'
        }
        foreach($decision in $value.Decisions) { Require ($null -eq $decision.Decision.Failure -and $decision.Expected.Relevant -eq $decision.Decision.Decision.Relevant -and
            $value.Native.HasDeclaredTypes -eq $decision.Decision.Decision.SummaryEnabled) 'Existing Main policy/guard input differs.' }
        foreach($finding in $value.Native.DeclaredFindings) {
            if($finding.Smell.Id -in @('DOC610','DOC611')) {
                Require ($finding.Line -eq $value.Contract.Member.Line -and $finding.Column -eq $value.Contract.Member.Column -and $finding.Context.OwnerKind -ceq 'Method') 'Full native flow Finding target not represented.'
                foreach($property in @('OwnerKind','SubjectKind','Accessibility','SymbolName','ContainingType','ContainingNamespace','ProjectName','IsGenerated','IsTestFile')) {
                    Equal $value.Contract.Member.Context.$property $finding.Context.$property "Full native flow Finding context not represented: $property"
                }
                Require (@($value.Contract.TypeDisplays | Where-Object {$_.DisplayName -ceq $finding.Context.TargetName}).Count -eq 1) 'Full native flow Finding type name not represented.'
            } else {
                $tags=@($value.Contract.Tags | Where-Object {$_.Anchor.Line -eq $finding.Line -and $_.Anchor.Column -eq $finding.Column -and $_.Snippet -ceq $finding.Snippet})
                Require ($tags.Count -eq 1) 'Full native tag Finding anchor/snippet not represented.'
                Equal $tags[0].Anchor.Context $finding.Context 'Full native tag Finding context not represented.'
            }
        }
        foreach($compiler in $value.Identity.LoadedRoslyn) { Require ($gate.HistoricalCompilerReferences.Sha256 -contains $compiler.Sha256) 'Not genuine pinned Historical compiler.' }
        Require ($value.FirstProcess -gt 0 -and $value.SecondProcess -gt 0 -and $value.Identity.LoadedRoslyn.Count -eq 2) 'Missing actual process/runtime evidence.'
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($cases).Count -eq 16) 'Sixteen complete member-contract differentials required.'
    $negatives=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b19') -File -Filter 'negative-*.json') {
        $value=ReadJson $file.FullName
        Require ($value.Failure.Code -ceq 'malformedResponse' -and $value.NoContractReceipt -and $value.NoResult -and $value.NoFallback -and $value.ProcessesStopped) 'Malformed contract not fail closed.'
        [ordered]@{Value=$value;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($negatives).Count -eq 22) 'Twenty-two missing/conflicting contract negatives required.'
    $admission=ReadJson (Join-Path $validation 'artifacts/p5o2b19/admission.json')
    Require ($admission.CallerFactsRejected -and $admission.InvalidDefaultModeRejected -and $admission.ClonedPayloadRejected -and $admission.LegacyCannotMintContract -and
        $admission.GuardExecutionDeferred -and $admission.DeclaredStillUnsupported -and $admission.NoFallback -and $admission.ProcessesStopped -and
        $admission.GuardFailure.WorkerFailure.Code -ceq 'analysisFailure' -and -not $admission.GuardNative.HasDeclaredTypes -and $admission.GuardNative.DeclaredFindings.Count -eq 0) 'Original admission/guard failure is not preserved.'
    $processes=@(Get-CimInstance Win32_Process | Where-Object {$_.CommandLine -and $_.CommandLine.Contains($validation) -and
        $_.CommandLine -match '(XMLDocNormalizer.HistoricalWorker.dll|HistoricalPdbFixture.dll|BoundaryTestProcess.dll)'})
    Require ($processes.Count -eq 0) 'Own isolated child still alive.'
    $owned=@($built)+@('Evaluation/OPEN-PIPELINE-BOUNDARIES.md','Evaluation/P5O2B19-historical-declared-finding-contract.md',
        'Evaluation/P5O2B19Proof/Run-Regressions.ps1','Evaluation/P5O2B19Proof/Write-Verify-FindingContract.ps1')
    $encoding=foreach($file in $owned) {
        $bytes=[IO.File]::ReadAllBytes([IO.Path]::GetFullPath($file)); $content=[Text.UTF8Encoding]::new($false,$true).GetString($bytes)
        Require ($content.Length -gt 0 -and $content[0] -ne [char]0xFEFF -and $content.EndsWith("`r`n") -and $content -notmatch '(?<!\r)\n') "Not UTF8/noBOM/CRLF: $file"
        if($file.EndsWith('.ps1')) {
            $tokens=$null; $errors=$null; [Management.Automation.Language.Parser]::ParseFile([IO.Path]::GetFullPath($file),[ref]$tokens,[ref]$errors) | Out-Null
            Require ($errors.Count -eq 0) "Invalid PS syntax: $file"
        }
        Fingerprint $file
    }
    $collisions=@(git -C D:/Repository/SEE ls-files | Group-Object {([string]$_).ToLowerInvariant()} | Where-Object Count -gt 1)
    Require ($collisions.Count -eq 0) 'Git path case collisions.'
    [string[]]$cs=@($built | Where-Object {$_ -like '*.cs'})
    & dotnet format whitespace . --folder --include $cs --verify-no-changes
    Require ($LASTEXITCODE -eq 0) 'Owned C# format failed.'
    & git diff --check
    Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $output='Evaluation/P5O2B19-historical-declared-finding-contract-audit.json'
    if(-not (Test-Path $output)) { [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),"{}`r`n",[Text.UTF8Encoding]::new($false)) }
    $attemptFiles=foreach($file in Get-ChildItem (Join-Path $validation 'artifacts/p5o2b19/attempts') -Recurse -File) { Fingerprint $file.FullName }
    $audit=[ordered]@{
        Package='P5O2B19';VerifiedUtc=[DateTime]::UtcNow.ToString('o');EvidenceVerifierPassed=$true
        Closure='Complete bounded selected-root member Finding inputs; productive guard/adoption deferred to B20'
        FindingContractCompleteForProfile=$true;GeneralHistoricalReportingImplemented=$false;HistoricalDeclaredActivated=$false;FullProductionFindingParityProven=$false
        GuardInputsEquivalent=$true;ProductiveSummaryGuardClosed=$false;B20='Establish declaration/doc skip facts before Summary; adopt complete contract at existing Main Finding boundary without duplicated policy or alias'
        Protocol=[ordered]@{OuterVersion=2;DocumentationProfile='single-root-declared-documentation-v1';LegacyNullOmitted=$true;B17EvidenceDtoAndWorkerFactEmitterUnchanged=$true;MaximumTags=128;ResponseLimit=1048576}
        Trust='Original P4/P5 receipts + original projected-payload weak origin + pinned compiler binding facts + exact syntax/cardinality/root/results provenance + retained bound PE type checks. No independent compiler re-execution or cryptographic semantic-proof claim.'
        Scope='Existing complete/proven bounded single project; one source-owned static nongeneric parameterless root; nongeneric named document types attestable in original retained PE symbol world. No general/uncertain/generic/out-of-world document/reference admission.'
        NoAlias=$true;NoCurrentFallback=$true;NoWorkerFindingPolicy=$true;NoSecondPePdbReferenceSourceValidation=$true
        Head=$initial.Head;FinalHead=(git rev-parse HEAD);ExternalHeadMovement=$false;Branch=$initial.Branch
        Protection=[ordered]@{TrackedFiles=914;CoreFiles=23;Stashes=6;RootIgnoreSha256=$initial.RootIgnore;IndexUnchanged=$true;AllUnrelatedBytesUnchanged=$true}
        Export=$export;ProtectedSnapshot=(Fingerprint 'artifacts/p5o2b19/protected-before.json');OwnedFiles=@($encoding)
        Tests=@($census);WaeBuilds=@($logs);DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'));DualGatePassed=$true
        CurrentBaselines=@($baselines);SharedAnalyzerSources=121;HistoricalAnalyzerDllPdbByteEqual=$true;MainWorkerImages='Legitimately changed for opt-in protocol/fact/context support; Current native behavior unchanged'
        Cases=@($cases);Negatives=@($negatives);Admission=$admission
        Isolation=[ordered]@{OwnLiveChildren=0;PinnedWorkerOnly=$true;ActualWindowsExecution=$true;LinuxRemoteExecution=$false;NoCrossVersionRoslynInProtocol=$true}
        Attempts=(ReadJson 'artifacts/p5o2b19/attempts.json');AttemptFiles=@($attemptFiles)
        SelfAnalysis=[ordered]@{Findings=16;Fresh=$false;Origin='Inherited B18/B17 baseline'}
        CanonicalFindingDiff=[ordered]@{Added=0;Removed=0;ChangedEvidence=0;Fresh=$false;Origin='Inherited baseline'}
        FinalGitStatus=@(git status --short);NoCommit=$true;NoPush=$true
    }
    $json=(ConvertTo-Json -InputObject $audit -Depth 100) -replace "`r?`n","`r`n"
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($output),($json+"`r`n"),[Text.UTF8Encoding]::new($false))
    $parsed=ReadJson $output
    Require ($parsed.EvidenceVerifierPassed -and $parsed.FindingContractCompleteForProfile -and -not $parsed.HistoricalDeclaredActivated -and -not $parsed.ProductiveSummaryGuardClosed) 'Invalid closure JSON.'
    Write-Host 'PASS: B19 complete bounded member contract, validated evidence, fresh regressions/byte baselines/protection/isolation; guard and activation remain B20.'
} finally { Pop-Location }
