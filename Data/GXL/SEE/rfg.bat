REM This script generates an RFG for our SEE implementation.
REM
REM Expected to be run with an Axivion Command Prompt in the root
REM folder of SEE.
REM
REM The Axivion Suite must be installed and integrated into MSBuild.
REM The integration with MSBuild works as follows. Run with administrator
REM privilages (we assume the Axivion Suite is installed in folder
REM "C:\Program Files (x86)\Bauhaus"):
REM
REM   "C:\Program Files (x86)\Bauhaus\doc\MSBuild\install_csharp_vs2019.cmd"
REM or:
REM   change the current directory to:
REM     "C:\Program Files (x86)\Bauhaus\doc\MSBuild"
REM   and then run:
REM     MSBuild /t:CSharp /p:VisualStudioVersion=19.0 InstallAxivion.proj

REM The folder where Axivion Suite is installed.
REM SET "AXIVION_BASE_DIR=f:\Program Files (x86)\Bauhaus"
SET "AXIVION_BASE_DIR=C:\Program Files (x86)\Bauhaus"

REM The path to MSBuild.
REM SET MSBUILD="f:\Program Files (x86)\Microsoft Visual Studio\2019\MSBuild\Current\Bin\amd64\MSBuild.exe" 
SET MSBUILD="C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" 

REM Generate RFG for SEE:
%MSBUILD% /t:AxivionBuild SEE.csproj
ir2rfg Temp\bin\Debug\SEE.dll.ir  Data\GXL\SEE\SEE.rfg

REM Generate RFG for SEEEditor:
REM %MSBUILD% /t:AxivionBuild SEE_Editor.csproj
REM ir2rfg Temp\bin\Debug\SEE_Editor.dll.ir  Data\GXL\SEE\SEE_Editor.rfg

REM Generate IR for all C# projects
REM FOR %%f IN (*.csproj) DO (
REM  Echo %%f
REM  %MSBUILD% %%f
REM )

REM Generate IR for selected C# projects
REM FOR %%f IN (SEE.csproj SEE_Editor.csproj SEETests.csproj) DO (
REM  Echo %%f
REM  %MSBUILD% /t:AxivionBuild %%f
REM )


REM Generate RFGs for all IRs
REM FOR %%f IN (Temp\bin\Debug\*.ir) DO (
REM  Echo %%f
REM  REM %%~nf expands f to a file name only.
REM  ir2rfg %%f Data\GXL\SEE\%%~nf.rfg
REM )


