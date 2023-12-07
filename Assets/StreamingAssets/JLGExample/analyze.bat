REM Must be executed in an Axivion command shell

set PROJECT=CodeFacts
set JAVAFILES=files.txt

REM Determine the Java source files to be analyzed
dir /b/s *.java > %JAVAFILES%

REM Compile Java files
javac -g -cp "%TOOLSJAR%" @%JAVAFILES%

REM Run dynamic analysis
java -cp . ExecutedLoCLogger -output %PROJECT%.jlg Main both mystring vowels mystring consonants mystring count 100 

REM Create RFG
java2rfg -rfg %PROJECT%.rfg -nocode -8 -cp . @%JAVAFILES%

REM Export RFG to GXL
rfgexport -f GXL --view "Code Facts" %PROJECT%.rfg %PROJECT%.gxl

REM Clean up
del *.class Unterordner\*.class
del *.rfg
del %JAVAFILES%
