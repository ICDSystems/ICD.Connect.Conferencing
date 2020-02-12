@echo off

call "%~dp0\Uninstall.bat"

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
set TargetApplicationPath=%InstallPath%%Application%
set ServiceName=ICD Zoom Middleware Service

set ZoomUsername=zoom
set ZoomPassword=noth74ing
set ZoomPort=2244
set ListenAddress=0.0.0.0
set ListenPort=2245

echo --------------------------------------------------------------------------
echo Please enter values for the following configuration items
echo (Press return key to accept defaults)
echo --------------------------------------------------------------------------

set /p ZoomUsername=Zoom Username (Default Zoom):
set /p ZoomPassword=Zoom Password (Default noth74ing):
set /p ZoomPort=Zoom Port (Default 2244):
set /p ListenAddress=Listen Address (Default 0.0.0.0):
set /p ListenPort=Listen Port (Default 2245):

REM Copying installation files to the install path
echo --------------------------------------------------------------------------
echo Copying installation files to the install path
echo --------------------------------------------------------------------------
md "%InstallPath%" 2>nul
copy "%~dp0\*" "%InstallPath%"

REM installing the service
echo --------------------------------------------------------------------------
echo Installing the service
echo --------------------------------------------------------------------------
"%TargetApplicationPath%" install --sudo -zoomUsername="%ZoomUsername%" -zoomPassword="%ZoomPassword%" -zoomPort="%ZoomPort%" -listenAddress="%ListenAddress%" -listenPort="%ListenPort%"

REM adding the firewall rule
echo --------------------------------------------------------------------------
echo Adding the firewall rule
echo --------------------------------------------------------------------------
netsh advfirewall firewall add rule name="%ServiceName%" dir=in action=allow protocol=TCP localport="%ListenPort%" program="%TargetApplicationPath%"

REM starting the service
echo --------------------------------------------------------------------------
echo Starting the service
echo --------------------------------------------------------------------------
"%TargetApplicationPath%" start

PAUSE
