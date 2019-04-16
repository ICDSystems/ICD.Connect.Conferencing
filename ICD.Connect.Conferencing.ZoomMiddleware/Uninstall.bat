@echo off

set InstallPath="C:\Program Files\ICD Systems\ZoomMiddleware\"
set Application="ICD.Connect.Conferencing.ZoomMiddleware.exe"
set LocalApplicationPath=%~dp0%Application%
set ServiceName="ICD Zoom Middleware Service"

%LocalApplicationPath% uninstall --sudo
rmdir /s /q %InstallPath%

netsh advfirewall firewall delete rule name=%ServiceName%

PAUSE
