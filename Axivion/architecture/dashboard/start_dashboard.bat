:: Starts the Axivion Dashboard.

@setlocal

@where /q dashserver || (
    @echo Axivion Suite not in PATH. Please run absvars.bat before calling %0
    @pause
    @exit /b 1
)

if not exist %~dp0databases (
    mkdir "%~dp0databases"
)

set "AXIVION_DASHBOARD_CONFIG=%~dp0config"
dashserver start
@if errorlevel 1 (
    @set _errorlevel=%ERRORLEVEL%
    @echo %0: error %_errorlevel%
    @pause
    @exit /b %_errorlevel%
)
