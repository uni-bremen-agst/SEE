:: Stops the Axivion Dashboard.

@setlocal

@where /q dashserver || (
    @echo Axivion Suite not in PATH. Please run absvars.bat before calling %0
    @exit /b 1
)

set "AXIVION_DASHBOARD_CONFIG=%~dp0config"
dashserver stop
@exit /b %ERRORLEVEL%
