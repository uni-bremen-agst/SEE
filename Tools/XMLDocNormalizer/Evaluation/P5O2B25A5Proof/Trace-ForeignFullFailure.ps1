$ErrorActionPreference = 'Stop'
$tool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records = Join-Path $tool 'artifacts/p5o2b25a5'
$validation = (Get-Content (Join-Path $records 'export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
function Require([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Hash([string]$Path) { [ordered]@{ Path = $Path; Sha256 = (Get-FileHash -LiteralPath $Path).Hash } }
function Hex($Bytes) { [BitConverter]::ToString([byte[]]$Bytes).Replace('-', '') }
# Metadata-only inspection, never executing the failing test, emitter or factory.
$assemblies = @{}
foreach ($relative in @('system.runtime.compilerservices.unsafe/6.1.0/lib/net462/System.Runtime.CompilerServices.Unsafe.dll', 'system.buffers/4.6.0/lib/net462/System.Buffers.dll', 'system.numerics.vectors/4.6.0/lib/net462/System.Numerics.Vectors.dll', 'system.memory/4.6.0/lib/net462/System.Memory.dll', 'system.collections.immutable/9.0.0/lib/net462/System.Collections.Immutable.dll', 'system.reflection.metadata/9.0.0/lib/net462/System.Reflection.Metadata.dll')) {
    $assembly = [Reflection.Assembly]::LoadFrom((Join-Path "$env:USERPROFILE/.nuget/packages" $relative))
    $assemblies[$assembly.GetName().Name] = $assembly
}
$resolver = [ResolveEventHandler]{ param($sender, $eventArgs) $name = [Reflection.AssemblyName]::new($eventArgs.Name).Name; if ($assemblies.ContainsKey($name)) { return $assemblies[$name] }; return $null }
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$path = Join-Path $validation 'Tests/XMLDocNormalizerTests/bin/Debug/net8.0/System.Reflection.Metadata.dll'
$stream = [IO.File]::OpenRead($path)
$pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
try {
    $reader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
    $types = @($reader.TypeDefinitions | Where-Object { $t = $reader.GetTypeDefinition($_); $reader.GetString($t.Name) -ceq 'AssemblyFile' -and $reader.GetString($t.Namespace) -ceq 'System.Reflection.Metadata' })
    Require ($types.Count -eq 1) 'Exactly one actual net8 AssemblyFile type required.'
    $type = $reader.GetTypeDefinition($types[0])
    $fields = @($type.GetFields() | ForEach-Object { $field = $reader.GetFieldDefinition($_); [ordered]@{ Name = $reader.GetString($field.Name); Signature = (Hex $reader.GetBlobBytes($field.Signature)) } })
    Require (@($fields | Where-Object Name -ceq '_reader').Count -eq 1) 'Actual executed dependency must retain reader backing.'
    $methods = @($type.GetMethods() | Where-Object { $reader.GetString($reader.GetMethodDefinition($_).Name) -ceq 'get_ContainsMetadata' })
    Require ($methods.Count -eq 1) 'ContainsMetadata getter missing.'
    $method = $reader.GetMethodDefinition($methods[0])
    $body = [System.Reflection.Metadata.PEReaderExtensions]::GetMethodBody($pe, $method.RelativeVirtualAddress)
    $il = Hex $body.GetILBytes()
    $relative = 'Tests/XMLDocNormalizerTests/Execution/Semantic/ExternalMetadataReferenceFactoryTests.cs'
    $workspace = Join-Path $tool $relative; $export = Join-Path $validation $relative
    $start = Get-Content (Join-Path $records 'start.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    Require ((Get-FileHash -LiteralPath $workspace).Hash -ceq ($start.OriginalFiles | Where-Object Path -ceq $relative).Sha256) 'Foreign test must remain byte-unchanged.'
    [xml]$trx = Get-Content (Join-Path $validation 'artifacts/p5o2b24/tests/full.trx') -Raw -Encoding UTF8
    $failed = @($trx.SelectNodes('//*[local-name()="UnitTestResult" and @outcome="Failed"]'))
    Require ($failed.Count -eq 1 -and $failed[0].testName -like '*ExternalMetadataReferenceFactoryTests.MultiModuleAssembly_FailsClosed') 'Preserve the actual sole Full failure.'
    $history = @(foreach ($name in @('P5O2B13-historical-source-compiler-scope-equivalence.md', 'P5O2B15-historical-cross-project-solution-scope-equivalence.md', 'P5O2B20-historical-declared-production-boundary.md')) { Hash (Join-Path $tool ('Evaluation/' + $name)) })
    [IO.File]::WriteAllText((Join-Path $records 'foreign-full-failure.json'), (([ordered]@{
        Package = 'P5O2B25A5'; DiagnosticOnly = $true; GeneratedUtc = [DateTime]::UtcNow.ToString('o')
        Test = $failed[0].testName; Message = $failed[0].Output.ErrorInfo.Message; Stack = $failed[0].Output.ErrorInfo.StackTrace; Duration = $failed[0].duration
        WorkspaceSource = (Hash $workspace); ExportSource = (Hash $export); UnchangedSource = $true
        ActualExecutedDependency = (Hash $path); AssemblyFileFields = $fields; ContainsMetadataGetterIl = $il
        ReadOnlyFinding = 'ReadAssemblyFiles returns reader-backed AssemblyFile structs after its using PEReader and stream are disposed; caller then dereferences ContainsMetadata at line 446, before TryCreate at line 451.'
        ExistingLifetimeHazardConfirmed = $true; FactoryCallReached = $false; AnalyzerSummaryEvaluatorOnFailingCallPath = $false
        FailedInvocationPeOrFreedMemoryRetained = $false; ExactMemoryReuseAtFailureProven = $false
        PreExistingReports = $history; TestOrProductionRepaired = $false; IsolatedOrFullRetry = $false; TestBuildOrEmitterInvokedByDiagnostic = $false
        DiagnosticDevelopmentNote = 'Initial metadata-only inspection called the C# GetMethodBody extension as an instance method in PowerShell and failed before output; changed to its static extension call. No test/build/emitter ran in either diagnostic invocation.'
    } | ConvertTo-Json -Depth 10) + "`r`n"), [Text.UTF8Encoding]::new($false))
    Write-Host 'READ-ONLY: actual net8 reader-backed struct confirmed; old Full fixture lifetime hazard, exact failing memory state not retained; no test execution/retry/repair.'
} finally {
    $pe.Dispose(); $stream.Dispose()
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}
