@REM Must be executed in an Axivion command shell

@REM This is to be able to set the environment variables only for this run of the script.
@setlocal

if "%JAVA_HOME%"=="" (
  @set "JAVA_HOME=C:\Program Files\OpenJDK\jdk-17.0.2"
  @set "PATH=%JAVA_HOME%\bin;%PATH%"
)

if "%JACOCO%"=="" (
  @set "JACOCO=C:\Program Files\jacoco\lib"
)

if "%AXIVION%"=="" (
  set "AXIVION=C:\Program Files (x86)\Bauhaus"
)

@set PROJECT=CodeFacts
@set JAVAFILES=files.txt

@echo Compiling Java project...

@REM Determine the Java source files to be analyzed.
@dir /b/s *.java > %JAVAFILES%

@REM Compile Java files.
@javac -g -cp "%TOOLSJAR%" @%JAVAFILES%

@echo Running dynamic analysis...

@REM Run dynamic analysis.
@java -cp . ExecutedLoCLogger -output %PROJECT%.jlg Main both mystring vowels mystring consonants mystring count 100 

@echo Running static analysis...

@REM Create RFG.
@"%AXIVION%\bin\java2rfg" -rfg %PROJECT%.rfg -nocode -8 -cp . @%JAVAFILES%

@REM Export RFG to GXL.
@"%AXIVION%\bin\rfgexport" -f GXL --view "Code Facts" %PROJECT%.rfg %PROJECT%.gxl

@echo Gathering test coverage with JaCoCo...

if exist "%JACOCO%\jacococli.jar" (
@   REM Run coverge analysis. Results are contained in jacoco.exec.
@   java -cp . -javaagent:"%JACOCO%\jacocoagent.jar=output=file" Main both mystring vowels mystring consonants mystring count 100
@   java -jar "%JACOCO%\jacococli.jar" report jacoco.exec --classfiles . --sourcefiles . --xml jacoco.xml
) else (
@   echo "'%JACOCO%' does not exist."
)

@echo Cleaning up...
@REM Clean up generated files not needed.
@del *.class Unterordner\*.class 2>nul
@del *.rfg 2>nul
@del %JAVAFILES% 2>nul
@del jacoco.exec 2>nul

:end
