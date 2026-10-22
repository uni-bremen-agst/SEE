$ErrorActionPreference = 'Stop'
# Execute the frozen A5 metadata-only verifier with ONLY package/record paths
# redirected in memory. No historical script or evidence file is edited/run
# with its old output paths; no build or analyzer is invoked by this verifier.
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$original = Join-Path $tool 'Evaluation/P5O2B25A5Proof/Verify-CompiledSources.ps1'
$source = Get-Content -LiteralPath $original -Raw -Encoding UTF8
$source = $source.Replace('p5o2b25a5', 'p5o2b25a6-r2').Replace('P5O2B25A5', 'P5O2B25A6')
$source = $source.Replace('$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', ('$tool=''' + $tool.Replace("'", "''") + "'"))
& ([scriptblock]::Create($source))
