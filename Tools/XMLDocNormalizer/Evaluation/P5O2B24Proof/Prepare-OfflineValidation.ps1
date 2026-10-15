param([Parameter(Mandatory=$true)][string]$ValidationRoot,
    [string]$HistoricalCache='artifacts/b23-a3d8/Tools/XMLDocNormalizer/artifacts/exception-flow-historical/packages')
$ErrorActionPreference='Stop'
$toolRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=[IO.Path]::GetFullPath($ValidationRoot)
if(-not $validation.StartsWith((Join-Path $toolRoot 'artifacts/'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Only an artifacts export may receive validation configuration.' }
$cache=[IO.Path]::GetFullPath((Join-Path $toolRoot $HistoricalCache))
if(-not (Test-Path -LiteralPath $cache -PathType Container)) { throw 'Existing isolated Historical cache required; no network fallback.' }
$mainCache=Join-Path $env:USERPROFILE '.nuget/packages'
if(-not (Test-Path -LiteralPath $mainCache -PathType Container)) { throw 'Existing Current cache required.' }
$destination=Join-Path $validation 'artifacts/exception-flow-historical/packages'
& robocopy $cache $destination /E /R:0 /W:0 /NFL /NDL /NJH /NJS
if($LASTEXITCODE -ge 8) { throw 'Local cache copy failed.' }
# Generated validation-only XML; original production feed/version configuration is never edited.
$settings=[Xml.XmlWriterSettings]::new(); $settings.Indent=$true; $settings.NewLineChars="`r`n"; $settings.Encoding=[Text.UTF8Encoding]::new($false)
[xml]$main='<configuration><packageSources><clear /></packageSources><packageSourceMapping><clear /></packageSourceMapping></configuration>'
$source=$main.CreateElement('add'); $source.SetAttribute('key','local-cache'); $source.SetAttribute('value',$mainCache); $null=$main.configuration.packageSources.AppendChild($source)
$writer=[Xml.XmlWriter]::Create((Join-Path $validation 'NuGet.Config'),$settings)
try { $main.Save($writer) } finally { $writer.Dispose() }
[xml]$historical=Get-Content (Join-Path $toolRoot 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config') -Raw
$disabled=$historical.CreateElement('disabledPackageSources')
foreach($remote in $historical.configuration.packageSources.add) {
    $entry=$historical.CreateElement('add'); $entry.SetAttribute('key',$remote.key); $entry.SetAttribute('value','true'); $null=$disabled.AppendChild($entry)
}
$null=$historical.configuration.AppendChild($disabled)
$source=$historical.CreateElement('add'); $source.SetAttribute('key','offline-cache'); $source.SetAttribute('value',$cache); $null=$historical.configuration.packageSources.AppendChild($source)
$mapping=$historical.CreateElement('packageSource'); $mapping.SetAttribute('key','offline-cache')
foreach($id in @('Microsoft.CodeAnalysis.Common','Microsoft.CodeAnalysis.CSharp','Microsoft.CodeAnalysis.Analyzers','System.Collections.Immutable','System.Reflection.Metadata')) {
    $package=$historical.CreateElement('package'); $package.SetAttribute('pattern',$id); $null=$mapping.AppendChild($package)
}
$null=$historical.configuration.packageSourceMapping.AppendChild($mapping)
$writer=[Xml.XmlWriter]::Create((Join-Path $validation 'src/XMLDocNormalizer.ExceptionFlow.Historical/NuGet.Config'),$settings)
try { $historical.Save($writer) } finally { $writer.Dispose() }
Write-Host 'Offline export only: original feed names/values retained, all remote sources disabled, explicitly mapped local caches.'
