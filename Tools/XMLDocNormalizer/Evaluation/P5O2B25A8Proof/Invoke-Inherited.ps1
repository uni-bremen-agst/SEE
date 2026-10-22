param([Parameter(Mandatory = $true)][ValidateSet('Prepare', 'Run', 'Protection', 'Revision', 'Trace', 'Compiled', 'Static', 'A6Gate', 'A7Gate')][string]$Mode, [string]$Phase)
$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$map = @{ Prepare = 'P5O2B25A6Proof/Prepare-Validation.ps1'; Run = 'P5O2B25A6Proof/Run-Validation.ps1'; Protection = 'P5O2B25A6Proof/Verify-Protection.ps1'; Revision = 'P5O2B25A6Proof/Verify-Revision.ps1'; Trace = 'P5O2B25A6Proof/Trace-Lifecycle.ps1'; Compiled = 'P5O2B25A5Proof/Verify-CompiledSources.ps1'; Static = 'P5O2B25A6Proof/Verify-Static.ps1'; A6Gate = 'P5O2B25A6Proof/Verify-Evidence.ps1'; A7Gate = 'P5O2B25A7Proof/Verify-Contract.ps1' }
$path = Join-Path $tool ('Evaluation/' + $map[$Mode])
$source = Get-Content -LiteralPath $path -Raw -Encoding UTF8
$source = $source.Replace('artifacts/p5o2b25a6-r2', 'artifacts/p5o2b25a8').Replace('artifacts/b25a6-c777-r2', 'artifacts/b25a8-c777')
if ($Mode -eq 'Prepare') { $source = $source.Replace("'^Evaluation/P5O2B25A[67]|ExceptionFlowGenericExpansionBoundaryTests.cs$'", "'^Evaluation/P5O2B25A8'") }
if ($Mode -eq 'Compiled') { $source = $source.Replace('p5o2b25a5', 'p5o2b25a8').Replace('P5O2B25A5', 'P5O2B25A8') }
if ($Mode -in @('A6Gate', 'A7Gate')) { $source = $source.Replace('Evaluation/P5O2B25A6-bounded-generic-context-expansion-after-scc-audit.json', 'artifacts/p5o2b25a8/a6-regression-audit.json') }
if ($Mode -eq 'A6Gate') { $source = $source.Replace('Final corrected source revision r2; initial failed serialization run is retained separately, not hidden or retried unchanged', 'Fresh A8 validation of unchanged final A6 source; original A6 serialization failure remains historical, not an A8 source correction') }
if ($Mode -eq 'A7Gate') { $source = $source.Replace('Evaluation/P5O2B25A7-finite-parametric-generic-context-contract-audit.json', 'artifacts/p5o2b25a8/a7-consistency-audit.json') }
$literalTool = '$tool = ''' + $tool.Replace("'", "''") + "'"
$source = $source.Replace('$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', $literalTool)
$source = $source.Replace('$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', $literalTool)
$source = $source.Replace('$PSScriptRoot', ("'" + $PSScriptRoot.Replace("'", "''") + "'"))
if ($Mode -eq 'Run') { & ([scriptblock]::Create($source)) -Phase $Phase } else { & ([scriptblock]::Create($source)) }
