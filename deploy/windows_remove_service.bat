@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

set serviceName=private.cloud

call:removeOldService
call:log complete
pause

:log
	echo [32m *%* [37m
goto:eof

:removeOldService
	call:log stop and delete service
	sc query %serviceName% > nul
	if %ERRORLEVEL% == 0 (
		sc stop %serviceName% > nul
		sc delete %serviceName% > nul
	)
goto:eof
