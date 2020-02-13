@echo off

echo --------------------------------------------------------------------------
echo Administrative permissions required. Detecting permissions...
echo --------------------------------------------------------------------------
net session >nul 2>&1
if %errorLevel% == 0 (
	echo Success: Administrative permissions confirmed.
) else (
	echo Failure: Current permissions inadequate.
	PAUSE
	EXIT
)

set InstallPath=C:\Program Files\ICD Systems\ZoomMiddleware\
set Application=ICD.Connect.Conferencing.ZoomMiddleware.exe
set LocalApplicationPath=%~dp0%Application%
set ServiceName=ICD Zoom Middleware Service

REM Uninstalling the existing application
echo --------------------------------------------------------------------------
echo Attempting to uninstall existing application
echo NOTE - Uninstall step will FAIL if there is no existing version of the software.
echo --------------------------------------------------------------------------
"%LocalApplicationPath%" uninstall --sudo
rmdir /s /q "%InstallPath%"

REM Removing the existing firewall rule
echo --------------------------------------------------------------------------
echo Attempting to delete existing firewall rules
echo NOTE - Deletion step will FAIL if there is no existing firewall rule.
echo --------------------------------------------------------------------------
netsh advfirewall firewall delete rule name="%ServiceName%"
netsh advfirewall firewall delete rule name="%ServiceName% Console"

PAUSE
