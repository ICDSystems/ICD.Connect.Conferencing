@echo off

set InstallPath="C:\Program Files\ICD Systems\ZoomMiddleware\"
set Application="ICD.Connect.Conferencing.ZoomMiddleware.exe"
set LocalApplicationPath=%~dp0%Application%

%LocalApplicationPath% uninstall --sudo
rmdir /s /q %InstallPath%

PAUSE
