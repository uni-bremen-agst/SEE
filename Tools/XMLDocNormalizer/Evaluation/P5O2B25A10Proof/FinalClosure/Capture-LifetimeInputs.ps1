$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = (Get-Content (Join-Path $PSScriptRoot 'lifetime-validation-location-short.json') -Raw | ConvertFrom-Json).ValidationRoot
$previous = Join-Path $tool 'artifacts/b25a10-development-17'
$records = Join-Path $tool 'artifacts/p5o2b25a10/final-r18'
[IO.Directory]::CreateDirectory($records) | Out-Null
$destination = Join-Path $records 'lifetime-input-delta.json'
if (Test-Path $destination) { throw 'Preserve previous capture.' }
function Hash([string]$path) { (Get-FileHash -LiteralPath $path).Hash }
$harness = 'Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalMetadataReferenceFactoryTests.cs'
$before = [IO.File]::ReadAllText((Join-Path $previous $harness)).Replace("`r`n","`n")
$after = [IO.File]::ReadAllText((Join-Path $tool $harness)).Replace("`r`n","`n")
$insertion = @'
            // AssemblyFile retains its MetadataReader. Keep its PE image alive
            // until the assertion has finished consuming the rows.
            static IEnumerable<AssemblyFile> ReadAssemblyFiles(byte[] image)
            {
                using MemoryStream stream = new(image, writable: false);
                using PEReader peReader = new(stream);
                MetadataReader reader = peReader.GetMetadataReader();

                foreach (AssemblyFileHandle handle in reader.AssemblyFiles)
                {
                    yield return reader.GetAssemblyFile(handle);
                }
            }

'@
$insertion = $insertion.Replace("`r`n","`n") + "`n"
if ($after.Replace($insertion,'') -cne $before -or !$after.Contains($insertion)) { throw 'Delta is not exactly the authorized local iterator insertion.' }
$rows = @(foreach ($root in @('src','Tests','build')) {
    foreach ($file in Get-ChildItem (Join-Path $tool $root) -Recurse -File) {
        $relative = $file.FullName.Substring($tool.Length+1).Replace('\','/')
        if ($relative -match '(^|/)(bin|obj|artifacts)/' -or $file.Name -eq 'NuGet.Config') { continue }
        $old = Hash (Join-Path $previous $relative)
        $current = Hash $file.FullName
        $exported = Hash (Join-Path $validation $relative)
        if ($current -cne $exported) { throw ('Workspace/export mismatch: ' + $relative) }
        if ($current -cne $old -and $relative -cne $harness) { throw ('Unapproved source/test/build delta: ' + $relative) }
        [ordered]@{Path=$relative;PreviousSha256=$old;WorkspaceSha256=$current;ExecutedExportSha256=$exported;Changed=$old -cne $current}
    }
})
# Detect removals too, not just edits/additions in the current inventory.
foreach ($root in @('src','Tests','build')) {
    foreach ($file in Get-ChildItem (Join-Path $previous $root) -Recurse -File) {
        $relative = $file.FullName.Substring($previous.Length+1).Replace('\','/')
        if ($relative -match '(^|/)(bin|obj|artifacts)/' -or $file.Name -eq 'NuGet.Config') { continue }
        if (!(Test-Path (Join-Path $tool $relative))) { throw ('Removed source/input: ' + $relative) }
    }
}
$preserved = @(foreach ($path in @(
    'artifacts/p5o2b25a10/runs/development-17-full.json',
    'artifacts/b25a10-development-17/artifacts/a10-closure-results/full.trx',
    'Evaluation/P5O2B25A10Proof/FinalClosure/await-approval-followup.json',
    'Evaluation/P5O2B25A10Proof/FinalClosure/independent-validation.json',
    'Evaluation/P5O2B25A10Proof/BroadClosure/verification-r17-final-independent.json',
    'Evaluation/P5O2B25A10Proof/FinalClosure/Verify-Gate.ps1')) {
    [ordered]@{Path=$path;Sha256=(Hash (Join-Path $tool $path))}
})
$reuse = @(foreach ($phase in @('broad','boundary','dual')) {
    $path = Join-Path $tool ('artifacts/p5o2b25a10/runs/development-17-' + $phase + '.json')
    $run = Get-Content $path -Raw | ConvertFrom-Json
    if ($null -eq $run.ExitCode -or $run.ExitCode -ne 0) { throw ('Invalid reused run: ' + $phase) }
    [ordered]@{Phase=$phase;RunPath=$path;Sha256=(Hash $path);OriginallyFinishedUtc=$run.FinishedUtc;Reexecuted=$false}
})
$record = [ordered]@{
    CapturedUtc=[DateTime]::UtcNow.ToString('o');ValidationRoot=$validation;PreviousValidationRoot=$previous
    ExactAuthorizedInsertionOnly=$true;BothExistingAssertionsVerbatimUnchanged=$true;FixtureImageGenerationUnchanged=$true
    NoEagerMaterialization=$true;IteratorOwnsStreamAndPEReaderUntilEnumeratorDisposal=$true
    FactoryIndependentlyOwnsItsPEReader=$true;ProductionFilesChanged=0
    Rows=$rows;PreservedSnapshots=$preserved;ReusedRuntimeEvidence=$reuse
    ReuseReason='Only the named Fact gains a local resource-owning iterator; every other src/Tests/build input is byte-identical to export17. Full covers the corrected harness; old Broad/Boundary/Dual/WAE/baseline and independent proofs are not represented as new runs.'
}
[IO.File]::WriteAllText($destination,(($record | ConvertTo-Json -Depth 12)+"`r`n"),[Text.UTF8Encoding]::new($false))
# Existing metadata-only source verifier, with output and export paths rebound;
# it checks the newly compiled test image, including the corrected harness.
$capture = Get-Content (Join-Path $PSScriptRoot 'Capture-CompiledInputs.ps1') -Raw
$capture = $capture.Replace("`$tool = [IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../../..'))", ("`$tool = '" + $tool + "'"))
$capture = $capture.Replace("`$validation = Join-Path `$tool ('artifacts/b25a10-development-' + `$Revision)", ("`$validation = '" + $validation + "'"))
& ([scriptblock]::Create($capture)) -Revision '18'
Write-Host ('Verified exact single-Fact source delta across ' + $rows.Count + ' inputs, plus fresh PE/PDB source bindings.')
