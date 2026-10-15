param([Parameter(Mandatory=$true)][ValidateSet('Snapshot','Isolated','Broad','Boundary','Dual','Full','Verify')][string]$Phase)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
$validation=(Get-Content (Join-Path $toolRoot 'artifacts/p5o2b24/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$recordRoot=Join-Path $toolRoot 'artifacts/p5o2b24/pid-gate-audit-20261015'
$utf8=[Text.UTF8Encoding]::new($false)
function Save($Value,[string]$Path) { [IO.File]::WriteAllText($Path,(($Value | ConvertTo-Json -Depth 40)+"`r`n"),$utf8) }
function Require([bool]$Value,[string]$Message) { if(-not $Value) { throw $Message } }
function Hash([string]$Path) { (Get-FileHash -LiteralPath $Path).Hash }
function Read([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function State {
    $paths=@(& git -C $repoRoot ls-files -- Tools/XMLDocNormalizer/src Tools/XMLDocNormalizer/Tests Tools/XMLDocNormalizer/build Tools/XMLDocNormalizer/Evaluation/P5O2B4Proof)+
        @(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer/src Tools/XMLDocNormalizer/Tests Tools/XMLDocNormalizer/build Tools/XMLDocNormalizer/Evaluation/P5O2B4Proof)
    $files=@($paths | Sort-Object -Unique | ForEach-Object { [ordered]@{Path=$_;Sha256=(Hash (Join-Path $repoRoot $_))} })
    [ordered]@{Head=(& git -C $repoRoot rev-parse HEAD);Index=(Hash (Join-Path $repoRoot '.git/index'));
        Ignore=(Hash (Join-Path $repoRoot '.gitignore'));Stashes=@(& git -C $repoRoot reflog show refs/stash --format='%H %gs');Files=$files;
        Core=@(Get-ChildItem (Join-Path $toolRoot 'src/XMLDocNormalizer.ExceptionFlow.Core') -File -Recurse | Sort-Object FullName | ForEach-Object { [ordered]@{Path=$_.FullName;Sha256=(Hash $_.FullName)} });
        TestImage=(Hash (Join-Path $validation 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll'))}
}
function CheckState {
    $before=Read (Join-Path $recordRoot 'before.json'); $now=State
    foreach($key in @('Head','Index','Ignore','Stashes','Files','Core','TestImage')) {
        Require (($before.$key | ConvertTo-Json -Depth 12 -Compress) -ceq ($now[$key] | ConvertTo-Json -Depth 12 -Compress)) "Unchanged-run invariant failed: $key"
    }
}
Push-Location $toolRoot
try {
    if($Phase -eq 'Snapshot') {
        Require (-not (Test-Path $recordRoot)) 'Audit record already exists; do not overwrite attempts.'
        [IO.Directory]::CreateDirectory($recordRoot) | Out-Null
        Save (State) (Join-Path $recordRoot 'before.json')
        $backup=Join-Path $recordRoot 'before'; [IO.Directory]::CreateDirectory($backup) | Out-Null
        foreach($name in @('P5O2B24-bounded-finding-contracts.md','P5O2B24-bounded-finding-contracts-audit.json')) {
            Copy-Item -LiteralPath (Join-Path $toolRoot ('Evaluation/'+$name)) -Destination $backup
        }
        foreach($relative in @('artifacts/dual-version-build','artifacts/p5o2b24/tests','artifacts/p5o2b24/cases')) {
            $destination=Join-Path $backup $relative
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
            Copy-Item -LiteralPath (Join-Path $validation $relative) -Destination $destination -Recurse
        }
        foreach($folder in @('p5o2b18','p5o2b22')) {
            $destination=Join-Path $backup ('artifacts/'+$folder); [IO.Directory]::CreateDirectory($destination) | Out-Null
            Get-ChildItem (Join-Path $validation ('artifacts/'+$folder)) -File -Filter '*.json' | Copy-Item -Destination $destination
        }
        Get-ChildItem (Join-Path $validation 'artifacts/p5o2b24') -File -Filter '*.log' | Copy-Item -Destination $backup
        $manifest=@(Get-ChildItem $backup -File -Recurse | Sort-Object FullName | ForEach-Object { [ordered]@{Path=$_.FullName;Sha256=(Hash $_.FullName)} })
        Save $manifest (Join-Path $recordRoot 'preserved-artifacts.json')
        Write-Host 'Preserved original failed audit, reports, TRX/logs, cases and source/protection fingerprints.'
        return
    }
    CheckState
    $record=Join-Path $recordRoot ($Phase.ToLowerInvariant()+'.json')
    Require (-not (Test-Path $record)) 'This explicit phase has already been invoked; do not retry until green.'
    $prerequisites=switch($Phase) {
        'Broad' { @('isolated') }; 'Boundary' { @('isolated','broad') }
        'Dual' { @('isolated','broad','boundary') }; 'Full' { @('dual') }; 'Verify' { @('dual','full') }; default { @() }
    }
    foreach($previous in $prerequisites) { Require ((Read (Join-Path $recordRoot ($previous+'.json'))).ExitCode -eq 0) "Stop rule: $previous failed." }
    $started=[DateTime]::UtcNow.ToString('o')
    Save ([ordered]@{Phase=$Phase;StartedUtc=$started;ExitCode=$null;State='Running or interrupted'}) $record
    $project=Join-Path $validation 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
    $trx=$null
    switch($Phase) {
        'Isolated' {
            $filter='(FullyQualifiedName=XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalDeclaredReadinessTests.IdenticalValidatedPolicyFacts_DoNotDetermineFinalFindings&DisplayName~cover-exact)|(FullyQualifiedName=XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat&DisplayName~base)|(FullyQualifiedName=XMLDocNormalizerTests.Worker.ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat&DisplayName~mixed)'
            $exe='dotnet'; $arguments=@('test',$project,'--no-build','--no-restore','--filter',$filter,'--logger','trx;LogFileName=isolated.trx','--results-directory',$recordRoot)
            $trx=Join-Path $recordRoot 'isolated.trx'; $expected=3
        }
        'Broad' {
            $exe='powershell'; $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Run-Regressions.ps1'),'-ValidationRoot',$validation,'-RepeatGroups','broad')
            $trx=Join-Path $validation 'artifacts/p5o2b24/tests/broad.trx'; $expected=3167
        }
        'Boundary' {
            $line=Get-Content (Join-Path $validation 'build/Verify-DualVersionBuild.ps1') -Encoding UTF8 | Where-Object { $_ -match "Invoke-DotNet 'main-boundary-tests' " }
            Require ($line -match "'--filter', '([^']+)'") 'Original boundary filter not found.'
            $filter=$Matches[1]; $gate=Join-Path $validation 'artifacts/dual-version-build'
            $exe='dotnet'; $arguments=@('test',$project,'--no-build','--no-restore','-p:Configuration=Debug','--filter',$filter,'--logger','trx;LogFileName=main-boundary.trx','--results-directory',$gate)
            $trx=Join-Path $gate 'main-boundary.trx'; $expected=578
        }
        'Dual' { $exe='powershell'; $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $validation 'build/Verify-DualVersionBuild.ps1'),'-RunMainBoundaryTests','-BuildTestProjectReferences') }
        'Full' { $exe='powershell'; $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Run-Regressions.ps1'),'-ValidationRoot',$validation,'-RepeatGroups','full'); $trx=Join-Path $validation 'artifacts/p5o2b24/tests/full.trx'; $expected=3258 }
        'Verify' { $exe='powershell'; $arguments=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Write-Verify-Findings.ps1'),'-ValidationRoot',$validation) }
    }
    Save ([ordered]@{Phase=$Phase;StartedUtc=$started;ExitCode=$null;State='Running or interrupted';Executable=$exe;Arguments=$arguments}) $record
    $previousPreference=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& $exe @arguments 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previousPreference }
    $logPath=Join-Path $recordRoot ($Phase.ToLowerInvariant()+'.log')
    [IO.File]::WriteAllText($logPath,$log,$utf8); Write-Host $log
    $counts=$null; $results=$null; $trxHash=$null
    if($trx -and (Test-Path $trx)) {
        $copy=Join-Path $recordRoot ($Phase.ToLowerInvariant()+'.trx')
        if([IO.Path]::GetFullPath($copy) -cne [IO.Path]::GetFullPath($trx)) { Copy-Item -LiteralPath $trx -Destination $copy }
        [xml]$xml=Get-Content -LiteralPath $trx -Raw -Encoding UTF8
        $node=$xml.SelectSingleNode('//*[local-name()="Counters"]')
        $counts=[ordered]@{Total=[int]$node.total;Passed=[int]$node.passed;Failed=[int]$node.failed;Skipped=[int]$node.notExecuted}
        $results=@($xml.SelectNodes('//*[local-name()="UnitTestResult"]') | ForEach-Object { [ordered]@{Test=$_.testName;Outcome=$_.outcome;Start=$_.startTime;End=$_.endTime;Duration=$_.duration;Output=$_.Output.InnerXml} })
        $trxHash=Hash $trx
        if($code -eq 0 -and ($counts.Total -ne $expected -or $counts.Passed -ne $expected -or $counts.Skipped -ne 0)) { $code=1 }
    }
    CheckState
    Save ([ordered]@{Phase=$Phase;StartedUtc=$started;FinishedUtc=[DateTime]::UtcNow.ToString('o');ExitCode=$code;Executable=$exe;Arguments=$arguments;Counts=$counts;TrxSha256=$trxHash;Results=$results;LogSha256=(Hash $logPath);UnchangedInputs=$true}) $record
    if($code -ne 0) { throw "STOP: $Phase failed; attempt retained. No retry, repair or follow-on gates authorized by this script." }
} finally { Pop-Location }
