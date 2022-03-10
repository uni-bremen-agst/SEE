@echo off

REM This script create a GXL file for all C# source files located in directory src.
REM Precondition: The Axivion Suite must be installed.

setlocal enabledelayedexpansion

REM Determine the parent directory of the current directory
REM for %%i in ("%~dp0..") do set "parent=%%~fi"
set "base=%~dp0"
REM Replace backslashes to slashes: Axivion Suite uses slashes
set "base=%base:\=/%"

REM Collect the source files
set "files="
for %%f in (src\*.cs) do set files=!files! "%%f"

REM Create RFG for code
cafesharp.exe -B"%base%" /out:mini.ir %files%
ir2rfg.exe mini.ir mini.rfg

REM Reduce the graph and export to GXL
rfgscript reduce.py --graph mini.rfg --view "Code Facts" --linkname "N global:mini" out.rfg
rfgexport -f GXL --view "Code Facts" out.rfg CodeFacts.gxl

REM Delete intermediate files
del out.rfg
del mini.ir
del mini.rfg
