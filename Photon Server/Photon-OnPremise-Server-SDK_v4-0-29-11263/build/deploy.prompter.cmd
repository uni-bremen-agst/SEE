@echo off
:: The available verbosity levels are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]
set verbosity=minimal
set buildfile=deploy.proj
set configuration=Debug

:start
cls
echo.
echo            ************************************************************
echo            *                     Build Prompt                         *
echo            ************************************************************
echo.
echo            Build and Copy Server Binaries To Deploy Folder
echo            Configuration: %configuration%
echo            Buildfile:     %buildfile%
echo.
echo            1.  Lite
echo            2.  Counter Publisher
echo            3.  Mmo Demo
echo            4.  Loadbalancing
echo.
echo            9.  All
echo.
echo            0.  Exit
echo.

:begin
IF NOT EXIST .\log\ MD .\log
set eof=
set choice=
set /p choice=Enter option 
if not '%choice%'=='' set choice=%choice:~0,1%
if '%choice%'=='1' call :lite
if '%choice%'=='2' call :counterpublisher
if '%choice%'=='3' call :mmo
if '%choice%'=='4' call :loadbalancing
if '%choice%'=='9' call :buildall
if '%choice%'=='0' goto eof
if '%eof%'=='' ECHO "%choice%" is not valid please try again
if '%eof%'=='' goto begin
pause
goto start

:lite
set rootpath=Lite
set slnfile=Lite.sln
set binpath=Lite\\
set dst=Lite
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %buildfile% /verbosity:%verbosity% /l:FileLogger,Microsoft.Build.Engine;logfile=log\%rootpath%Build.log;verbosity=%verbosity%;performancesummary /property:slnfile="%slnfile%" /property:binp="%binpath%" /property:rootpath="%rootpath%" /property:dst="%dst%" /t:BuildAndCopyForDeploy
goto done

:counterpublisher
set rootpath=CounterPublisher
set slnfile=CounterPublisher.sln
set binpath=
set dst=CounterPublisher
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %buildfile% /verbosity:%verbosity% /l:FileLogger,Microsoft.Build.Engine;logfile=log\%rootpath%Build.log;verbosity=%verbosity%;performancesummary /property:slnfile="%slnfile%" /property:binp="%binpath%" /property:rootpath="%rootpath%" /property:dst="%dst%" /t:BuildAndCopyForDeploy
goto done

:mmo
set rootpath=Mmo
set slnfile=Photon.Mmo.sln
set binpath=Photon.MmoDemo.Server\\
set dst=MmoDemo
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %buildfile% /verbosity:%verbosity% /l:FileLogger,Microsoft.Build.Engine;logfile=log\%rootpath%Build.log;verbosity=%verbosity%;performancesummary /property:slnfile="%slnfile%" /property:binp="%binpath%" /property:rootpath="%rootpath%" /property:dst="%dst%" /t:BuildAndCopyForDeploy
goto done

:loadbalancing
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %buildfile% /verbosity:%verbosity% /l:FileLogger,Microsoft.Build.Engine;logfile=log\LoadbalancingBuild.log;verbosity=%verbosity%;performancesummary /property:Configuration="%configuration%" /t:BuildLoadbalancing
goto done

:buildall
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe %buildfile% /verbosity:%verbosity% /l:FileLogger,Microsoft.Build.Engine;logfile=log\Build.log;verbosity=%verbosity%;performancesummary /property:Configuration="%configuration%" /t:BuildAndCopyForDeployComplete

REM call :lite
REM call :litelobby
REM call :policy
REM call :counterpublisher
REM call :mmo

:done
:eof
set eof=1
