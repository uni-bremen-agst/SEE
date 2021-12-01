@REM This script will generate C# lexers for each of the g4 files in this directory. 
@ECHO OFF
for %%f in (*.g4) do (
    java -jar antlr.jar -Dlanguage=CSharp %%~nf.g4
    IF %ERRORLEVEL% NEQ 0 GOTO ERROR
    IF %ERRORLEVEL% EQU 0 ECHO Lexer generated for %%f%.
)
EXIT 0

ERROR:
ECHO Error occured while generating lexer for %%f%.