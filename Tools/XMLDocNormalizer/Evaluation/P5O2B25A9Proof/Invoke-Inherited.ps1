param([Parameter(Mandatory = $true)][ValidateSet('Prepare', 'Run', 'Protection', 'Revision', 'Trace', 'Compiled', 'Static', 'A6Gate', 'A7Gate', 'A8Gate')][string]$Mode, [string]$Phase)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($Mode -eq 'A8Gate') {
    $path = Join-Path $tool 'Evaluation/P5O2B25A8Proof/Verify-A8.ps1'
    $source = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    $source = $source.Replace('artifacts/p5o2b25a8', 'artifacts/p5o2b25a9')
    # The old failed launcher is historical evidence, NOT a new A9 attempt.
    $source = $source.Replace("Json (Join-Path `$records 'runs/proof-build.json')", "Json (Join-Path `$tool 'artifacts/p5o2b25a8/runs/proof-build.json')")
    $source = $source.Replace("Join-Path `$records ('finite-proof/' + `$name)", "Join-Path `$tool ('artifacts/p5o2b25a8/finite-proof/' + `$name)")
    foreach ($name in @('runs/proof-build.json', 'runs/proof-build.log')) { $source = $source.Replace("Hash (Join-Path `$records '$name')", "Hash (Join-Path `$tool 'artifacts/p5o2b25a8/$name')") }
    $source = $source.Replace("Join-Path `$tool 'Evaluation/P5O2B25A8-correlated-recursive-type-family-contract-audit.json'", "Join-Path `$tool 'artifacts/p5o2b25a9/a8-consistency-audit.json'")
    $source = $source.Replace('$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', ('$tool = ''' + $tool.Replace("'", "''") + "'"))
    $source = $source.Replace('$PSScriptRoot', ("'" + (Join-Path $tool 'Evaluation/P5O2B25A8Proof').Replace("'", "''") + "'"))
    & ([scriptblock]::Create($source))
    exit
}
$map = @{ Prepare = 'P5O2B25A6Proof/Prepare-Validation.ps1'; Run = 'P5O2B25A6Proof/Run-Validation.ps1'; Protection = 'P5O2B25A6Proof/Verify-Protection.ps1'; Revision = 'P5O2B25A6Proof/Verify-Revision.ps1'; Trace = 'P5O2B25A6Proof/Trace-Lifecycle.ps1'; Compiled = 'P5O2B25A5Proof/Verify-CompiledSources.ps1'; Static = 'P5O2B25A6Proof/Verify-Static.ps1'; A6Gate = 'P5O2B25A6Proof/Verify-Evidence.ps1'; A7Gate = 'P5O2B25A7Proof/Verify-Contract.ps1' }
$source = Get-Content -LiteralPath (Join-Path $tool ('Evaluation/' + $map[$Mode])) -Raw -Encoding UTF8
$source = $source.Replace('artifacts/p5o2b25a6-r2', 'artifacts/p5o2b25a9').Replace('artifacts/b25a6-c777-r2', 'artifacts/b25a9-c777')
if ($Mode -eq 'Prepare') { $source = $source.Replace("'^Evaluation/P5O2B25A[67]|ExceptionFlowGenericExpansionBoundaryTests.cs$'", "'^Evaluation/P5O2B25A9'") }
if ($Mode -eq 'Compiled') { $source = $source.Replace('p5o2b25a5', 'p5o2b25a9').Replace('P5O2B25A5', 'P5O2B25A9') }
if ($Mode -in @('A6Gate', 'A7Gate')) { $source = $source.Replace('Evaluation/P5O2B25A6-bounded-generic-context-expansion-after-scc-audit.json', 'artifacts/p5o2b25a9/a6-regression-audit.json') }
if ($Mode -eq 'A6Gate') { $source = $source.Replace('Final corrected source revision r2; initial failed serialization run is retained separately, not hidden or retried unchanged', 'Fresh A9 validation of unchanged A6 source; original serialization revision remains historical') }
if ($Mode -eq 'A7Gate') { $source = $source.Replace('Evaluation/P5O2B25A7-finite-parametric-generic-context-contract-audit.json', 'artifacts/p5o2b25a9/a7-consistency-audit.json') }
$source = $source.Replace('$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', ('$tool = ''' + $tool.Replace("'", "''") + "'"))
$source = $source.Replace('$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', ('$tool = ''' + $tool.Replace("'", "''") + "'"))
$source = $source.Replace('$PSScriptRoot', ("'" + $PSScriptRoot.Replace("'", "''") + "'"))
if ($Mode -eq 'Run') { & ([scriptblock]::Create($source)) -Phase $Phase } else { & ([scriptblock]::Create($source)) }
