REM include Bauhaus bin in exectuable path
set "PATH=C:\Program Files (x86)\Bauhaus\bin;%PATH%"

set "BAUHAUS_CONFIG=%userprofile%\SEE\Axivion"
set "AXIVION_DASHBOARD_CONFIG=%userprofile%\Axivion"
set "AXIVION_DATABASES_DIR=%userprofile%\Axivion"
set "REQUESTS_CA_BUNDLE=%AXIVION_DASHBOARD_CONFIG%\cert\auto.crt"

REM specific to this machine
REM set "AXIVION_DASHBOARD_URL=https://localhost:9443/axivion/"
set "AXIVION_DASHBOARD_URL=https://swt-jenkins.informatik.uni-bremen.de:9443/axivion/"

REM execute command line parameters
%*
