$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repoRoot=[IO.Path]::GetFullPath((Join-Path $toolRoot '../..'))
function Require([bool]$Value,[string]$Message) { if(-not $Value) { throw $Message } }
function ReadText([string]$Path) { [IO.File]::ReadAllText($Path).Replace("`r`n","`n") }
function Fingerprint([string]$Path) { [ordered]@{Path=[IO.Path]::GetFullPath($Path);Sha256=(Get-FileHash -LiteralPath $Path).Hash} }
Push-Location $toolRoot
try {
    $before=Get-Content 'artifacts/pid-g1/before-wip.json' -Raw -Encoding UTF8 | ConvertFrom-Json
    $client='Tools/XMLDocNormalizer/src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerClient.cs'
    $verified=[Collections.Generic.List[object]]::new()
    foreach($file in $before | Where-Object { $_.Path -like '*/src/*' -or $_.Path -like '*ExceptionFlowHistoricalBoundedFindingTests.cs' -or $_.Path -like '*P5O2B4Proof/TestProcess/Program.cs' -or $_.Path -like '*build/Verify-DualVersionBuild.ps1' }) {
        $full=Join-Path $repoRoot $file.Path
        if($file.Path -ne $client) { Require ((Get-FileHash $full).Hash -ceq $file.Sha256) "Existing B24 semantic/build/fixture input changed: $($file.Path)" }
        $verified.Add((Fingerprint $full))
    }
    $old=ReadText (Join-Path $toolRoot ('artifacts/pid-g1/before-wip/'+$client))
    $now=ReadText (Join-Path $repoRoot $client)
    $restored=$now -replace '        private static readonly ConditionalWeakTable<HistoricalWorkerCallResult, OwnedProcessExit> processExits = new\(\);\n',''
    $restored=$restored -replace '        internal static bool TryGetProcessExit\(HistoricalWorkerCallResult\? call, out OwnedProcessExit\? exit\)\n        \{ exit = null; return call != null && processExits.TryGetValue\(call, out exit\); \}\n',''
    $restored=$restored -replace '            HistoricalWorkerCallResult\? returnedCall = null;\n',''
    $restored=$restored -replace '            HistoricalWorkerCallResult Observe\(HistoricalWorkerCallResult call\) \{ returnedCall = call; return call; \}\n',''
    $restored=$restored -replace 'return Observe\(([^;]+)\);','return $1;'
    $restored=$restored -replace '                if \(returnedCall != null\) \{ processExits.Add\(returnedCall, OwnedProcessExit.Capture\(process\)\); \}\n',''
    Require ($restored -ceq $old) 'Client changes exceed the exact diagnostic wrappers/registration; B21 or lifetime behavior may have changed.'
    $callPath='Tools/XMLDocNormalizer/src/XMLDocNormalizer/Execution/Historical/HistoricalWorkerCallResult.cs'
    $callNow=ReadText (Join-Path $repoRoot $callPath)
    $callRestored=$callNow -replace '        // Main-local lifecycle diagnostics only\. Raw validation / record cloning cannot mint one\.\n',''
    $callRestored=$callRestored -replace '        public OwnedProcessExit\? ProcessExit => HistoricalWorkerClient.TryGetProcessExit\(this, out var exit\) \? exit : null;\n',''
    $callOld=(@(& git -C $repoRoot show ('HEAD:'+$callPath)) -join "`n")+"`n"
    Require ($callRestored -ceq $callOld) 'Call result changes exceed computed lifecycle diagnostics.'
    $verified.Add((Fingerprint (Join-Path $repoRoot $callPath)))
    $allowedNew='Tools/XMLDocNormalizer/src/XMLDocNormalizer/Execution/Historical/OwnedProcessExit.cs'
    $verified.Add((Fingerprint (Join-Path $repoRoot $allowedNew)))
    $changed=@(& git -C $repoRoot diff --name-only HEAD -- Tools/XMLDocNormalizer/src)+@(& git -C $repoRoot ls-files --others --exclude-standard -- Tools/XMLDocNormalizer/src)
    $oldChanges=@($before.Path | Where-Object { $_ -like 'Tools/XMLDocNormalizer/src/*' })
    foreach($path in $changed) { Require ($path -in $oldChanges -or $path -in @($callPath,$allowedNew)) "Unrelated production change: $path" }
    $parent=Get-Content 'artifacts/b23-a3d8/Tools/XMLDocNormalizer/artifacts/dual-version-build/audit.json' -Raw -Encoding UTF8 | ConvertFrom-Json
    Require ($parent.SharedSourceFingerprints.Count -eq 121) 'Unexpected shared analyzer inventory.'
    $validation=(Get-Content 'artifacts/p5o2b24/export.json' -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
    foreach($file in $parent.SharedSourceFingerprints) {
        $full=Join-Path $toolRoot $file.Path
        $built=Join-Path $validation $file.Path
        Require ((Get-FileHash $built).Hash -ceq $file.Sha256) "Built Analyzer bytes changed: $($file.Path)"
        Require ((ReadText $full) -ceq (ReadText $built)) "Local Analyzer content changed: $($file.Path)"
        $verified.Add((Fingerprint $full))
    }
    & git diff --quiet HEAD -- src/XMLDocNormalizer/XMLDocNormalizer.csproj src/XMLDocNormalizer.ExceptionFlow.Historical src/XMLDocNormalizer.HistoricalWorker/XMLDocNormalizer.HistoricalWorker.csproj build/ExceptionFlow.HistoricalSources.props
    Require ($LASTEXITCODE -eq 0) 'Compiler/project/version/semantic host configuration changed.'
    $audit=[ordered]@{Passed=$true;VerifiedUtc=[DateTime]::UtcNow.ToString('o');B21AuthorityUnchanged=$true;B24SemanticSourcesUnchanged=$true;SharedAnalyzerSourceCount=121;ClientChange='Only Observe wrappers, private exit observation registry and capture after unchanged owned cleanup';VerifiedSources=@($verified.ToArray())}
    [IO.File]::WriteAllText((Join-Path $toolRoot 'artifacts/pid-g1/scope-check.json'),(($audit | ConvertTo-Json -Depth 8)+"`r`n"),[Text.UTF8Encoding]::new($false))
    Write-Host 'PASS: exact client diagnostic delta only; B21/cleanup/B24 semantics, compiler configuration and all 121 shared analyzer sources unchanged.'
} finally { Pop-Location }
