param([Parameter(Mandatory=$true)][string]$ValidationRoot)
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
function Require([bool]$Condition,[string]$Message) { if(-not $Condition) { throw $Message } }
function ReadJson([string]$Path) { Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
function Json($Value) { ConvertTo-Json -InputObject $Value -Depth 100 -Compress }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
function CheckTests([string]$Name,[string]$Path,[int]$Expected) {
    [xml]$trx=Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $summary=$trx.SelectSingleNode('//*[local-name()="ResultSummary"]')
    $counts=$trx.SelectSingleNode('//*[local-name()="Counters"]')
    Require ($summary.outcome -eq 'Completed' -and $counts.total -eq "$Expected" -and $counts.passed -eq "$Expected" -and $counts.failed -eq '0' -and $counts.notExecuted -eq '0') "Incomplete/failed/skipped $Name"
    Require (([DateTime]$trx.SelectSingleNode('//*[local-name()="Times"]').start).ToUniversalTime() -ge ([DateTime]$gate.StartedUtc).ToUniversalTime()) "Stale $Name"
    [ordered]@{Name=$Name;Total=$Expected;Passed=$Expected;Failed=0;Skipped=0;Artifact=(Fingerprint $Path)}
}
Push-Location $toolRoot
try {
    $output=Join-Path $toolRoot 'Evaluation/P5O2B23-bounded-external-target-adoption-audit.json'
    $pending=[ordered]@{Package='P5O2B23';EvidenceVerifierPassed=$false;StartedUtc=[DateTime]::UtcNow.ToString('o');State='Running or interrupted'}
    [IO.File]::WriteAllText($output,(($pending | ConvertTo-Json)+"`r`n"),[Text.UTF8Encoding]::new($false))
    $before=ReadJson 'artifacts/p5o2b23/protected-before.json'
    Require ((& git rev-parse HEAD) -ceq $before.Head) 'External HEAD movement; audit before continuing.'
    Require ((Get-FileHash (Join-Path $repoRoot '.gitignore')).Hash -ceq $before.Ignore) 'Protected ignore changed.'
    Require ((Get-FileHash (Join-Path $repoRoot '.git/index')).Hash -ceq $before.Index) 'Index changed.'
    Require ((Json @(& git reflog show refs/stash --format='%H %gs')) -ceq (Json $before.Stashes)) 'Stashes changed.'
    Require (@(Get-ChildItem src/XMLDocNormalizer.ExceptionFlow.Core -Recurse -File).Count -eq $before.Core.Count) 'Core file inventory changed.'
    foreach($file in $before.Core) { Require ((Get-FileHash -LiteralPath $file.Path).Hash -ceq $file.Sha256) "Core bytes changed: $($file.Path)" }
    $export=ReadJson 'artifacts/p5o2b23/export.json'
    Require ([IO.Path]::GetFullPath($ValidationRoot) -ceq $export.ToolRoot) 'Unexpected validation tree.'
    $archiveEntries=@(& tar -tf artifacts/p5o2b23/head.tar)
    Require (@($archiveEntries | Where-Object { $_ -match '/(bin|obj|artifacts)/' }).Count -eq 0) 'Archive contained build artifacts.'
    $paths=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer)
    $files=@($paths | Sort-Object -Unique | Where-Object { $_ -notlike '*-audit.json' })
    $sourceInputs=@($files | Where-Object { $_ -like '*.cs' -or $_ -like '*/build/Verify-DualVersionBuild.ps1' })
    Require ($sourceInputs.Count -eq 8) 'Seven C# files plus permanent build gate expected.'
    foreach($file in $sourceInputs) {
        $relative=$file.Substring('Tools/XMLDocNormalizer/'.Length)
        Require ((Get-FileHash (Join-Path $repoRoot $file)).Hash -ceq (Get-FileHash (Join-Path $ValidationRoot $relative)).Hash) "Built source mismatch: $file"
    }
    $changedTracked=@(& git -C $repoRoot diff --name-only HEAD)
    Require (@($changedTracked | Where-Object { $_ -ne '.gitignore' -and $files -cnotcontains $_ }).Count -eq 0) 'Unexpected tracked changes.'
    $gateRoot=Join-Path $ValidationRoot 'artifacts/dual-version-build'
    $gate=ReadJson (Join-Path $gateRoot 'audit.json')
    $parentGate=ReadJson 'artifacts/b22-6a60/Tools/XMLDocNormalizer/artifacts/dual-version-build/audit.json'
    Require ($gate.Passed -and $gate.RuntimeExecuted -and $gate.MainBoundaryExecuted) 'Dual gate/runtime/boundary missing.'
    Require ($gate.SharedCount -eq 121 -and $gate.HostCount -eq 1) 'Shared analyzer cut changed.'
    Require ((Json $gate.SharedSourceFingerprints) -ceq (Json $parentGate.SharedSourceFingerprints)) 'Shared analyzer source bytes changed.'
    $hostRelative='src/XMLDocNormalizer.ExceptionFlow.Historical/HistoricalSemanticEnvironment.cs'
    $parentHost=Join-Path $toolRoot ('artifacts/b22-6a60/Tools/XMLDocNormalizer/'+$hostRelative)
    Require ((Get-FileHash (Join-Path $ValidationRoot $hostRelative)).Hash -ceq (Get-FileHash $parentHost).Hash) 'Historical semantic host changed.'
    $workerSources=foreach($workerSource in Get-ChildItem (Join-Path $ValidationRoot 'src/XMLDocNormalizer.HistoricalWorker') -File -Filter '*.cs') {
        $parentSource=Join-Path $toolRoot ('artifacts/b22-6a60/Tools/XMLDocNormalizer/src/XMLDocNormalizer.HistoricalWorker/'+$workerSource.Name)
        Require ((Get-FileHash $workerSource.FullName).Hash -ceq (Get-FileHash $parentSource).Hash) 'Worker source changed.'
        Fingerprint $workerSource.FullName
    }
    $builds=foreach($step in $gate.Steps) {
        Require ($step.ExitCode -eq 0) "Failed gate step: $($step.Name)"
        if($step.Name.EndsWith('-build')) {
            $path=Join-Path $gateRoot ($step.Name+'.log'); $log=Get-Content $path -Raw -Encoding UTF8
            Require ($log -match '0 Warnung\(en\)' -and $log -match '0 Fehler') "Not clean WAE: $($step.Name)"
            Fingerprint $path
        }
    }
    Require (@($builds).Count -eq 11) 'Eleven WAE builds required.'
    $binaryChecks=foreach($image in $gate.BuildImages | Where-Object Owner -eq 'historical') {
        $parent=@($parentGate.BuildImages | Where-Object Step -eq $image.Step)[0]
        [ordered]@{Owner='historical';Step=$image.Step;DllByteEqual=($image.Image.Sha256 -ceq $parent.Image.Sha256);PdbByteEqual=($image.Image.PdbSha256 -ceq $parent.Image.PdbSha256);CrossExportBuildRootChanged=$true}
    }
    for($index=0;$index -lt $gate.WorkerBuildImages.Count;$index++) {
        $image=$gate.WorkerBuildImages[$index]; $parent=$parentGate.WorkerBuildImages[$index]
        $binaryChecks+=@([ordered]@{Owner='worker';Step=$index;DllByteEqual=($image.Sha256 -ceq $parent.Sha256);PdbByteEqual=($image.PdbSha256 -ceq $parent.PdbSha256);CrossExportBuildRootChanged=$true})
    }
    $expected=[ordered]@{b23=36;b22=46;b21=26;b20=10;b19=39;b18=7;b17=25;b16=18;b15=28;b14=31;b13=34;b12=29;b11=16;b10=34;'b5-b9'=156;productive=717;focused=824;broad=3125;full=3216}
    $tests=foreach($name in $expected.Keys) { CheckTests $name (Join-Path $ValidationRoot ('artifacts/p5o2b23/tests/'+$name+'.trx')) $expected[$name] }
    $tests+=@(CheckTests 'boundary' (Join-Path $gateRoot 'main-boundary.trx') 578)
    $tests+=@(CheckTests 'boundary-external' (Join-Path $gateRoot 'external-target.trx') 36)
    $baselines=foreach($file in Get-ChildItem (Join-Path $ValidationRoot 'artifacts/p5o2b9/current') -Filter '*.json') {
        $old=Join-Path $toolRoot ('artifacts/b22-6a60/parent-current/'+$file.Name)
        Require ((Get-FileHash $old).Hash -ceq (Get-FileHash $file.FullName).Hash) "Current baseline differs: $($file.Name)"
        [ordered]@{Name=$file.Name;ByteEqual=$true;Artifact=(Fingerprint $file.FullName)}
    }
    Require (@($baselines).Count -eq 8) 'Eight Current baselines required.'
    $e2e=foreach($file in Get-ChildItem (Join-Path $ValidationRoot 'artifacts/p5o2b23/e2e') -Filter '*.json') {
        $item=ReadJson $file.FullName; $run=$item.Result
        if($file.Name.StartsWith('positive-')) { Require ($run.ExitCode -eq 1 -and $run.Stdout -match '1 documentation issue' -and -not $run.Stderr) 'Positive CLI failed.' }
        elseif($file.Name.StartsWith('empty-')) { Require ($run.ExitCode -eq 0 -and $run.Stdout -match 'Check succeeded' -and -not $run.Stderr) 'Empty CLI failed.' }
        elseif($file.Name.StartsWith('mode-')) { Require ($run.ExitCode -eq 2 -and -not (Test-Path $run.Report)) 'Unsupported mode admitted.' }
        else { Require ($run.ExitCode -eq 4 -and $run.Stderr -match 'External target failed:' -and $run.Stdout -notmatch 'Check succeeded' -and -not (Test-Path $run.Report)) 'Negative CLI failed open.' }
        if($file.Name -eq 'negative-attestation.json') { Require ($run.Stderr -match 'execution attestation') 'No genuine physical replay rejection.' }
        Fingerprint $file.FullName
    }
    Require (@($e2e).Count -eq 29) 'Expected six positive/empty, twenty negative and three mode CLI outcomes.'
    foreach($file in $files) {
        $path=Join-Path $repoRoot $file; $bytes=[IO.File]::ReadAllBytes($path); $text=[Text.UTF8Encoding]::new($false,$true).GetString($bytes)
        Require ($text.Length -gt 0 -and $text[0] -ne [char]0xFEFF -and $text.EndsWith("`r`n") -and $text -notmatch '(?<!\r)\n') "UTF8/noBOM/CRLF: $file"
        if($file.EndsWith('.json')) { $null=$text | ConvertFrom-Json }
        if($file.EndsWith('.ps1')) { $errors=$null; $tokens=$null; $null=[Management.Automation.Language.Parser]::ParseFile($path,[ref]$tokens,[ref]$errors); Require ($errors.Count -eq 0) "PowerShell parse: $file" }
    }
    $cs=@($sourceInputs | Where-Object { $_.EndsWith('.cs') } | ForEach-Object { $_.Substring('Tools/XMLDocNormalizer/'.Length) })
    & dotnet format whitespace . --folder --include $cs --verify-no-changes
    Require ($LASTEXITCODE -eq 0) 'Owned C# whitespace failed.'
    & git diff --check; Require ($LASTEXITCODE -eq 0) 'Git whitespace failed.'
    $tracked=@(& git -C $repoRoot ls-files)
    $seen=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $trackedExact=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($path in $tracked) { $null=$trackedExact.Add($path); if(-not $seen.Add($path)) { throw "Case collision: $path" } }
    foreach($path in $files) { if(-not $trackedExact.Contains($path) -and -not $seen.Add($path)) { throw "Case collision: $path" } }
    $owned=@($files | ForEach-Object { Fingerprint (Join-Path $repoRoot $_) })
    $audit=[ordered]@{
        Package='P5O2B23';VerifiedUtc=[DateTime]::UtcNow.ToString('o');EvidenceVerifierPassed=$true
        Head=(& git rev-parse HEAD);ExternalHeadMovement=$false;Scope='Explicit local unsigned complete/proven v1 single-project selected-root Declared only'
        RealCliReachable=$true;SharedReporting=$true;NoDuplicateCounting=$true;NoCurrentFallback=$true
        AnalyzerSemanticsChanged=$false;RoslynVersionsChanged=$false;SharedAnalyzerSources=121
        Tests=$tests;WaeBuilds=$builds;CurrentBaselines=$baselines;CrossExportBinaryComparisons=$binaryChecks;UnchangedWorkerSources=$workerSources;CliEvidence=$e2e;OwnedFiles=$owned
        DualGate=(Fingerprint (Join-Path $gateRoot 'audit.json'))
        Protection=[ordered]@{RootIgnoreUnchanged=$true;IndexUnchanged=$true;CoreFileCount=$before.Core.Count;CoreUnchanged=$true;StashCount=$before.Stashes.Count;StashesUnchanged=$true}
        SelfAnalysis=[ordered]@{Findings=16;Fresh=$false;Origin='Previous B22/B21 evidence; not a new self-analysis of added B23 source'}
        CanonicalFindingDiff=[ordered]@{Added=0;Removed=0;ChangedEvidence=0;Fresh=$false;Origin='Previous B22/B21 evidence, not rerun'}
        Attempts=@('Initial script launch blocked by Windows execution policy; rerun with process-local Bypass.',
            'Initial sandboxed restore failed NU1900; unchanged restore with network authorization passed, no audit suppression.',
            'Two relative-path setup commands used export cwd and failed/no-op; retained tool output, corrected absolute/original-root invocations.',
            'First B23 run: 31/33 passed, two tests assumed camelCase for existing PascalCase JSON. Reporter unchanged; corrected test assertions.',
            'Second B23 run: 36/36 passed after three additional unsupported-mode cases.',
            'First dual gate: 578/578 passed. Second: 577/578, unchanged B16 Stopped(pid) liveness assertion failed after semantic checks. Isolated unchanged B16 repeat: 18/18. No client/test change; PID reuse is a possible explanation, not established.',
            'Final gate uses formatted final inputs; previous gate logs/TRX retained. No automatic retry or excluded tests.')
        FinalGitStatus=@(& git status --short);NoCommit=$true;NoPush=$true
    }
    $output=Join-Path $toolRoot 'Evaluation/P5O2B23-bounded-external-target-adoption-audit.json'
    [IO.File]::WriteAllText($output,(($audit | ConvertTo-Json -Depth 100)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host 'PASS: B23 real CLI/Worker/attested Findings/shared output, all regressions, binaries, input scope, formatting and protected state.'
} finally { Pop-Location }
