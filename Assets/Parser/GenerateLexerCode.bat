@REM This script will generate C# lexers for each of the g4 files in this directory. 
@ECHO OFF
for %%f in (*.g4) do (
    java -jar antlr.jar -Dlanguage=CSharp %%~nf.g4
)