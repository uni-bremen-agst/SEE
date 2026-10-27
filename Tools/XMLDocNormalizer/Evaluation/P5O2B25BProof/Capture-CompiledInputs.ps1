$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $PSScriptRoot 'implementation-export.json') -Raw | ConvertFrom-Json).Export
$records=Join-Path $PSScriptRoot 'Results'
if (Test-Path (Join-Path $records 'validation-inputs.json')) { throw 'Preserve existing compiled-input capture' }
function Source([string]$relative) {
    $workspace=(Get-FileHash -LiteralPath (Join-Path $tool $relative)).Hash
    $export=(Get-FileHash -LiteralPath (Join-Path $validation $relative)).Hash
    if ($workspace -cne $export) { throw ('Source/export mismatch: ' + $relative) }
    [ordered]@{Path=$relative;WorkspaceSha256=$workspace;ExportSha256=$export}
}
function Sources([string]$project) {
    $raw=& dotnet msbuild (Join-Path $validation $project) -getItem:Compile -nologo
    if ($LASTEXITCODE -ne 0) { throw 'Compile inventory failed' }
    $items=($raw -join "`n" | ConvertFrom-Json).Items.Compile
    @(foreach($item in $items) {
        $relative=[IO.Path]::GetFullPath($item.FullPath).Substring($validation.Length+1).Replace('\','/')
        if ($relative -notmatch '(^|/)(bin|obj|artifacts)/') { Source $relative }
    })
}
$tests=Sources 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
$main=Sources 'src/XMLDocNormalizer/XMLDocNormalizer.csproj'
$historical=Sources 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj'
$shared=@($historical | Where-Object Path -like 'src/XMLDocNormalizer/*')
if ($shared.Count -ne 125) { throw '125 shared sources required' }
$images=@(foreach($path in @('src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll','src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll','artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll','artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll','Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll')) {
    [ordered]@{Path=$path;Sha256=(Get-FileHash (Join-Path $validation $path)).Hash}
})
[ordered]@{Sources=$tests;MainSources=$main;HistoricalSources=$historical;SharedSources=$shared;Images=$images} | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $records 'validation-inputs.json') -Encoding UTF8
[ordered]@{ToolRoot=$validation} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $records 'export.json') -Encoding UTF8
# Reuse the frozen metadata-only verifier, changing only package/output bindings and the source sets to all actual inputs.
$source=Get-Content (Join-Path $tool 'Evaluation/P5O2B25A5Proof/Verify-CompiledSources.ps1') -Raw
$source=$source.Replace("`$tool=[IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../..'))", "`$tool='$tool'")
$source=$source.Replace('artifacts/p5o2b25a5','Evaluation/P5O2B25BProof/Results').Replace('P5O2B25A5','P5O2B25B')
$source=$source.Replace('@($inputs.Sources)+@($inputs.SharedSources)','@($inputs.Sources)+@($inputs.MainSources)+@($inputs.HistoricalSources)')
$source=$source.Replace('else { @($inputs.SharedSources) }', 'elseif($imagePath.StartsWith(''src/'')) { @($inputs.MainSources) } else { @($inputs.HistoricalSources) }')
& ([scriptblock]::Create($source))
