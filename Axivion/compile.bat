REM This script generates an RFG for our SEE implementation.
REM
REM It is expected to be run within an Axivion Command Prompt in the root 
REM folder of SEE.
REM
REM Axivion Suite version 7.1.4 or higher must be installed.

REM path to resulting RFG (relative to SEE root directory).
SET "RFG=Data\GXL\SEE\SEE.rfg"

REM The path to MSBuild.
SET MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" 

REM Compile C# code into IR
REM  -solution:SEE.sln ^
build_csproj ^
 -msbuild:%MSBUILD% ^
 -p:langversion=latest ^
 -p:AdditionalOptions="/B%cd%" ^
 SEE.csproj

REM Extract RFG from IR
ir2rfg Temp\bin\Debug\SEE.dll.ir %RFG%

REM Reduce the graph to all components in SEE and only its immediate neighbors.
rfgscript Axivion\reduce.py --graph %RFG% --view "Code Facts" --namespace SEE %RFG%
 
REM RFG can be visualized as follows:
REM gravis Data\GXL\SEE\SEE.rfg
