param([ValidateSet('Start','Final')][string]$Phase = 'Start')
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
function ReadGit([string[]]$arguments) {
    $output = @(& git.exe --no-optional-locks -C $repo @arguments)
    if ($LASTEXITCODE -ne 0) { throw ('Read-only Git command failed: ' + ($arguments -join ' ')) }
    return $output
}
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
function WriteNew([string]$name,$value) {
    $path = Join-Path $PSScriptRoot $name
    if (Test-Path -LiteralPath $path) { throw ('Preserve previous evidence: ' + $path) }
    [IO.File]::WriteAllText($path,(($value | ConvertTo-Json -Depth 15)+"`r`n"),[Text.UTF8Encoding]::new($false))
}
$head = @(ReadGit @('rev-parse','HEAD'))[0]
$index = Hash (Join-Path $repo '.git/index')
$ignore = Hash (Join-Path $repo '.gitignore')
$stashes = @(ReadGit @('stash','list','--format=%H %gs'))
$staged = @(ReadGit @('diff','--cached','--name-only'))
$status = @(ReadGit @('status','--short'))
if ($staged.Count -ne 0) { throw 'Index is not clean; no repair authorized.' }
if ($Phase -eq 'Final') {
    $start = Get-Content (Join-Path $PSScriptRoot 'start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $differences = @(foreach ($file in @($start.CodeAndTestFiles)+@($start.HistoricalEvaluationFiles)) {
        if ((Hash (Join-Path $repo $file.Path)) -cne $file.Sha256) { $file.Path }
    })
    $coreDifferences = @(foreach ($file in $start.Core) { if ((Hash $file.Path) -cne $file.Sha256) { $file.Path } })
    if ($head -cne $start.Head -or $index -cne $start.IndexSha256 -or $ignore -cne $start.RootIgnoreSha256 -or
        $differences.Count -ne 0 -or $coreDifferences.Count -ne 0 -or ($stashes -join "`n") -cne ($start.Stashes -join "`n")) {
        throw 'Protected input/source/evidence drift: STOP, do not repair.'
    }
    WriteNew 'verification.json' ([ordered]@{
        CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=$head;IndexSha256=$index;RootIgnoreSha256=$ignore
        IndexUnchanged=$true;SemanticIndexClean=$true;RootIgnoreUnchanged=$true;SixStashesUnchanged=$true
        CoreChecked=$start.Core.Count;NewCoreDifferences=$coreDifferences
        HistoricalEvaluationFilesChecked=$start.HistoricalEvaluationFiles.Count;CodeAndTestFilesChecked=$start.CodeAndTestFiles.Count;Differences=$differences
        GitStatusShort=$status;ProductionChanges=0;ExistingTestChanges=0;NewTests=0;Builds=0;TestRuns=0
        Status='STOP_AwaitingOneConsolidatedCharacterizationApproval';B25BActivated=$false;WireDtoChanged=$false;WorkerProfileChanged=$false
        Commit=$false;Push=$false;StashOperations=$false;HistoryRewrite=$false
    })
    Write-Host 'PASS: unchanged code/tests/history/Core/index/ignore/stashes; preflight-only STOP.'
    exit 0
}
$prior = Get-Content (Join-Path $tool 'Evaluation/P5O2B25BReadiness/repository-start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$core = @(foreach ($file in $prior.Core) { [ordered]@{Path=$file.Path;Sha256=(Hash $file.Path);ReadinessSha256=$file.ActualSha256} })
$code = @(foreach ($path in ReadGit @('ls-files','--','Tools/XMLDocNormalizer/src','Tools/XMLDocNormalizer/Tests','Tools/XMLDocNormalizer/build')) {
    if ($path -notmatch '(^|/)(bin|obj)/') { [ordered]@{Path=$path;Sha256=(Hash (Join-Path $repo $path))} }
})
$history = @(foreach ($path in ReadGit @('ls-files','--','Tools/XMLDocNormalizer/Evaluation')) {
    [ordered]@{Path=$path;Sha256=(Hash (Join-Path $repo $path))}
})
$tests = @(foreach ($file in $code | Where-Object {$_.Path -match '/Tests/.*\.cs$'}) {
    $text = [IO.File]::ReadAllText((Join-Path $repo $file.Path))
    $hits = @([regex]::Matches($text,'(?m)^.*(?:FindExternalExceptionSmellsAsync|TryAdmitFindingEvidence|TryGetFindingFacts|Solution is not admitted|AssertNoFindingAdmission|OwnershipSnapshot|HistoricalWorkerClient\.cs|ExternalFindings\.cs|ExternalTargetExecution\.cs|HistoricalWorkerPayloadProjection\.cs|WorkerProtocol\.cs).*$') | ForEach-Object {
        [ordered]@{Line=1+([regex]::Matches($text.Substring(0,$_.Index),"`n")).Count;Text=$_.Value.Trim()}
    })
    [ordered]@{Path=$file.Path;Sha256=$file.Sha256;WorkerTestFile=$file.Path -match '/Worker/';BoundaryBindings=$hits;Assessment=if($file.Path -match '(ExceptionFlowHistoricalSolutionFindingTests|ExceptionFlowAssemblyQualifiedOwnershipTests)\.cs$'){'CONSOLIDATED_BATCH_ONLY'}else{'UNCHANGED_CONTRACT'}}
})
$solutionFile = 'Tools/XMLDocNormalizer/Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionFindingTests.cs'
$text = [IO.File]::ReadAllText((Join-Path $repo $solutionFile))
$method = 'ExistingB15FlowGraphAndCurrentFindings_AreNotFinalHistoricalFindingAdmission'
$cases = @([regex]::Matches($text.Substring(0,$text.IndexOf('public async Task '+$method)),'\[InlineData\((\d+), "([^"]+)"\)\]') | ForEach-Object {
    [ordered]@{Count=[int]$_.Groups[1].Value;Scenario=$_.Groups[2].Value}
})
if ($cases.Count -ne 15) { throw 'Unexpected B25 case set; inspect instead of assuming the old batch.' }
$assertions = @(
    [ordered]@{Id='B25B-C1';Path=$solutionFile;Method='AssertNoFindingAdmission';Old='Assert.False(result.Succeeded); Assert.Empty(result.Findings); Assert.Null(result.Direct.Dispatch); Assert.Null(result.Summary);'},
    [ordered]@{Id='B25B-C2';Path=$solutionFile;Method='StopRule_DoesNotIntroducePolicyRoutingAttestationOrPidWorkarounds';Old='Assert.Contains("Solution is not admitted", external); Assert.DoesNotContain("SupportingProjects", external);'},
    [ordered]@{Id='B25B-C3a';Path='Tools/XMLDocNormalizer/Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs';Method='StopRule_LeavesOriginalProductionAndClosedFindingAdmission';Old='Assert.Contains("Solution is not admitted", external);'},
    [ordered]@{Id='B25B-C3b';Path='Tools/XMLDocNormalizer/Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAssemblyQualifiedOwnershipTests.cs';Method='StopRule_LeavesOriginalProductionAndClosedFindingAdmission';Old='Assert.DoesNotContain("SupportingProjects", external);'}
)
foreach ($assertion in $assertions) {
    $source = [IO.File]::ReadAllText((Join-Path $repo $assertion.Path))
    $span=$source.IndexOf($assertion.Old,[StringComparison]::Ordinal)
    if ($span -lt 0 -or $source.IndexOf($assertion.Old,$span+1,[StringComparison]::Ordinal) -ge 0) { throw ('Assertion not unique: ' + $assertion.Id) }
    $assertion.Line=1+([regex]::Matches($source.Substring(0,$span),"`n")).Count
    $assertion.SpanStart=$span;$assertion.SourceSha256=Hash (Join-Path $repo $assertion.Path)
}
WriteNew 'start.json' ([ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=$head;Parent=@(ReadGit @('rev-parse','HEAD^'))[0];Branch=@(ReadGit @('branch','--show-current'))[0]
    IndexSha256=$index;RootIgnoreSha256=$ignore;Stashes=$stashes;StagedPaths=$staged;InitialGitStatusShort=$status
    Core=$core;CodeAndTestFiles=$code;HistoricalEvaluationFiles=$history
    CoreChangedSinceReadiness=@($core | Where-Object {$_.Sha256 -cne $_.ReadinessSha256})
    HistoricalProtectionManifestChanged=$false;NoBaselineRewritten=$true
})
WriteNew 'characterization-inventory.json' ([ordered]@{
    Head=$head;TestFileInventory=$tests;TestFiles=$tests.Count;WorkerFiles=@($tests | Where-Object WorkerTestFile).Count
    SourceBindingFiles=@($tests | Where-Object {$_.BoundaryBindings.Count -gt 0}).Count
    ExistingB25Theory=$method;ExistingB25Cases=$cases;AdditionalOwnerFact='AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted'
    Assertions=$assertions;ApprovalGroups=3;AffectedExistingTestCases=18
    Scope='Complete source/call/assertion preflight of directly affected B24/B25/Solution/Historical contracts plus all test-file and source-guard census; not a new runtime replay of the entire Full suite.'
    ExistingCliIncompleteJobsRemainNegative=$true;ProductionChanged=$false;TestsChanged=$false;TestsExecuted=0;Builds=0
})
Write-Host ('Captured preflight: '+$tests.Count+' test files, '+$cases.Count+' B25 parameter cases plus owner/guards; no implementation.')
