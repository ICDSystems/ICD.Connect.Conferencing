@echo off

set InstallPath="C:\Program Files\ICD Systems\ZoomMiddleware\"
set Application="ICD.Connect.Conferencing.ZoomMiddleware.exe"
set LocalApplicationPath=%~dp0%Application%
set TargetApplicationPath=%InstallPath%%Application%
set ServiceName="ICD Zoom Middleware Service"

set ZoomUsername="zoom"
set ZoomPassword="zoomus123"
set ZoomPort=2244
set ListenAddress="0.0.0.0"
set ListenPort=2245

%LocalApplicationPath% uninstall --sudo
rmdir /s /q %InstallPath%

netsh advfirewall firewall delete rule name=%ServiceName%

set /p ZoomUsername=Zoom Username:
set /p ZoomPassword=Zoom Password:
set /p ZoomPort=Zoom Port:
set /p ListenAddress=Listen Address:
set /p ListenPort=Listen Port:

md %InstallPath% 2>nul
copy %~dp0\* %InstallPath%

%TargetApplicationPath% install --sudo -zoomUsername=%ZoomUsername% -zoomPassword=%ZoomPassword% -zoomPort=%ZoomPort% -listenAddress=%ListenAddress% -listenPort=%ListenPort%

netsh advfirewall firewall show rule name=%ServiceName% >nul
if ERRORLEVEL 1 (
    netsh advfirewall firewall add rule name=%ServiceName% dir=in action=allow protocol=TCP localport=%ListenPort% service=%ServiceName%
)

sc start %ServiceName%

PAUSE
