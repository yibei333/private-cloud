@echo off
cd /d "%~dp0"

set scriptLocation=%cd%
set projectPath=%scriptLocation%\..\..\..\src\PrivateCloud.Server
set packagePath=%projectPath%\bin\packages
set binaryPath=%packagePath%\linux

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

cd "%scriptLocation%"
git pull

::https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
dotnet publish "%projectPath%" -o "%binaryPath%" -r linux-x64 -c Release --sc /p:PublishSingleFile=true

set versionPath="%binaryPath%\version.txt"
set /p version=<"%versionPath%"
set filename=%packagePath%\server.privatecloud.linux64.%version%.tar.gz

cd %binaryPath%
tar -czf %filename% *

echo "%scriptLocation%"
cd "%scriptLocation%"
IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

::pause 