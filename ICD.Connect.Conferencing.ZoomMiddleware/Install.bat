@echo off

set InstallPath = "C:\Program Files\ICD Systems\ZoomMiddleware"
set Application = "ICD.Connect.Conferencing.ZoomMiddleware.exe"
set ZoomUsername = "zoom"
set ZoomPassword = "zoomus123"
set ZoomPort = 2244
set ListenAddress = "0.0.0.0"
set ListenPort = 2245

%~dp0\%Application% uninstall --sudo
rmdir /s /q %InstallPath%

set /p ZoomUsername=Zoom Username:
set /p ZoomPassword=Zoom Password:
set /p ZoomPort=Zoom Port:
set /p ListenAddress=Listen Address:
set /p ListenPort=Listen Port:

md %InstallPath% 2>nul
copy %~dp0\* %InstallPath%

%InstallPath%\%Application% install --sudo -zoomUsername=%ZoomUsername% -zoomPassword=%ZoomPassword% -zoomPort=%ZoomPort% -listenAddress=%ListenAddress% -listenPort=%ListenPort%

PAUSE
