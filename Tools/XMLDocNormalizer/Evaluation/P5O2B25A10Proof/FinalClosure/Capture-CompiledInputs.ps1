param([ValidatePattern('^[0-9]{2}$')][string]$Revision = '17')
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$validation = Join-Path $tool ('artifacts/b25a10-development-' + $Revision)
$relativeRecords = 'artifacts/p5o2b25a10/final-r' + $Revision
$records = Join-Path $tool $relativeRecords
$destination = Join-Path $records 'validation-inputs.json'
if (Test-Path -LiteralPath $destination) { throw 'Do not overwrite compiled input capture.' }
function Source([string]$relative) {
    $workspace = (Get-FileHash (Join-Path $tool $relative)).Hash
    $export = (Get-FileHash (Join-Path $validation $relative)).Hash
    if ($workspace -cne $export) { throw ('Source/export mismatch: ' + $relative) }
    [ordered]@{ Path=$relative; WorkspaceSha256=$workspace; ExportSha256=$export }
}
[xml]$manifest = Get-Content (Join-Path $validation 'build/ExceptionFlow.HistoricalSources.props') -Raw -Encoding UTF8
$shared = @(foreach ($include in $manifest.Project.ItemGroup.Compile.Include) {
    $full = [IO.Path]::GetFullPath((Join-Path $validation $include.Replace('$(MSBuildThisFileDirectory)', 'build/').Replace('\','/')))
    Source $full.Substring($validation.Length + 1).Replace('\','/')
})
if ($shared.Count -ne 125) { throw 'Unexpected shared-source inventory.' }
$sources = @(foreach ($path in @('Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerProcessTests.cs','Tests/XMLDocNormalizerTests/Worker/ExceptionFlowFiniteOrbitAdmissionTests.cs','Tests/XMLDocNormalizerTests/Helpers/ExceptionFlowFiniteOrbitAssertions.cs','Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalMetadataReferenceFactoryTests.cs')) { Source $path })
$images = @(foreach ($path in @('src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll','src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll','artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll','artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll','Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll')) {
    [ordered]@{ Path=$path; Sha256=(Get-FileHash (Join-Path $validation $path)).Hash }
})
[IO.File]::WriteAllText((Join-Path $records 'export.json'),(([ordered]@{ToolRoot=$validation} | ConvertTo-Json)+"`r`n"),[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText($destination,(([ordered]@{Sources=$sources;SharedSources=$shared;Images=$images} | ConvertTo-Json -Depth 10)+"`r`n"),[Text.UTF8Encoding]::new($false))
# Reuse the frozen metadata-only verifier with output paths redirected in
# memory. No historical script/audit is edited; no product/compiler is loaded.
$source = Get-Content (Join-Path $tool 'Evaluation/P5O2B25A5Proof/Verify-CompiledSources.ps1') -Raw -Encoding UTF8
$source = $source.Replace('artifacts/p5o2b25a5',$relativeRecords).Replace('P5O2B25A5','P5O2B25A10')
$source = $source.Replace('$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', ('$tool=''' + $tool.Replace("'","''") + "'"))
& ([scriptblock]::Create($source))
