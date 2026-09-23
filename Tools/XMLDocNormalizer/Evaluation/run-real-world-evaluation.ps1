param(
    [string]$Manifest = (Join-Path $PSScriptRoot 'real-world-evaluation.manifest.json'),
    [string]$Workspace = (Join-Path $PSScriptRoot '..\artifacts\real-world-evaluation'),
    [string]$Output = (Join-Path $PSScriptRoot '..\artifacts\real-world-evaluation\reports'),
    [ValidateSet('enabled', 'disabled', 'both')]
    [string]$SourceLink = 'both',
    [ValidateSet('strict', 'verified-line-endings', 'both')]
    [string]$SourceReconstruction = 'strict'
)

$ErrorActionPreference = 'Stop'

function Invoke-Evaluation(
    [string]$SourceLinkPolicy,
    [string]$ReconstructionPolicy,
    [string]$ReportDirectory)
{
    dotnet run --project (Join-Path $PSScriptRoot 'XMLDocNormalizer.Evaluation\XMLDocNormalizer.Evaluation.csproj') -- `
        --manifest ([IO.Path]::GetFullPath($Manifest)) `
        --workspace ([IO.Path]::GetFullPath($Workspace)) `
        --output ([IO.Path]::GetFullPath($ReportDirectory)) `
        --source-link $SourceLinkPolicy `
        --source-reconstruction $ReconstructionPolicy

    if ($LASTEXITCODE -ne 0)
    {
        throw "Real-world evaluation reported an unexpected outcome for Source Link $SourceLinkPolicy and reconstruction $ReconstructionPolicy."
    }
}

$sourceLinkPolicies = if ($SourceLink -eq 'both') { @('disabled', 'enabled') } else { @($SourceLink) }
$reconstructionPolicies = if ($SourceReconstruction -eq 'both') { @('strict', 'verified-line-endings') } else { @($SourceReconstruction) }

foreach ($sourceLinkPolicy in $sourceLinkPolicies)
{
    foreach ($reconstructionPolicy in $reconstructionPolicies)
    {
        $reportDirectory = if ($sourceLinkPolicies.Count -eq 1 -and $reconstructionPolicies.Count -eq 1)
        {
            $Output
        }
        elseif ($reconstructionPolicies.Count -eq 1 -and $reconstructionPolicy -eq 'strict')
        {
            Join-Path $Output "source-link-$sourceLinkPolicy"
        }
        elseif ($sourceLinkPolicies.Count -eq 1)
        {
            Join-Path $Output "source-reconstruction-$reconstructionPolicy"
        }
        else
        {
            Join-Path $Output "source-link-$sourceLinkPolicy-source-reconstruction-$reconstructionPolicy"
        }

        Invoke-Evaluation $sourceLinkPolicy $reconstructionPolicy $reportDirectory
    }
}
