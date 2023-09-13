REM This script generates an RFG for our SEE implementation.
REM
REM It is expected to be run within an Axivion Command Prompt in the root 
REM folder of SEE.
REM
REM Axivion Suite version 7.1.4 or higher must be installed.

REM path to resulting RFG (relative to SEE root directory).
SET "RFG=Data\GXL\SEE\SEE.rfg"

REM The path to MSBuild.
SET MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" 

REM The Visual Studio .csproj files need to be created before we can start the build.
"C:\Program Files\Unity\Hub\Editor\2022.3.9f1\Editor\Unity.exe" ^
 -batchmode -nographics -logFile - ^
 -executeMethod CITools.SolutionGenerator.Sync -projectPath . -quit

REM Compile C# code into IR
REM build_csproj -msbuild:%MSBUILD% -p:langversion=latest -p:AdditionalOptions="/B%cd%" SEE.csproj
REM Extract RFG from IR
REM ir2rfg Temp\bin\Debug\SEE.dll.ir %RFG%

csharp2rfg --library --no_duplicate_edges ^
 --configuration DEBUG ^
 --msbuild_path "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
 -v --framework_path "C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\6.0.22" ^
 SEE.csproj %RFG%
 
REM Reduce the graph to all components in SEE and only its immediate neighbors.
rfgscript Axivion\reduce.py --graph %RFG% --view "Code Facts" --namespace SEE %RFG%                                     
 
REM RFG can be visualized as follows:
REM gravis %RFG%
