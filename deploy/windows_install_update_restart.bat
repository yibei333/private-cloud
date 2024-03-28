@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

set localVersion=0
set remoteVersion=0
set serviceName=private.cloud
set fileDownloaded=0

call:checkVersion

call:downloadPackage

call:removeOldService

call:unPackPackage

call:setFfmpeg

call:installAndStartService

call:openBrowser

call:clean

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
		call:restartService
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

:restartService
	call:log restart service
	sc stop %serviceName%
	sc start %serviceName%
goto:eof

:downloadPackage
	call:log start downloading package with version:%remoteVersion%
	call:downloadFile https://gitee.com/developer333/private-cloud/releases/download/%remoteVersion%/server.privatecloud.win64.%remoteVersion%.zip package.zip
	if %fileDownloaded% == 0 (
		call:log retry get package from github
		call:downloadFile https://github.com/yibei333/private-cloud/releases/download/%remoteVersion%/server.privatecloud.win64.%remoteVersion%.zip package.zip
	)
	if %fileDownloaded% == 0 (
		call:log download package failed
		pause
		exit 1
	)
goto:eof

:downloadFile
	call:log download url is '%1'
	curl --ssl-no-revoke -L -f -# --connect-timeout 10 -m 300 -o %2 %1

	if %ERRORLEVEL% == 0 (
		call:log download file success
		set fileDownloaded=1
	) || (
		call:log curl failed
		del %2
		fileDownloaded=0
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

:setFfmpeg
	if exist bin/data/ffmpeg/ffmpeg.exe (
		exit /b 0
	)
	call:log start downloading ffmpeg
	call:downloadFile https://gitee.com/developer333/private-cloud/releases/download/%remoteVersion%/ffmpeg.windows.zip ffmpeg.windows.zip

	if %fileDownloaded% == 0 (
		call:log retry get ffmpeg from github
		call:downloadFile https://github.com/yibei333/private-cloud/releases/download/%remoteVersion%/ffmpeg.windows.zip ffmpeg.windows.zip
	)
	if %fileDownloaded% == 0 (
		call:log download ffmpeg failed
		pause
		exit 1
	)

	powershell Expand-Archive -Force -Path ffmpeg.windows.zip -DestinationPath bin/data/ffmpeg
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

:removeFile
	if exist %1 (
		del %1
	)
goto:eof

:clean
	call:removeFile temp.txt
	call:removeFile package.zip
	call:removeFile ffmpeg.windows.zip
goto:eof