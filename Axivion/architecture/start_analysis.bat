:: Runs the Axivion CI or its configuration (the latter if and only
:: if command-line parameter config was set).

@setlocal

@where /q cafeCC || (
    @echo Axivion Suite not in PATH. Please run absvars.bat before calling %0
    @exit /b 1
)

set "PYTHONPATH=%PYTHONPATH%;%~dp0rules"
set "AXIVION_PROJECT_NAME=SEE"
set "AXIVION_DASHBOARD_URL=http://localhost:9090/axivion"
set "AXIVION_DATABASES_DIRECTORY=%~dp0dashboard\databases"
set "BAUHAUS_CONFIG=%~dp0config\architecture_config.json;%~dp0config\axivion_config.json"
if "%1" == "config" (
    axivion_config || exit /b 1
) else (
    axivion_ci %* || exit /b 1
)
