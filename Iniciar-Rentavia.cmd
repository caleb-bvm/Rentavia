@echo off
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Iniciar-Rentavia.ps1" %*
set "RENTAVIA_EXIT_CODE=%ERRORLEVEL%"
if not "%RENTAVIA_EXIT_CODE%"=="0" pause
exit /b %RENTAVIA_EXIT_CODE%
