$ErrorActionPreference='Stop'
$tool=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$directory=Join-Path $PSScriptRoot 'BroadRootCauseAudit'
$output=Join-Path $directory 'assertion-bindings.json'
if(Test-Path -LiteralPath $output){throw 'Retain original binding capture.'}
$partition=Get-Content (Join-Path $directory 'partition.json') -Raw -Encoding UTF8|ConvertFrom-Json
$bindings=@(foreach($row in $partition.Rows|Where-Object {$_.PrimaryGroup -in @('D','W')}){
    $matches=[Collections.Generic.List[object]]::new()
    $inlinePair=switch($row.Method){
        'HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess' {@(84,93)}
        'ExceptionFlowAnalysisRouterTests.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract' {@(161,172)}
        default {@()}
    }
    foreach($assertion in $row.ExistingGrant.Assertions){
        $relative=$assertion.File -replace '[\\/]+','/'
        $lines=Get-Content (Join-Path $tool $relative) -Encoding UTF8
        foreach($frame in [regex]::Matches($row.Stack,'(?<file>[A-Za-z]:[^\r\n]*?\.cs):line (?<line>\d+)')){
            $file=$frame.Groups['file'].Value -replace '[\\/]+','/'
            $line=[int]$frame.Groups['line'].Value
            if(!$file.EndsWith($relative,[StringComparison]::Ordinal)){continue}
            $same=$line -eq $assertion.Line
            $declaration=$assertion.Line -eq $line+1 -and $lines[$line-1].TrimEnd().EndsWith('=')
            if($same -or $declaration){$matches.Add([pscustomobject]@{File=$relative;SequencePoint=$line;GrantedAssertionLine=$assertion.Line;SequencePointAtVariableDeclaration=$declaration;Assertion=$assertion.Current})}
            if($inlinePair.Count -eq 2 -and $assertion.Line -eq $inlinePair[0] -and $assertion.Current.StartsWith('[InlineData(') -and
                $row.Test.Contains('public static async') -and $row.Test.Contains('code: "AnalysisFailure"') -and
                $line -eq $inlinePair[1] -and $lines[$line-1].Contains('Assert.Equal(code,') -and
                ($lines[$inlinePair[0]-1].Trim() -ceq $assertion.Current)){
                $matches.Add([pscustomobject]@{File=$relative;SequencePoint=$line;GrantedInlineDataLine=$assertion.Line;FullInlineData=$assertion.Current;ConsumerAssertionUnchanged=$lines[$line-1].Trim();TruncatedXunitSourceResolvedByUniqueInlineDataCodeAndAsyncPrefix=$true})
            }
        }
    }
    if(!$matches.Count){throw ('No exact granted assertion/containing variable-declaration sequence point in stack: '+$row.Test)}
    $caseRestriction=if($row.PrimaryGroup -eq 'D'){'Exact Fact#0; no InlineData in observed case.'}else{$row.ExistingGrant.Parameters -join '; '}
    [pscustomobject]@{Test=$row.Test;PrimaryGroup=$row.PrimaryGroup;Grant=$row.ExistingGrant.Group;ExactGrantedCase=$row.ExistingGrant.Case;CaseRestriction=$caseRestriction;Binding=@($matches.ToArray());SourceTextVerifiedInPartition=$row.GrantedAssertionTextsPresentInCurrentSource}
})
if($bindings.Count -ne 104){throw 'Require91 semantic plus13 Worker bindings.'}
$record=[ordered]@{Kind='Scoped assertion and actual stack sequence-point binding, not method-name permission inference';Authority='User approved R1-R10 after Phase0; copied draft grant Approved flags remain historical, not current authority.';BoundCases=$bindings.Count;Unbound=0;Rows=$bindings;NewTestsExecuted=$false;ProductionOrTestEdits=0}
[IO.File]::WriteAllText($output,(($record|ConvertTo-Json -Depth 15)+"`r`n"),[Text.UTF8Encoding]::new($false))
Write-Host '104 approved first-failure assertions bound, including variable-declaration sequence points and scoped shared-helper callers.'
