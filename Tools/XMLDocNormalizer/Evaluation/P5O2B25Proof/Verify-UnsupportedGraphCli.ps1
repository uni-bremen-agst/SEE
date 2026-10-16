$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validation=(Get-Content (Join-Path $tool 'artifacts/p5o2b25/export.json') -Raw -Encoding UTF8 | ConvertFrom-Json).ToolRoot
$destination=Join-Path $tool 'artifacts/p5o2b25/raw-graph-cli'
if(Test-Path $destination) { throw 'Preserve every raw graph CLI attempt.' }
[IO.Directory]::CreateDirectory($destination) | Out-Null
$runs=foreach($count in @(2,3)) {
    # These are the actual complete raw graph jobs that succeeded in the
    # withdrawn prototype. Rejection is now checked against HEAD-identical Main.
    $prototype=Get-Content (Join-Path $validation ('artifacts/p5o2b25/cases/cli-'+$count+'-False.json')) -Raw -Encoding UTF8 | ConvertFrom-Json
    $job=Join-Path ([IO.Path]::GetDirectoryName($prototype.Run.Report)) 'job.json'
    $raw=Get-Content $job -Raw -Encoding UTF8 | ConvertFrom-Json
    if($raw.supportingProjects.Count -ne ($count-1)) { throw 'Not a complete explicit prototype graph.' }
    $report=Join-Path $destination ('report-'+$count+'.json')
    $previous=$ErrorActionPreference
    try { $ErrorActionPreference='Continue'; $log=& dotnet (Join-Path $validation 'src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll') --check --external-target $job --format json --output $report 2>&1 | Out-String; $code=$LASTEXITCODE }
    finally { $ErrorActionPreference=$previous }
    [IO.File]::WriteAllText((Join-Path $destination ('cli-'+$count+'.log')),$log,[Text.UTF8Encoding]::new($false))
    if($code -ne 4 -or (Test-Path $report) -or $log -notmatch 'supportingProjects') { throw 'Complete raw graph job was not visibly rejected without reporting.' }
    [ordered]@{Projects=$count;Job=$job;JobSha256=(Get-FileHash $job).Hash;ExitCode=$code;NoReport=$true;SchemaNotAdmitted=$true;Log=$log}
}
$result=[ordered]@{Passed=$true;GeneratedUtc=[DateTime]::UtcNow.ToString('o');MainSha256=(Get-FileHash (Join-Path $validation 'src/XMLDocNormalizer/bin/Debug/net8.0/XMLDocNormalizer.dll')).Hash;Runs=@($runs)}
[IO.File]::WriteAllText((Join-Path $destination 'audit.json'),(($result | ConvertTo-Json -Depth 8)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host 'PASS: complete A->B and A->B->C raw jobs remain unsupported in the unchanged production CLI; exit 4, no reports.'
