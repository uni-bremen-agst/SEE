param([string]$Revision='')
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export=(Get-Content (Join-Path $PSScriptRoot 'export.json') -Raw|ConvertFrom-Json).Export
$records=Join-Path $PSScriptRoot 'Results';$null=New-Item -ItemType Directory -Path $records -Force
if($Revision){$records=Join-Path $records $Revision;$null=New-Item -ItemType Directory -Path $records -Force}
if(Test-Path (Join-Path $records 'validation-inputs.json')){throw 'Preserve compiled input capture'}
function Sources([string]$project){
 $raw=& dotnet msbuild (Join-Path $export $project) -getItem:Compile -nologo
 if($LASTEXITCODE){throw 'Compile inventory failed'}
 @(foreach($item in ($raw -join "`n"|ConvertFrom-Json).Items.Compile){
  $relative=[IO.Path]::GetFullPath($item.FullPath).Substring($export.Length+1).Replace('\','/')
  if($relative -match '(^|/)(bin|obj|artifacts)/'){continue}
  $before=(Get-FileHash (Join-Path $tool $relative)).Hash;$after=(Get-FileHash (Join-Path $export $relative)).Hash
  if($before -cne $after){throw ('Source/export mismatch: '+$relative)}
  [ordered]@{Path=$relative;WorkspaceSha256=$before;ExportSha256=$after}
 })
}
$tests=Sources 'Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj'
$main=Sources 'src/XMLDocNormalizer/XMLDocNormalizer.csproj'
$historical=Sources 'src/XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj'
$shared=@($historical|Where-Object Path -like 'src/XMLDocNormalizer/*')
if($shared.Count -ne 125){throw 'Shared125 contract changed'}
$images=@(foreach($path in @('src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll','src/XMLDocNormalizer.ExceptionFlow.Core/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Core.dll','artifacts/exception-flow-historical/XMLDocNormalizer.ExceptionFlow.Historical/bin/Debug/net8.0/XMLDocNormalizer.ExceptionFlow.Historical.dll','artifacts/exception-flow-historical/XMLDocNormalizer.HistoricalWorker/bin/Debug/net8.0/XMLDocNormalizer.HistoricalWorker.dll','Tests/XMLDocNormalizerTests/bin/Debug/net8.0/XMLDocNormalizerTests.dll')){[ordered]@{Path=$path;Sha256=(Get-FileHash (Join-Path $export $path)).Hash}})
[ordered]@{Sources=$tests;MainSources=$main;HistoricalSources=$historical;SharedSources=$shared;Images=$images}|ConvertTo-Json -Depth 9|Set-Content (Join-Path $records 'validation-inputs.json') -Encoding UTF8
[ordered]@{ToolRoot=$export}|ConvertTo-Json|Set-Content (Join-Path $records 'export.json') -Encoding UTF8
# Existing metadata-only PDB verifier, with only paths/package label and the
# complete actual source inventories rebound to this new evidence package.
$source=Get-Content (Join-Path $tool 'Evaluation/P5O2B25A5Proof/Verify-CompiledSources.ps1') -Raw
$source=$source.Replace("`$tool=[IO.Path]::GetFullPath((Join-Path `$PSScriptRoot '../..'))", "`$tool='$tool'")
$binding='Evaluation/CoverageCompletionProof/Results'
if($Revision){$binding+='/'+$Revision}
$source=$source.Replace('artifacts/p5o2b25a5',$binding).Replace('P5O2B25A5','CoverageCompletion')
$source=$source.Replace('@($inputs.Sources)+@($inputs.SharedSources)','@($inputs.Sources)+@($inputs.MainSources)+@($inputs.HistoricalSources)')
$source=$source.Replace('else { @($inputs.SharedSources) }', 'elseif($imagePath.StartsWith(''src/'')) { @($inputs.MainSources) } else { @($inputs.HistoricalSources) }')
& ([scriptblock]::Create($source))
