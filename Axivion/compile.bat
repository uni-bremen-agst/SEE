REM This script generates an RFG for our SEE implementation.
REM
REM It is expected to be run within an Axivion Command Prompt in the root 
REM folder of SEE.
REM
REM Axivion Suite version 7.3 or higher must be installed.

REM ---------------------
REM Configuration Section
REM ---------------------

REM path to resulting RFG (relative to SEE root directory).
SET "RFG=Data\GXL\SEE\SEE.rfg"
REM The output GXL file. Same path as RFG but with .gxl as file extension.
SET "GXL=%RFG:~0,-4%.gxl"

REM The path to MSBuild needed by csharp2rfg.
SET "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" 

REM The path to the Unity editor to generate the solution and csproj files.
SET "UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.9f1\Editor\Unity.exe"

REM The path to AspNetCore.App needed by csharp2rfg.
SET "ASPNETCORE=C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\6.0.22"

REM goto export

REM -------------
REM Build Section
REM -------------

REM The Visual Studio .csproj files need to be created before we can start the build.
:createCSPROJ
 "%UNITY%" ^
 -batchmode -nographics -logFile - ^
 -executeMethod CITools.SolutionGenerator.Sync -projectPath . -quit

REM Compile C# code into IR
REM build_csproj -msbuild:%MSBUILD% -p:langversion=latest -p:AdditionalOptions="/B%cd%" SEE.csproj
REM Extract RFG from IR
REM ir2rfg Temp\bin\Debug\SEE.dll.ir %RFG%

REM Generate RFG
:generateRFG
csharp2rfg --library --no_duplicate_edges ^
 --configuration DEBUG ^
 --msbuild_path "%MSBUILD%" ^
 -v --framework_path "%ASPNETCORE%" ^
 SEE.csproj "%RFG%"
 
REM Reduce the graph to all components in SEE and only its immediate neighbors.
:reduce
rfgscript Axivion\reduce.py --graph "%RFG%" "%RFG%"

:export
REM Export to GXL.
rfgexport -f GXL -o "Code Facts" "%RFG%" "%GXL%"

REM RFG can be visualized as follows:
REM gravis %RFG%
