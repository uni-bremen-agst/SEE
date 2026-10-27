$ErrorActionPreference='Stop'
$proof=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$tool=[IO.Path]::GetFullPath((Join-Path $proof '../..'))
$repo=[IO.Path]::GetFullPath((Join-Path $tool '../..'))
$destination=Join-Path $PSScriptRoot 'start.json'
if(Test-Path -LiteralPath $destination) { throw 'Preserve existing closure start' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
function GitRead([string[]]$arguments) { $out=@(& git.exe --no-optional-locks -C $repo @arguments); if($LASTEXITCODE -ne 0){throw 'Read-only Git failed'}; $out }
$prior=Get-Content (Join-Path $proof 'Preflight/start.json') -Raw | ConvertFrom-Json
$initial=@(Get-ChildItem -LiteralPath $proof -File -Recurse | Where-Object { !$_.FullName.StartsWith($PSScriptRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) } | ForEach-Object {
    [ordered]@{Path=$_.FullName.Substring($proof.Length+1).Replace('\','/');Bytes=$_.Length;Sha256=(Hash $_.FullName)}
})
$sources=@(foreach($path in GitRead @('ls-files','-c','-o','--exclude-standard','--','Tools/XMLDocNormalizer/src','Tools/XMLDocNormalizer/Tests','Tools/XMLDocNormalizer/build')) {
    if($path -match '(^|/)(bin|obj|artifacts)/'){continue}
    [ordered]@{Path=$path;Sha256=(Hash (Join-Path $repo $path))}
})
$core=@(foreach($file in $prior.Core) { [ordered]@{Path=$file.Path;HistoricalSha256=$file.Sha256;ClosureEntrySha256=(Hash $file.Path);ModifiedUtc=(Get-Item -LiteralPath $file.Path).LastWriteTimeUtc.ToString('o')} })
$historical=@(foreach($file in $prior.HistoricalEvaluationFiles) {
    $actual=Hash (Join-Path $repo $file.Path)
    if($actual -cne $file.Sha256){throw ('Historical drift: '+$file.Path)}
    [ordered]@{Path=$file.Path;Sha256=$actual}
})
$w4=@(foreach($gate in @('broad','boundary','full','harness-isolated','broad-harness','full-harness')) {
    $path='Results/'+$gate+'-evidence/adoption/W4-equal-results-different-graphs.json'
    $file=Get-Item -LiteralPath (Join-Path $proof $path)
    $producer=switch($gate){'boundary'{'broad'} 'harness-isolated'{'full'} default{$gate}}
    [ordered]@{Path=$path;CapturedByGate=$gate;ProducerGate=$producer;ProducedInCapturedGate=$gate -eq $producer;Bytes=$file.Length;Sha256=(Hash $file.FullName);
        GateRecord='Results/'+$gate+'.json';GateRecordSha256=(Hash (Join-Path $proof ('Results/'+$gate+'.json')));
        ProducerRecord='Results/'+$producer+'.json';Role='W4 actual two/three original graphs; equal canonical results; genuine execution-bound cross-pair rejection';
        Consumers=@('Witness-Index.md:13','Evaluation/P5O2B25B-bounded-solution-finding-adoption.md:183','Run-Validation.ps1 capture directory convention','Verify-Gates.ps1:90 checks live EXPORT raw witness, not packaged copy','Results/b25b-gates.json historical existence check');
        StoredRawHashReferencesFound=$false;RequiresPermanentlyRawRepositoryFile=$false;
        Decision='One lossless canonical gzip per distinct RAW SHA-256; retain every gate/execution context and all TRX/logs'}
})
$metadata=@(foreach($revision in @('a2fae5fc29f682461b38385990568fc5da494ae7','1ef19a37bbecc2ba1f9f2b9e2be27f7cc092c65e')) {
    $info=@($core | Where-Object Path -like '*.AssemblyInfo.cs')[0]
    $text=[IO.File]::ReadAllText($info.Path).Replace('1ef19a37bbecc2ba1f9f2b9e2be27f7cc092c65e',$revision)
    $attributes=@([regex]::Matches($text,'\[assembly: ([^(]+)\("([^"]*)"\)\]')|ForEach-Object {$_.Groups[1].Value+$_.Groups[2].Value})
    $sha=[Security.Cryptography.SHA256]::Create()
    try {
        $digest=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($attributes -join [string][char]0x2028)+[char]0x2028))).Replace('-','').ToLowerInvariant()
        [ordered]@{Commit=$revision;AssemblyInfoSha256=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))).Replace('-','');CacheSha256=[BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($digest+"`r`n"))).Replace('-','')}
    } finally {$sha.Dispose()}
})
$staged=@(GitRead @('diff','--cached','--name-only'))
if($staged.Count){throw 'Index must stay untouched'}
[ordered]@{CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=@(GitRead @('rev-parse','HEAD'))[0];Parent=@(GitRead @('rev-parse','HEAD^'))[0];
    Branch=@(GitRead @('branch','--show-current'))[0];Upstream=@(GitRead @('rev-parse','--abbrev-ref','@{upstream}'))[0];GitStatus=@(GitRead @('status','--short'));
    IndexSha256=(Hash (Join-Path $repo '.git/index'));RootIgnoreSha256=(Hash (Join-Path $repo '.gitignore'));Stashes=@(GitRead @('stash','list','--format=%H %gs'));
    OriginalFiles=$initial;OriginalFileCount=$initial.Count;OriginalBytes=($initial|ForEach-Object {$_.Bytes}|Measure-Object -Sum).Sum;Sources=$sources;Core=$core;HistoricalFiles=$historical;
    PreexistingCoreDifferences=@($core|Where-Object {$_.ClosureEntrySha256 -cne $_.HistoricalSha256});MetadataReconstructions=$metadata;
    CoreDifferenceClassification='Pre-existing generated commit metadata only, old/new AssemblyInfo and cache hashes reproduced exactly; writer not proved. Historical protection manifests and Exit1 evidence NOT replaced or repaired.';
    W4=$w4;SemanticComparisonExcludingOnlyProcessAndExecutionIdsSha256='BB51399F4D208879697D144F1BDD596F8E114C5056A9554FCD37D5212E2337AA';
    RawPayloadsNotSemanticallyCollapsed=$true;ProductTestsStarted=0;RuntimeStatus='Runtime stability investigation: OPEN - recurrent, unattributed process failure'} |
    ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $destination -Encoding UTF8
Write-Output ('Closure entry frozen: '+$initial.Count+' files; '+$w4.Count+' W4 paths; no production/test/Core/index/history mutation')
