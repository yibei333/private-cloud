@echo off
cd /d "%~dp0"

set projectPath=%cd%\..\..\..\src\PrivateCloud.Server
set packagePath=%projectPath%\bin\packages
set binaryPath=%packagePath%\linux
set versionPath=%cd%\..\..\version.txt
set /p version=<"%versionPath%"
set filename=%packagePath%\server.privatecloud.linux64.%version%.zip

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

cd "%cd%"
git pull

::https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
dotnet publish "%projectPath%" -o "%binaryPath%" -r linux-x64 -c Release --sc /p:PublishSingleFile=true

copy "%versionPath%" "%binaryPath%"

powershell Compress-Archive -Force -LiteralPath "%binaryPath%" -DestinationPath "%filename%"

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

::pause 