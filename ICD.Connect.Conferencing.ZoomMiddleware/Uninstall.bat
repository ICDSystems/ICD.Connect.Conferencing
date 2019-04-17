@echo off

set InstallPath=C:\Program Files\ICD Systems\ZoomMiddleware\
set Application=ICD.Connect.Conferencing.ZoomMiddleware.exe
set LocalApplicationPath=%~dp0%Application%
set ServiceName=ICD Zoom Middleware Service

REM Uninstalling the existing application
"%LocalApplicationPath%" uninstall --sudo
rmdir /s /q "%InstallPath%"

REM Removing the existing firewall rule
netsh advfirewall firewall delete rule name="%ServiceName%"

PAUSE
