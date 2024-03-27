@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

set localVersion=0
set remoteVersion=0
set serviceName=private.cloud

call:checkVersion

call:downloadPackage

call:removeOldService

call:unPackPackage

call:installAndStartService

call:openBrowser

pause

:log
	echo [32m *%* [37m
goto:eof

:checkVersion
	call:log check version
	call:getLocalVersion %cd%\bin\version.txt
	
	call:log getting remote version
	call:getRemoteVersion https://gitee.com/developer333/private-cloud/raw/main/pack/version.txt
	if %remoteVersion% == 0 (
		call:log retry get remote version from github
		call:getRemoteVersion https://raw.githubusercontent.com/yibei333/private-cloud/main/pack/version.txt
	)
	if %remoteVersion% == 0 (
		call:log unable to get remote version
		pause
		exit 1
	)
	call:log remote version is %remoteVersion%

	if %remoteVersion% == %localVersion% (
		call:log already update to date
		pause
		exit 0
	)
goto:eof

:getLocalVersion
	call:log getting local version
	if exist %1 (
		set /p localVersion=<%1
	) 
	call:log local version is %localVersion%
goto:eof

:getRemoteVersion
	curl --ssl-no-revoke -L -f -#  --connect-timeout 10 -m 10 -o temp.txt %1
	if %ERRORLEVEL% == 0 (
		set /p remoteVersion=<temp.txt
		del temp.txt
	) || (
		call:log curl failed
	)
goto:eof

:downloadPackage
	call:log start downloading package with version:%remoteVersion%
	set packageDownloaded=0
	call:downloadFile https://gitee.com/developer333/private-cloud/releases/download/%remoteVersion%/server.privatecloud.linux64.%remoteVersion%.zip
	if %packageDownloaded% == 0 (
		call:log retry get package from github
		call:downloadFile https://github.com/yibei333/private-cloud/releases/download/%remoteVersion%/server.privatecloud.win64.%remoteVersion%.zip
	)
	if %packageDownloaded% == 0 (
		call:log download package failed
		pause
		exit 0
	)
goto:eof

:downloadFile
	call:log url is '%1'
	curl --ssl-no-revoke -L -f -# --connect-timeout 10 -m 300 -o package.zip %1

	if %ERRORLEVEL% == 0 (
    		call:log download success
		set packageDownloaded=1
	) || (
    		call:log curl failed
		del package.zip
	)
goto:eof

:removeOldService
	call:log stop and delete old service
	sc query %serviceName% > nul
	if %ERRORLEVEL% == 0 (
		sc stop %serviceName% > nul
		sc delete %serviceName% > nul
	)
goto:eof

:unPackPackage
	call:log unpack package
	if exist bin\appsettings.Other.json (
		copy bin\appsettings.Other.json bin\appsettings.Other.json.bak
	)
	powershell Expand-Archive -Force -Path package.zip -DestinationPath bin
	if exist bin\appsettings.Other.json.bak (
		copy bin\appsettings.Other.json.bak bin\appsettings.Other.json
	)
goto:eof

:installAndStartService
	call:log install and start service
	sc create %serviceName% binPath="%cd%\bin\PrivateCloud.Server.exe" start=auto
	sc start %serviceName%
goto:eof

:openBrowser
	call:log open browser
	start http://localhost:9090
goto:eof