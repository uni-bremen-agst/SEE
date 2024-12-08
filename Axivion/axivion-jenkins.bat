@REM This batch file sets the various environment variables needed to
@REM run the Axivion Suite tools and then executes the command line parameters
@REM passed to this batch file. If there are none, only the variables are
@REM set.

@REM This is to be able to set the environment variables only for this run of the script.
@setlocal

@echo off

@REM Because this batch script is called by Jenkins, we apparently need to
@REM call this first to avoid failing with an error because of the error
@REM "The system cannot find the path specified.":
cmd.exe /c

if not "%AXIVION_LOCAL_BUILD%"=="" (
  @echo "Variable AXIVION_LOCAL_BUILD is set. If axivion_ci is run, it will be a local build."

  if "%AXIVION_PASSWORD%"=="" (
    @echo "Environment variable AXIVION_PASSWORD not set. You might be prompted for your Axivion Dashboard password interactively."
  )

  if "%AXIVION_USERNAME%"=="" (
    @echo "Environment variable AXIVION_USERNAME not set. It is assumed that your local account matches your user ID of the Axivion Dashboard."
  )

  if "%AXIVION_REFERENCE_DATE%"=="" (
    @echo "Environment variable AXIVION_REFERENCE_DATE not set. Comparison will be against latest analysis version of the Axivion Dashboard."
    @echo "If setting AXIVION_REFERENCE_DATE, its syntax must conform to ISO timestamps."
  )
)

if "%AXIVION%"=="" (
  set "AXIVION=C:\Program Files (x86)\Bauhaus"
)

if not exist "%AXIVION%" (
  @echo "Directory %AXIVION% does not exist. You need to set environment variable AXIVION to the directory where the Axivion suite is installed."
  goto error
)

@REM include AXIVION bin in exectuable path
if not exist "%AXIVION%\bin%" (
  @echo "Directory %AXIVION%\bin does not exist. You need to set environment variable AXIVION to the directory where the Axivion suite is installed having a bin subdirectory."
  goto error
)

set "PATH=%AXIVION%\bin;%PATH%"

@REM SEE project directory (generally the Jenkins workspace if this job is run
@REM by Jenkins, in which case WORKSPACE is set, or otherwise the current directory)
if "%WORKSPACE%" == "" (
  set "SEEDIRECTORY=%cd%"
) else (
  set "SEEDIRECTORY=%WORKSPACE%"
)

if not exist "%SEEDIRECTORY%" (
  @echo "Directory %SEEDIRECTORY% does not exist. You need to set environment variable SEEDIRECTORY to the directory where the SEE project resides."
  goto error
)

@REM where the Axivion configuration for SEE resides
if "%BAUHAUS_CONFIG%"=="" (
  set "BAUHAUS_CONFIG=%SEEDIRECTORY%\Axivion"
)

if not exist "%BAUHAUS_CONFIG%" (
  @echo "Directory %BAUHAUS_CONFIG% does not exist. You need to set environment variable BAUHAUS_CONFIG to the directory where the Axivion configuration for SEE resides."
  goto error
)

@REM where the Axivion dashserver configuration resides
if "%AXIVION_DASHBOARD_CONFIG%"=="" (
  set "AXIVION_DASHBOARD_CONFIG=C:\Users\koschke\Axivion"
)

if not exist "%AXIVION_DASHBOARD_CONFIG%" (
  @echo "Directory %AXIVION_DASHBOARD_CONFIG% does not exist. You need to set environment variable AXIVION_DASHBOARD_CONFIG to the directory where the Axivion dashboard configuration resides."
  goto error
)

if "%AXIVION_DATABASES_DIR%"=="" (
  set "AXIVION_DATABASES_DIR=%AXIVION_DASHBOARD_CONFIG%"
)

if not exist "%AXIVION_DATABASES_DIR%" (
  @echo "Directory %AXIVION_DATABASES_DIR% does not exist. You need to set environment variable AXIVION_DATABASES_DIR to the directory where the Axivion dashboard databases reside."
  goto error
)

@REM The Axivion dashboard server certificate can be downloaded using a
@REM brower. The certiciate file must contain the whole certificate chain.
if "%REQUESTS_CA_BUNDLE%"=="" (
  set "REQUESTS_CA_BUNDLE=%AXIVION_DASHBOARD_CONFIG%\cert\stvr2.crt"
)

if not exist "%REQUESTS_CA_BUNDLE%" (
  @echo "File %REQUESTS_CA_BUNDLE% does not exist. You need to set environment variable REQUESTS_CA_BUNDLE to the file containing the CA bundle for the Axivion dashboard."
  goto errormore
)

@REM URL of the dashserver
if "%AXIVION_DASHBOARD_URL%"=="" (
  @REM set "AXIVION_DASHBOARD_URL=https://localhost:9443/axivion/"
  set "AXIVION_DASHBOARD_URL=https://stvr2.informatik.uni-bremen.de:9443/axivion/"
)

if "%UNITY%"=="" (
  set "UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.53f1"
)

if not exist "%UNITY%" (
  @echo "Directory %UNITY% does not exist. You need to set environment variable UNITY to where Unity is installed."
  goto error
)

@echo AXIVION="%AXIVION%"
@echo SEEDIRECTORY="%SEEDIRECTORY%"
@echo AXIVION_DASHBOARD_CONFIG="%AXIVION_DASHBOARD_CONFIG%"
@echo AXIVION_DASHBOARD_URL="%AXIVION_DASHBOARD_URL%"
@echo UNITY="%UNITY%"

@REM If the dashserver is installed as a Windows service, you can
@REM start and stop it as follows:
@REM   net (start|stop) "axivion_dashboard_service"
@REM or use the Windows Services Console (services.msc).

@REM The Visual Studio .csproj files need to be created before we can start the build.
@"%UNITY%\Editor\Unity.exe" -batchmode -nographics -logFile - -executeMethod CITools.SolutionGenerator.Sync -projectPath . -quit
@REM "%UNITY%\Editor\Unity.exe" -batchmode -nographics -logFile - -executeMethod UnityEditor.SyncVS.SyncSolution -projectPath . -quit

@REM Count the number of command-line parameters
@setlocal enabledelayedexpansion
@set argCount=0
for %%x in (%*) do (
   set /A argCount+=1
)

@REM @echo "Number of processed arguments: %argCount%"

@REM Execute command line parameters if there are any.
IF %argCount% == 0 (
  @echo "No parameters to be executed given"
) ELSE (
  %*
)

:end

if not "%AXIVION_LOCAL_BUILD%"=="" (
  if "%AXIVION_REFERENCE_DATE%"=="" (
    @echo "Comparison is relative to the latest analysis version of the Axivion Dashboard."
  ) else (
    @echo "Comparison is relative to the analysis version at %AXIVION_REFERENCE_DATE% of the Axivion Dashboard."
  )
  @echo "Results of this local build can be found in "%userprofile%"/.bauhaus/localbuild"
  @echo "You can view the results as follows:"
  @echo "   %AXIVION%"\bin\dashserver start --local --install_file="%USERPROFILE%"\.bauhaus\localbuild\projects\SEE.db --noauth"
  @echo "The URL and the necessary credentials (in case you do not use --noauth) will be made available when you run the above command."
  @echo "When done, you can stop the Dashboard server as follows:"
  @echo "   %AXIVION%"\bin\dashserver stop --local"
)

:error

endlocal
