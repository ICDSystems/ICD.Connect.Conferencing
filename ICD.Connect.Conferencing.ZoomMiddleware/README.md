# README

## Introduction
The **Zoom Middleware Service** is a load-balancing application that provides an SSH to TCP passthrough to facilitate improved performance on slower client applications.

## Uninstallation
To uninstall simply run the **Unistall.bat** Windows Batch file as an **administrator**. This will remove the Middleware service, firewall rule, application and related files from the system.

## Installation
To begin the installation process run the **Install.bat** Windows Batch file as an **administrator**. The batch file will run throught the following steps and prompt the user for input:

1. The installation process will attempt to uninstall an existing version of the software. This step will fail if there is no existing version of the software.
2. The installer will ask the user to specify a number of configuration parameters. Pressing return without specifying a value will set the parameter to its default:
	* **Zoom Username** - The username configured in the Zoom software (defaults to **zoom**)
	* **Zoom Password** - The password configured in the Zoom software (defaults to **noth74ing**)
	* **Zoom Port** - The port that the Zoom software's SSH server is listening on (defaults to **2244**)
	* **Listen Address** - The control processor client IP that will be accepted by the middleware (defaults to all IPs: **0.0.0.0**)
	* **Listen Port** - The port that the middleware TCP server is hosted on (defaults to **2245**)
3. The installer will copy all of the application files to the install path (**C:\Program Files\ICD Systems\ZoomMiddleware**)
4. The installer will setup the **ICD Zoom Middleware Service** as a Windows service set to **autostart**.
5. The installer will create a Windows **Firewall Rule**
6. Finally the service will be started.

## Troubleshooting
The Zoom Middleware Service will log any warnings or errors to **C:\ProgramData\ICD Systems\Logs**

**Zoom will reject all connections unless an account has been signed in.**

A TCP console is hosted at the middleware listen port + 1 (defaults to 2246)
