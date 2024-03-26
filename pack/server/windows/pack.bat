@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

set projectPath=%cd%\..\..\..\src\PrivateCloud.Server
set packagePath=%projectPath%\bin\packages
set binaryPath=%packagePath%\windows
set versionPath=%cd%\..\..\version.txt
set /p version=<"%versionPath%"
set filename=%packagePath%\server.privatecloud.win64.%version%.zip

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

cd "%cd%"
git pull

::https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
dotnet publish "%projectPath%" -o "%binaryPath%" -r win-x64 -c Release --sc /p:PublishSingleFile=true

copy "%versionPath%" "%binaryPath%"

powershell Compress-Archive -Force -LiteralPath "%binaryPath%" -DestinationPath "%filename%"

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

pause 