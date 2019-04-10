@echo off

set InstallPath = "C:\Program Files\ICD Systems\ZoomMiddleware"
set Application = "ICD.Connect.Conferencing.ZoomMiddleware.exe"

%~dp0\%Application% uninstall --sudo
rmdir /s /q %InstallPath%

PAUSE
