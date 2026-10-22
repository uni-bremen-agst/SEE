$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$source = Get-Content (Join-Path $tool 'Evaluation/P5O2B25A7Proof/Capture-Inventory.ps1') -Raw -Encoding UTF8
$source = $source.Replace('artifacts/p5o2b25a7/inventory.json', 'artifacts/p5o2b25a8/characterization-inventory.json')
$source = $source.Replace('$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ''../..''))', ('$tool = ''' + $tool.Replace("'", "''") + "'"))
& ([scriptblock]::Create($source))
