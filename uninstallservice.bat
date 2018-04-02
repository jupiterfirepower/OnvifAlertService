set serviceName=OnvifAlertService

sc query %serviceName% | find "does not exist" > nul
if %ERRORLEVEL% EQU 1 (
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" stop
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" uninstall
)
