@echo off

cd /d "%~dp0"
set sourcePath=%cd%\..\src
set clientPath=%sourcePath%\PrivateCloud.Maui\bin\packages
set serverPath=%sourcePath%\PrivateCloud.Server\bin\packages
set targetPath=%cd%\packages

cd /d "%~dp0"
call "%cd%/server/windows/pack.bat"

cd /d "%~dp0"
call "%cd%/server/linux/pack.bat"

cd /d "%~dp0"
call "%cd%/clients/windows/pack.bat"

cd /d "%~dp0"
call "%cd%/clients/android/pack.bat"

cd /d "%~dp0"
xcopy /E /I "%clientPath%\*" "%targetPath%"
xcopy /E /I "%serverPath%\*" "%targetPath%"

IF EXIST "%clientPath%" (
    rd /s /q "%clientPath%"
) 
IF EXIST "%serverPath%" (
    rd /s /q "%serverPath%"
) 

pause 