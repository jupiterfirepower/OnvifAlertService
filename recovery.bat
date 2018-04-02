SETLOCAL ENABLEDELAYEDEXPANSION
@echo on
set host=%COMPUTERNAME%
echo %host%

for /f "tokens=2 delims=:" %%a in ('sc.exe queryex "3deYeAlertService" ^| find /i "PID"') do set MyPID=%%a

copy /y nul "C:\Apps\AlertService\logs\recoveryrestart.evt" > nul
set serviceName=3deYeAlertService
echo off
sc query %serviceName% | find "does not exist" > nul
if %ERRORLEVEL% EQU 0 (
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" install --delayed
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" start
)
if %ERRORLEVEL% EQU 1 (
taskkill.exe /f /PID %MyPID%
sc delete %serviceName%
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" install --delayed
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" start
)
