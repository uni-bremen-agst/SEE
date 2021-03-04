REM This batch file sets the various environment variables needed to 
REM run the Axivion Suite tools and then executes the command line parameters
REM passed to this batch file. If there are none, only the variables are
REM set.

REM user workspace (generally the home directory or the Jenkins workspace)
set "USERWORKSPACE=%WORKSPACE%"

REM Python
set "PATH=C:\Users\SWT\AppData\Local\Programs\Python\Python38;%PATH%"
set "BAUHAUS_PYTHON=C:\Users\SWT\AppData\Local\Programs\Python\Python38\python.exe"

REM include Bauhaus bin in exectuable path
set "PATH=C:\Program Files (x86)\Bauhaus\bin;%PATH%"

REM SEE project directory
set "SEEDIRECTORY=%USERWORKSPACE%"

REM where the Axivion configuration resides within SEE
set "BAUHAUS_CONFIG=%SEEDIRECTORY%\Axivion"

REM where the Axivion dashserver configuration resides
set "SWT=C:\Users\SWT"
set "AXIVION_DASHBOARD_CONFIG=%SWT%\Axivion"
set "AXIVION_DATABASES_DIR=%AXIVION_DASHBOARD_CONFIG%"
set "REQUESTS_CA_BUNDLE=%AXIVION_DASHBOARD_CONFIG%\cert\auto.crt"

REM URL of the dashserver
REM set "AXIVION_DASHBOARD_URL=https://localhost:9443/axivion/"
set "AXIVION_DASHBOARD_URL=https://swt-jenkins.informatik.uni-bremen.de:9443/axivion/"

REM If the dashserver is installed as a Windows service, you can
REM start and stop it as follows:
REM   net (start|stop) "axivion_dashboard_service"
REM or use the Windows Services Console (services.msc).

REM The Visual Studio .csproj files need to be created before we can start the build.
"C:\Program Files\Unity\Hub\Editor\2019.4.21f1\Editor\Unity.exe" -batchmode -nographics -logFile - -executeMethod UnityEditor.SyncVS.SyncSolution -projectPath . -quit

REM execute command line parameters
%*

REM No-error exit code so that Jenkins is happy
exit /b 0

