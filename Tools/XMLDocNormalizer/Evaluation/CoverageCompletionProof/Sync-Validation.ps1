param([Parameter(Mandatory=$true)][string]$Revision)
$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$export=(Get-Content (Join-Path $PSScriptRoot 'export.json') -Raw|ConvertFrom-Json).Export
$destination=Join-Path $PSScriptRoot ($Revision+'-inputs.json')
if(Test-Path $destination){throw 'Preserve previous input revision'}
$paths=@(& git --no-optional-locks -C $tool diff --relative --name-only -- src Tests build README.md docs)
$paths+=@(& git --no-optional-locks -C $tool ls-files -o --exclude-standard -- src Tests build docs)
$records=@(foreach($path in $paths|Sort-Object -Unique){
 if($path -match '(^|/)(bin|obj|artifacts)/'){continue}
 $source=Join-Path $tool $path; $target=Join-Path $export $path
 $null=New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force
 Copy-Item -LiteralPath $source -Destination $target
 [ordered]@{Path=$path;WorkspaceSha256=(Get-FileHash $source).Hash;ExportSha256=(Get-FileHash $target).Hash}
})
[ordered]@{Revision=$Revision;Export=$export;Files=$records;CapturedUtc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json -Depth 6|Set-Content $destination -Encoding UTF8
Write-Output ('Synced '+$records.Count+' authorized input files')
