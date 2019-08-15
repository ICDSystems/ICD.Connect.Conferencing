@echo off

call "%~dp0\Uninstall.bat"

set InstallPath=C:\Program Files\ICD Systems\ZoomMiddleware\
set Application=ICD.Connect.Conferencing.ZoomMiddleware.exe
set LocalApplicationPath=%~dp0%Application%
set TargetApplicationPath=%InstallPath%%Application%
set ServiceName=ICD Zoom Middleware Service

set ZoomUsername=zoom
set ZoomPassword=noth74ing
set ZoomPort=2244
set ListenAddress=0.0.0.0
set ListenPort=2245

set /p ZoomUsername=Zoom Username:
set /p ZoomPassword=Zoom Password:
set /p ZoomPort=Zoom Port:
set /p ListenAddress=Listen Address:
set /p ListenPort=Listen Port:

REM Copying installation files to the install path
md "%InstallPath%" 2>nul
copy "%~dp0\*" "%InstallPath%"

Rem installing the service
"%TargetApplicationPath%" install --sudo -zoomUsername="%ZoomUsername%" -zoomPassword="%ZoomPassword%" -zoomPort="%ZoomPort%" -listenAddress="%ListenAddress%" -listenPort="%ListenPort%"

Rem adding the firewall rule
netsh advfirewall firewall add rule name="%ServiceName%" dir=in action=allow protocol=TCP localport="%ListenPort%" program="%TargetApplicationPath%"

REM starting the service
"%TargetApplicationPath%" start

PAUSE
