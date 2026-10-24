param([ValidateSet('Start','Inventory','Final')][string]$Phase = 'Start', [string]$VerifiedRemoteHead)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
$prefix = 'Tools/XMLDocNormalizer/Evaluation/P5O2B25A10Proof/'
function ReadGit([string[]]$arguments) {
    $result = @(& git.exe --no-optional-locks -C $repo @arguments)
    if ($LASTEXITCODE -ne 0) { throw ('Read-only git command failed: ' + ($arguments -join ' ')) }
    return $result
}
function Hash([string]$path) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash }
function BytesHash([byte[]]$bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-','') } finally { $sha.Dispose() }
}
function SaveNew([string]$name, $value) {
    $path = Join-Path $PSScriptRoot $name
    if (Test-Path -LiteralPath $path) { throw ('Preserve existing audit: ' + $path) }
    [IO.File]::WriteAllText($path,(($value | ConvertTo-Json -Depth 18)+"`r`n"),[Text.UTF8Encoding]::new($false))
}
$head = @(ReadGit @('rev-parse','HEAD'))[0]
$index = Hash (Join-Path $repo '.git/index')
$ignore = Hash (Join-Path $repo '.gitignore')
$stashes = @(ReadGit @('stash','list','--format=%H %gs'))
$status = @(ReadGit @('status','--short'))
$staged = @(ReadGit @('diff','--cached','--name-only'))
$previous = Get-Content (Join-Path $tool 'Evaluation/P5O2B25A10Proof/FinalClosure/lifetime-closure.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$core = @(foreach ($file in $previous.Core) {
    [ordered]@{Path=$file.Path;A10ClosureSha256=$file.AcceptedS1OrFrozenSha256;ActualSha256=(Hash $file.Path)}
})
if ($Phase -eq 'Final') {
    $start = Get-Content (Join-Path $PSScriptRoot 'repository-start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $differences = @(foreach ($file in $start.HistoricalFiles) {
        if ((Hash (Join-Path $repo $file.Path)) -cne $file.WorktreeSha256) { $file.Path }
    })
    $coreDifferences = @(foreach ($file in $start.Core) { if ((Hash $file.Path) -cne $file.ActualSha256) { $file.Path } })
    if ($head -cne $start.Head -or $index -cne $start.PhysicalIndexSha256 -or $ignore -cne $start.RootIgnoreSha256 -or
        $staged.Count -ne 0 -or $differences.Count -ne 0 -or $coreDifferences.Count -ne 0 -or
        ($stashes -join "`n") -cne ($start.Stashes -join "`n")) { throw 'Protected input changed during review; STOP, do not repair.' }
    SaveNew 'repository-final.json' ([ordered]@{
        CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=$head;PhysicalIndexSha256=$index;RootIgnoreSha256=$ignore
        HistoricalFilesChecked=$start.HistoricalFiles.Count;HistoricalDifferences=$differences;CoreFilesChecked=$core.Count;CoreDifferencesThisReview=$coreDifferences
        IndexUnchanged=$true;SemanticIndexClean=$true;RootIgnoreUnchanged=$true;SixStashesUnchanged=$true
        GitStatusShort=$status;TestsExecuted=0;BuildsExecuted=0;CleanupPerformed=$false;ProductionOrTestFilesChanged=$false
        B25BActivated=$false;Commit=$false;Push=$false;HistoryRewrite=$false;StashOperations=$false
    })
    Write-Host 'PASS: final read-only review protection checks; no test/build/activation.'
    exit 0
}
if ($VerifiedRemoteHead -cne $head) { throw 'Pass the directly observed matching remote HEAD, not a cached upstream assumption.' }
$added = @(ReadGit @('diff-tree','--no-commit-id','--diff-filter=A','--name-only','-r',$head,'--',$prefix))
$rows = @(foreach ($line in ReadGit @('ls-tree','-r','-l',$head,'--',$prefix)) {
    if ($line -notmatch '^\d+ blob ([0-9a-f]+)\s+(\d+)\t(.+)$') { throw ('Unexpected Git tree entry: ' + $line) }
    $blob=$Matches[1];$size=[long]$Matches[2];$path=$Matches[3];$relative=$path.Substring($prefix.Length)
    $classification = if ($relative -match '(^|/)(bin|obj|packages?|cache|caches|temp|tmp)/' -or $relative -match '\.(dll|pdb|exe|nupkg|cache)$') { 'H-CLEAN-CANDIDATE' }
        elseif ($relative -match '\.(trx|log)$' -or $size -ge 1MB) { 'H-REVIEW-RETAIN' } else { 'H-KEEP' }
    [pscustomobject]@{Path=$path;Relative=$relative;GitBlob=$blob;GitBlobBytes=$size;AddedByA10=$added -contains $path;WorktreeBytes=(Get-Item -LiteralPath (Join-Path $repo $path)).Length;WorktreeSha256=(Hash (Join-Path $repo $path));Classification=$classification}
})
$groups = @($rows | Group-Object {$_.Relative.Split('/')[0]} | ForEach-Object {
    [ordered]@{Name=$_.Name;Files=$_.Count;GitBlobBytes=($_.Group | Measure-Object GitBlobBytes -Sum).Sum}
})
$categories = @(foreach ($pattern in @('(^|/)(bin|obj)/','(^|/)(packages?|cache|caches|temp|tmp)/','\.(dll|pdb|exe|nupkg|cache)$','\.trx$','\.log$')) {
    $files=@($rows | Where-Object Relative -match $pattern)
    [ordered]@{Pattern=$pattern;Files=$files.Count;GitBlobBytes=($files | Measure-Object GitBlobBytes -Sum).Sum}
})
$duplicates = @($rows | Group-Object GitBlob | Where-Object Count -gt 1 | ForEach-Object {
    [pscustomobject][ordered]@{GitBlob=$_.Name;Copies=$_.Count;EachGitBlobBytes=$_.Group[0].GitBlobBytes;ExtraBytes=($_.Count-1)*$_.Group[0].GitBlobBytes;Paths=@($_.Group.Path);Decision='H-REVIEW-RETAIN: same bytes are not proof of dispensable historical references; no deletion or relinking in this review.'}
})
$historical = @(foreach ($path in ReadGit @('ls-files','--','Tools/XMLDocNormalizer/Evaluation')) {
    if ($path -match '/P5O2B25(?!BReadiness)' -and $path -notmatch '(^|/)(bin|obj)/') {
        [ordered]@{Path=$path;WorktreeSha256=(Hash (Join-Path $repo $path))}
    }
})
$info = $core | Where-Object Path -like '*.AssemblyInfo.cs'
$cache = $core | Where-Object Path -like '*.AssemblyInfoInputs.cache'
$infoText = [IO.File]::ReadAllText($info.Path)
$reconstructedOld = $infoText.Replace($head,$previous.Head)
$metadata = @(foreach ($revision in @($previous.Head,$head)) {
    $text = $infoText.Replace($head,$revision)
    $attributes = @([regex]::Matches($text,'\[assembly: ([^(]+)\("([^"]*)"\)\]') | ForEach-Object { $_.Groups[1].Value + $_.Groups[2].Value })
    $inputText = ($attributes -join [string][char]0x2028) + [char]0x2028
    $digest = (BytesHash ([Text.Encoding]::UTF8.GetBytes($inputText))).ToLowerInvariant()
    [ordered]@{Commit=$revision;AssemblyInfoSha256=(BytesHash ([Text.Encoding]::UTF8.GetBytes($text)));CacheDigest=$digest;CacheFileSha256=(BytesHash ([Text.Encoding]::UTF8.GetBytes($digest+"`r`n")))}
})
$metadataOnly = (BytesHash ([Text.Encoding]::UTF8.GetBytes($reconstructedOld))) -ceq $info.A10ClosureSha256 -and
    $metadata[0].CacheFileSha256 -ceq $cache.A10ClosureSha256 -and $metadata[1].CacheFileSha256 -ceq $cache.ActualSha256
if ($Phase -eq 'Start') { SaveNew 'repository-start.json' ([ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o');Head=$head;Parent=@(ReadGit @('rev-parse','HEAD^'))[0];Branch=@(ReadGit @('branch','--show-current'))[0]
    Upstream=@(ReadGit @('rev-parse','--abbrev-ref','--symbolic-full-name','@{upstream}'))[0];CachedUpstreamHead=@(ReadGit @('rev-parse','@{upstream}'))[0]
    DirectRemoteHead=$VerifiedRemoteHead;DirectRemoteCommand='git --no-optional-locks ls-remote --exit-code origin refs/heads/fix/documentation; read-only escalation after sandbox port22 denial'
    HeadConfirmedPushed=$true;GitStatusShort=$status;StagedPaths=$staged;PhysicalIndexSha256=$index;RootIgnoreSha256=$ignore;Stashes=$stashes
    Core=$core;PreexistingCoreMetadataDifferences=@($core | Where-Object {$_.ActualSha256 -cne $_.A10ClosureSha256})
    GeneratedCommitMetadataOnlyReproduced=$metadataOnly;MetadataReconstructions=$metadata;WriterProcessProved=$false;HistoricalProtectionManifestChanged=$false
    ArtifactsDirectoryExists=(Test-Path -LiteralPath (Join-Path $tool 'artifacts'));HistoricalFiles=$historical
}) }
SaveNew 'hygiene-inventory.json' ([ordered]@{
    Commit=$head;Parent=@(ReadGit @('rev-parse','HEAD^'))[0];Scope=$prefix;TrackedFiles=$rows.Count;AddedFiles=$added.Count
    TotalGitBlobBytes=($rows | Measure-Object GitBlobBytes -Sum).Sum;SizeBasis='Git blob bytes at pushed HEAD; worktree CRLF bytes are separately recorded, never substituted for original hashes'
    Rows=$rows;Groups=$groups;Categories=$categories;DuplicateGroups=$duplicates
    DuplicateExtraCopies=($duplicates | ForEach-Object {$_.Copies-1} | Measure-Object -Sum).Sum
    DuplicateExtraBytes=($duplicates | Measure-Object ExtraBytes -Sum).Sum
    CleanupPerformed=$false;RootIgnoreEdited=$false;LocalProofIgnoreEdited=$false
    Decision='No tracked H-CLEAN build/cache/binary products. Retain all hash-referenced final/historical result sets and nontrivial intermediate evidence; deduplication requires a separate reference-preserving plan.'
})
Write-Host ('Captured pushed HEAD/protection and ' + $rows.Count + ' A10 Proof Git blobs; no cleanup or build.')
