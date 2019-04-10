@echo off

%~dp0\ICD.Connect.Conferencing.ZoomMiddleware.exe uninstall --sudo

rmdir /s /q C:\ProgramData\ICD\ZoomMiddleware

set ZoomUsername=zoom
set ZoomPassword=zoomus123
set ZoomPort=2244
set ListenAddress=0.0.0.0
set ListenPort=2245

set /p ZoomUsername=Zoom Username:
set /p ZoomPassword=Zoom Password:
set /p ZoomPort=Zoom Port:
set /p ListenAddress=Listen Address:
set /p ListenPort=Listen Port:

md C:\ProgramData\ICD\ZoomMiddleware 2>nul
copy %~dp0\* C:\ProgramData\ICD\ZoomMiddleware

C:\ProgramData\ICD\ZoomMiddleware\ICD.Connect.Conferencing.ZoomMiddleware.exe install --sudo -zoomUsername=%ZoomUsername% -zoomPassword=%ZoomPassword% -zoomPort=%ZoomPort% -listenAddress=%ListenAddress% -listenPort=%ListenPort%

PAUSE
