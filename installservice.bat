set serviceName=OnvifAlertService

sc query %serviceName% | find "does not exist" > nul
if %ERRORLEVEL% EQU 0 (
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" install --delayed
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" start

)
if %ERRORLEVEL% EQU 1 (
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" uninstall
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" install --delayed
"C:\Apps\AlertService\Onvif.Services.AlertService.exe" start
)