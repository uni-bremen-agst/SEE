$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$originalRecords = Join-Path $tool 'artifacts/p5o2b25a6'
$records = Join-Path $tool 'artifacts/p5o2b25a6-r2'
$originalExport = (Get-Content (Join-Path $originalRecords 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Hash([string]$path) { [ordered]@{ Path = [IO.Path]::GetFullPath($path); Sha256 = (Get-FileHash -LiteralPath $path).Hash } }
$relative = 'Tests/XMLDocNormalizerTests/Worker/ExceptionFlowGenericExpansionBoundaryTests.cs'
$oldPath = Join-Path $originalExport $relative
$newPath = Join-Path $tool $relative
$old = (Get-Content -LiteralPath $oldPath -Raw -Encoding UTF8).Replace("`r`n", "`n")
$new = (Get-Content -LiteralPath $newPath -Raw -Encoding UTF8).Replace("`r`n", "`n")
$before = 'observations.Add(new { Step = step, Exact = RoslynCanonicalIdentityFactory.CreateCallableIdentity(instance), Successor = RoslynCanonicalIdentityFactory.CreateCallableIdentity(successor) });'
$after = 'observations.Add(new { Step = step, ExactKey = CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(instance)), SuccessorKey = CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(successor)) });'
Require ($old.Contains($before) -and $new.Contains($after) -and $new.Replace($after, $before) -ceq $old) 'Source correction differs beyond new observation output projection.'
$inputsPath = Join-Path $originalRecords 'validation-inputs.json'
$inputs = Get-Content -LiteralPath $inputsPath -Raw -Encoding UTF8 | ConvertFrom-Json
Require ((Get-FileHash $oldPath).Hash -ceq $inputs.Sources[0].ExportSha256) 'Initial source capture changed.'
foreach ($image in $inputs.Images) { Require ((Get-FileHash (Join-Path $originalExport $image.Path)).Hash -ceq $image.Sha256) ('Initial image changed: ' + $image.Path) }
$trx = Join-Path $originalExport 'artifacts/p5o2b25a6/tests/witnesses.trx'
[xml]$xml = Get-Content -LiteralPath $trx -Raw -Encoding UTF8
$counter = $xml.SelectSingleNode('//*[local-name()="Counters"]')
Require ([int]$counter.total -eq 8 -and [int]$counter.failed -eq 2 -and [int]$counter.passed -eq 6 -and [int]$counter.notExecuted -eq 0) 'Retain exact initial failure inventory.'
$errors = @($xml.SelectNodes('//*[local-name()="UnitTestResult" and @outcome!="Passed"]') | ForEach-Object {
    Require ($_.testName -match 'dictionary: True' -and $_.Output.ErrorInfo.Message -match 'System.Text.Json.JsonException' -and $_.Output.ErrorInfo.StackTrace -match 'BoundaryTests.Evidence') 'Unexpected initial failure; investigate, do not relabel.'
    [ordered]@{ Test = $_.testName; Message = $_.Output.ErrorInfo.Message; Stack = $_.Output.ErrorInfo.StackTrace }
})
$runs = @(foreach ($name in @('build', 'dual', 'witnesses')) {
    $path = Join-Path $originalRecords ('runs/' + $name + '.json')
    $run = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    Require ($null -ne $run.ExitCode) 'Initial run incomplete.'
    Require (($name -eq 'witnesses' -and $run.ExitCode -eq 1) -or ($name -ne 'witnesses' -and $run.ExitCode -eq 0)) 'Initial run exit changed.'
    [ordered]@{ Name = $name; ExitCode = $run.ExitCode; Artifact = (Hash $path); Log = (Hash (Join-Path $originalRecords ('runs/' + $name + '.log'))) }
})
foreach ($name in @('contexts', 'broad', 'full')) { Require (!(Test-Path -LiteralPath (Join-Path $originalRecords ('runs/' + $name + '.json')))) ('Unexpected initial later run: ' + $name) }
$result = [ordered]@{ Passed = $true; InitialSource = (Hash $oldPath); CorrectedSource = (Hash $newPath); OnlyNewObservationSerializationChanged = $true; AssertionsUnchanged = $true; WitnessStepsUnchanged = 24; SerializerDepthUnchanged = $true; InitialTrx = (Hash $trx); InitialFailed = 2; InitialPassed = 6; Failures = $errors; InitialRuns = $runs; InitialInputs = (Hash $inputsPath); InitialCompiledBindings = (Hash (Join-Path $originalRecords 'compiled-source-verification.json')); InitialImagesRetained = $true; UnchangedGreenRetry = $false; InitialBroadOrFullExecuted = $false }
[IO.File]::WriteAllText((Join-Path $records 'source-correction-revision.json'), (($result | ConvertTo-Json -Depth 12) + "`r`n"), [Text.UTF8Encoding]::new($false))
Write-Host 'PASS: exact one-line new witness output fix; initial 6/8 failure, all sources/images/logs preserved.'
