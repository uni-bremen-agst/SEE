$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$repo = [IO.Path]::GetFullPath((Join-Path $tool '../..'))
function Require([bool]$condition, [string]$message) {
    if (!$condition) { throw $message }
}
function Hash([string]$path) {
    Require (Test-Path -LiteralPath $path -PathType Leaf) ('Missing file: ' + $path)
    (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}
function Json([string]$relative) {
    Get-Content -LiteralPath (Join-Path $tool $relative) -Raw -Encoding UTF8 | ConvertFrom-Json
}
Push-Location $tool
try {
    $head = & git --no-optional-locks rev-parse HEAD
    Require ($LASTEXITCODE -eq 0) 'Cannot read HEAD.'
    Require ($head -ceq '9b76e179a568c64ae972cc3a45980a094e44245e') 'Unexpected HEAD movement; document, do not repair.'
    $parent = & git --no-optional-locks rev-parse 'HEAD^'
    Require ($LASTEXITCODE -eq 0 -and $parent -ceq 'c777f9642c145a57c112c28216c3996128c6234b') 'Unexpected parent.'
    & git --no-optional-locks diff --cached --quiet
    Require ($LASTEXITCODE -eq 0) 'Semantic index changed; no repair authorized.'
    Require ((Hash (Join-Path $repo '.git/index')) -ceq 'CB38F3D3B845ABD54FEC4C2E111E77BFF7F31B80A73F438A657BAF03173F8158') 'Physical index changed; do not repair.'
    Require ((Hash (Join-Path $repo '.gitignore')) -ceq '7FA827563D3AD2A24C52417F5EA208ED0A3018EA825F847B7E130B10264FF9E5') 'Protected ignore changed.'
    $changed = @(& git --no-optional-locks -C $repo diff --name-only HEAD)
    Require ($LASTEXITCODE -eq 0) 'Cannot read tracked diff.'
    Require ($changed.Count -eq 1 -and $changed[0] -ceq '.gitignore') 'Unexpected tracked change: Phase 0 must not edit production, tests or old evidence.'

    $start = Json 'artifacts/p5o2b25a9/start.json'
    $core = @(Get-ChildItem -LiteralPath (Join-Path $tool 'src/XMLDocNormalizer.ExceptionFlow.Core') -Recurse -File -Force)
    Require ($core.Count -eq 23 -and @($start.Core).Count -eq 23) 'Core inventory changed.'
    foreach ($file in $start.Core) { Require ((Hash $file.Path) -ceq $file.Sha256) ('Core changed: ' + $file.Path) }
    $stashes = @(& git --no-optional-locks stash list --format='%H %gs')
    Require ($LASTEXITCODE -eq 0) 'Cannot read stash inventory.'
    Require ($stashes.Count -eq 6 -and ($stashes -join "`n") -ceq ($start.Stashes -join "`n")) 'Stashes changed.'
    Require (@($start.OriginalFiles).Count -eq 1092) 'Unexpected inherited manifest.'
    foreach ($file in $start.OriginalFiles) { Require ((Hash $file.Path) -ceq $file.Sha256) ('Inherited file changed: ' + $file.Path) }
    Require ((Hash (Join-Path $tool 'Evaluation/P5O2B25A9-decidable-query-profile-audit.json')) -ceq '10FA0C41DAEFFED0B07962997F52A4A8B714F496D41444E0B7673FBD7437DA7E') 'Frozen A9 audit changed.'
    Require ((Hash (Join-Path $tool 'Evaluation/P5O2B25A9Proof/Characterization-preflight.md')) -ceq 'C7AEE5E4F1D82944BF4DF53EF0FDF3FD398041EB496554D233F494DB9B94D1E6') 'Frozen A9 preflight changed.'

    $inventory = Json 'artifacts/p5o2b25a9/characterization-inventory.json'
    $pattern = $inventory.Pattern
    $hits = @(& rg -n -i $pattern Tests --glob '*.cs' --glob '!**/bin/**' --glob '!**/obj/**')
    Require ($LASTEXITCODE -eq 0 -and $hits.Count -eq 513) 'Characterization search changed.'
    $paths = @(& rg -l -i $pattern Tests --glob '*.cs' --glob '!**/bin/**' --glob '!**/obj/**')
    Require ($LASTEXITCODE -eq 0) 'Characterization file search failed.'
    $paths += 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowRecursiveSummaryClosureTests.cs'
    Require (@($paths | Sort-Object -Unique).Count -eq 71 -and @($inventory.Files).Count -eq 71) 'Characterization inventory changed.'
    foreach ($file in $inventory.Files) { Require ((Hash (Join-Path $tool $file.Path)) -ceq $file.Sha256) ('Existing test changed: ' + $file.Path) }

    $audit = Json 'Evaluation/P5O2B25A10-finite-orbit-admission-and-projection-audit.json'
    Require (!$audit.ProductionImplementationStarted -and $audit.ProductionFilesChanged -eq 0 -and $audit.TestFilesChanged -eq 0) 'Not an unchanged-source read-only audit.'
    Require ($audit.CharacterizationInventory.ApprovalReceived -and $audit.Status -ceq 'STOP_ProtectedCoreDrift_AndSupplementalBindingAmbiguities_NoImplementation') 'Expected recorded protected-Core/supplemental-binding STOP; never silently accept drift.'
    Require (!$audit.RawGraphProductionBypassFound -and $audit.CharacterizationInventory.AdditionalApprovalReceived) 'Expected no raw bypass and approved conditional R7.'
    Require ($audit.FixturePreflight.OriginalExcludedMethods -eq 202 -and $audit.FixturePreflight.RemainingUnreconstructedOriginalFixtures -eq 0) 'Original fixture reconstruction count changed.'
    Require (!$audit.FixturePreflight.FullFixturePreflightCompleted -and $audit.FixturePreflight.SupplementalAmbiguousMethods -eq 85) 'Do not claim definitive closure over ambiguous bindings.'
    Require (!$audit.B25BActivated -and !$audit.SolutionTransitiveFindingAdmission) 'Finding admission must stay closed.'
    $status = @(& git --no-optional-locks status --short)
    Require ($LASTEXITCODE -eq 0) 'Cannot read final status.'
    [ordered]@{
        Phase = 'ReadOnlyLifecycleAudit'; Status = 'PASS - protection only, not implementation closure'
        Head = $head; IndexSha256 = (Hash (Join-Path $repo '.git/index'))
        ProtectedCore = 23; Stashes = 6; InheritedFilesVerified = 1092
        CharacterizationFiles = 71; LiteralMatches = 513
        ProductionOrTestChanges = 0; ProductOrTestBuildRun = $false
        Approval = 'R1-R7 and layer A approved; conditional R8-R10 plus explicit supplemental E bindings pending'; GitStatus = $status
    } | ConvertTo-Json -Depth 5
} finally { Pop-Location }
