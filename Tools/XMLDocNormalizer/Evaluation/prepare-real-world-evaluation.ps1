param(
    [string]$Manifest = (Join-Path $PSScriptRoot 'real-world-evaluation.manifest.json'),
    [string]$Workspace = (Join-Path $PSScriptRoot '..\artifacts\real-world-evaluation')
)

$ErrorActionPreference = 'Stop'
$manifestPath = [IO.Path]::GetFullPath($Manifest)
$workspacePath = [IO.Path]::GetFullPath($Workspace)
$downloadsPath = Join-Path $workspacePath 'downloads'
New-Item -ItemType Directory -Force -Path $downloadsPath | Out-Null
$definition = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

foreach ($artifact in $definition.artifacts)
{
    $archivePath = Join-Path $downloadsPath ($artifact.id + '.zip')
    Invoke-WebRequest -UseBasicParsing -Uri $artifact.url -OutFile $archivePath
    $actualHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash

    if (-not [string]::Equals($actualHash, $artifact.sha256, [StringComparison]::OrdinalIgnoreCase))
    {
        throw "SHA-256 mismatch for $($artifact.id)."
    }

    $destination = [IO.Path]::GetFullPath((Join-Path $workspacePath $artifact.extractTo))
    $prefix = $workspacePath.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar

    if (-not $destination.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase))
    {
        throw "Extraction target leaves the evaluation workspace: $destination"
    }

    New-Item -ItemType Directory -Force -Path $destination | Out-Null
    Expand-Archive -LiteralPath $archivePath -DestinationPath $destination -Force
}

Write-Output "Prepared pinned package data under $workspacePath"
