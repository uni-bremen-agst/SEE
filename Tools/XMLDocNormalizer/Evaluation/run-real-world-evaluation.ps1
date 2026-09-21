param(
    [string]$Manifest = (Join-Path $PSScriptRoot 'real-world-evaluation.manifest.json'),
    [string]$Workspace = (Join-Path $PSScriptRoot '..\artifacts\real-world-evaluation'),
    [string]$Output = (Join-Path $PSScriptRoot '..\artifacts\real-world-evaluation\reports'),
    [ValidateSet('enabled', 'disabled', 'both')]
    [string]$SourceLink = 'both'
)

$ErrorActionPreference = 'Stop'

function Invoke-Evaluation([string]$Policy, [string]$ReportDirectory)
{
    dotnet run --project (Join-Path $PSScriptRoot 'XMLDocNormalizer.Evaluation\XMLDocNormalizer.Evaluation.csproj') -- `
        --manifest ([IO.Path]::GetFullPath($Manifest)) `
        --workspace ([IO.Path]::GetFullPath($Workspace)) `
        --output ([IO.Path]::GetFullPath($ReportDirectory)) `
        --source-link $Policy

    if ($LASTEXITCODE -ne 0)
    {
        throw "Real-world evaluation reported an unexpected outcome for Source Link $Policy."
    }
}

if ($SourceLink -eq 'both')
{
    Invoke-Evaluation 'disabled' (Join-Path $Output 'source-link-disabled')
    Invoke-Evaluation 'enabled' (Join-Path $Output 'source-link-enabled')
}
else
{
    Invoke-Evaluation $SourceLink $Output
}
