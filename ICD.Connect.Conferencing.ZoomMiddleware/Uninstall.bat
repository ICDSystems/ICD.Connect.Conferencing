@echo off

%~dp0\ICD.Connect.Conferencing.ZoomMiddleware.exe uninstall --sudo

rmdir /s /q C:\ProgramData\ICD\ZoomMiddleware

PAUSE
