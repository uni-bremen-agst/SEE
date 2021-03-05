REM This batch file sets the various environment variables needed to 
REM run the Axivion Suite tools and then executes the command line parameters
REM passed to this batch file. If there are none, only the variables are
REM set.

REM user workspace (generally the home directory or the Jenkins workspace)
set "USERWORKSPACE=%userprofile%"

REM Python
set "PATH=C:\Users\SWT\AppData\Local\Programs\Python\Python38;%PATH%"
set "BAUHAUS_PYTHON=C:\Users\SWT\AppData\Local\Programs\Python\Python38\python.exe"

REM include Bauhaus bin in exectuable path
set "PATH=C:\Program Files (x86)\Bauhaus\bin;%PATH%"

REM SEE project directory
set "SEEDIRECTORY=%USERWORKSPACE%\SEE"

REM where the Axivion configuration resides within SEE
set "BAUHAUS_CONFIG=%SEEDIRECTORY%\Axivion"

REM where the Axivion dashserver configuration resides
set "AXIVION_DASHBOARD_CONFIG=%USERWORKSPACE%\Axivion"
set "AXIVION_DATABASES_DIR=%AXIVION_DASHBOARD_CONFIG%"
set "REQUESTS_CA_BUNDLE=%AXIVION_DASHBOARD_CONFIG%\cert\auto.crt"

REM URL of the dashserver
REM set "AXIVION_DASHBOARD_URL=https://localhost:9443/axivion/"
set "AXIVION_DASHBOARD_URL=https://swt-jenkins.informatik.uni-bremen.de:9443/axivion/"

REM If the dashserver is installed as a Windows service, you can
REM start and stop it as follows:
REM   net (start|stop) "axivion_dashboard_service"
REM or use the Windows Services Console (services.msc).

REM Count the number of command-line parameters
setlocal enabledelayedexpansion
set argCount=0
for %%x in (%*) do (
   set /A argCount+=1
)

REM echo "Number of processed arguments: %argCount%"

REM Execute command line parameters if there are any.
IF %argCount% == 0 (
  echo "No parameters to be executed given"
) ELSE (
  REM We are executing only the first parameter. If this parameter is 
  REM an executable with other parameters, the executable and its
  REM parameters must be enclosed in double quotes.
  
  REM %*
  %~1
)

REM Exit with error code of last executed command (echo if no parameters were given;
REM and otherwise the result of the executed command-line parameters).
exit /b %ERRORLEVEL%
