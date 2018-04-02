@echo off
set serviceName=OnvifAlertService
SC QUERY %serviceName% | FIND "STATE" | FIND "RUNNING" > nul
exit /b %errorlevel% 
  