$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export = Join-Path $tool 'artifacts/b25a10-development-13'
$output = Join-Path $PSScriptRoot 'BroadFollowup'
if (Test-Path -LiteralPath $output) { throw 'Retain existing attempt/evidence; no overwrite.' }
[xml]$trx = Get-Content (Join-Path $export 'artifacts/a10-closure-results/broad.trx') -Raw -Encoding UTF8
$batches = @('Characterization-approval-batch.md','Production-finding-characterization-batch.md','Final-consolidated-approval-batch.md')
$approval = @($batches | ForEach-Object { Get-Content (Join-Path $PSScriptRoot $_) -Raw -Encoding UTF8 }) -join "`n"
$inventory = @(foreach ($result in $trx.TestRun.Results.UnitTestResult | Where-Object outcome -eq 'Failed') {
    $method = [regex]::Match($result.testName, '\.([^.()]+)(?:\(|$)').Groups[1].Value
    $message = $result.Output.ErrorInfo.Message
    $category = if ($message -match 'DirectoryNotFoundException') { 'MissingIsolatedFixtureArtifact' }
        elseif ($result.testName -match '\.HistoricalWorker(Client|Lifetime)Tests\.') { 'ProcessHarnessAssertion_NotProvedFlaky' }
        elseif ($method -eq 'SharedManifest_MatchesProvenBoundaryAndCurrentPhysicalSources') { 'AdditionalProtectedManifestExpectation' }
        elseif ($method -in @('LowerFactResolverClosure_RemainsIndependentOfAnalyzer','BuilderClosure_HasNoAnalyzerOrSessionBacklink')) { 'ArchitectureRegression_DoNotRelaxAssertions' }
        elseif ($method -eq 'ReducedExtension_RegisteredSupportingSource_UsesUnreducedSourceAndReceiverFact') { 'RawGraphRebindingRegression_DoNotRelaxAssertions' }
        elseif ($method -eq 'PermanentBoundary_MainOwnsGuardAndPolicy_WorkerOnlyExecutesExplicitPurpose') { 'IncompleteSyntaxProductionRegression_DoNotRelaxAssertions' }
        elseif ($method -in @('ExternalDocumentationContract_IsTransitiveOnly','MultipleExternalContracts_ArePropagated','ForeachElementFromOfTypeHelper_DoesNotProduceFinding','GenuineCli_RejectsWithoutFindingsFallbackOrSuccess') -and !$approval.Contains($method)) { 'AdditionalUnlistedSemanticExpectation_ReadOnlyReviewRequired' }
        elseif ($approval.Contains($method)) { 'ExistingBatchMethodNameMatch_ExactCaseAndCertificateStillRequired' }
        else { 'Unmapped_ReadOnlyReviewRequired' }
    [ordered]@{ Test=$result.testName; Method=$method; Category=$category; Message=$message; Stack=$result.Output.ErrorInfo.StackTrace }
})
$counter = [ordered]@{}
foreach ($attribute in $trx.TestRun.ResultSummary.Counters.Attributes) { if ($attribute.Name -ne 'xmlns') { $counter[$attribute.Name]=[int]$attribute.Value } }
$log = Get-Content (Join-Path $tool 'artifacts/p5o2b25a10/runs/development-13-broad.log') -Raw -Encoding UTF8
$clr = $log -match 'Fatal error|Internal CLR error|0x80131506|Failed to create CoreCLR|Process is terminated'
$groups = @($inventory | Group-Object Category | ForEach-Object { [ordered]@{ Category=$_.Name; Count=$_.Count } })
[IO.Directory]::CreateDirectory($output) | Out-Null
$copies = @(
    @((Join-Path $export 'artifacts/a10-closure-results/broad.trx'),'broad.trx'),
    @((Join-Path $tool 'artifacts/p5o2b25a10/runs/development-13-broad.json'),'run.json'),
    @((Join-Path $tool 'artifacts/p5o2b25a10/runs/development-13-broad.log'),'run.log')
)
$evidence = @(foreach ($pair in $copies) {
    $target=Join-Path $output $pair[1]
    [IO.File]::Copy($pair[0],$target,$false)
    [ordered]@{ Name=$pair[1]; Sha256=(Get-FileHash $target).Hash }
})
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o'); Status='STOP_BroadAdditionalArchitectureAndCharacterizationReview'
    Counters=$counter; Categories=$groups; Failures=$inventory; Evidence=$evidence
    ClrCrashSignatureDetected=$clr; R2RemainsTargetedGreen=$true
    HistoricalInputMissing=!(Test-Path (Join-Path $export 'artifacts/p5o2b7/historical-pdb/historical-input.dll'))
    BoundaryTestProcessMissing=!(Test-Path (Join-Path $export 'artifacts/p5o2b4/test-process/bin/Debug/net8.0/BoundaryTestProcess.dll'))
    BroadPreparationIncomplete=$true; MissingFixturesAreHarnessPreparationErrorNotProductEvidence=$true
    MethodNameBatchMatchIsNotExactCasePermissionOrClassificationProof=$true
    FixtureRepairOrGreenRetryPerformed=$false; AdditionalTestAssertionsChanged=$false
    FullBoundaryDualAndFinalGatesExecuted=$false; A10Complete=$false; B25BActivated=$false
}
[IO.File]::WriteAllText((Join-Path $output 'verification.json'),(($record | ConvertTo-Json -Depth 12)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host ($counter | ConvertTo-Json -Compress)
Write-Host ($groups | ConvertTo-Json -Compress)
