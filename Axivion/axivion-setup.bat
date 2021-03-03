REM This batch file sets the various environment variables needed to 
REM run the Axivion Suite tools and then executes the command line parameters
REM passed to this batch file. If there are none, only the variables are
REM set.

REM Python
set "PATH=C:\Users\SWT\AppData\Local\Programs\Python\Python38;%PATH%"
set "BAUHAUS_PYTHON=C:\Users\SWT\AppData\Local\Programs\Python\Python38\python.exe"

REM include Bauhaus bin in exectuable path
set "PATH=C:\Program Files (x86)\Bauhaus\bin;%PATH%"

REM set "BAUHAUS_CONFIG=%userprofile%\SEE\Axivion"
set "BAUHAUS_CONFIG=%WORKSPACE%\Axivion"

REM set "AXIVIONCI=%userprofile%\Axivion"
set "AXIVIONCI=C:\Users\SWT\Axivion"
set "AXIVION_DASHBOARD_CONFIG=%AXIVIONCI%"
set "AXIVION_DATABASES_DIR=%AXIVIONCI%"
set "REQUESTS_CA_BUNDLE=%AXIVION_DASHBOARD_CONFIG%\cert\auto.crt"

REM specific to this machine
REM set "AXIVION_DASHBOARD_URL=https://localhost:9443/axivion/"
set "AXIVION_DASHBOARD_URL=https://swt-jenkins.informatik.uni-bremen.de:9443/axivion/"

REM execute command line parameters
%*
