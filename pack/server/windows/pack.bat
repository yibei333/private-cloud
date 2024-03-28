@echo off
cd /d "%~dp0"

set scriptLocation=%cd%
set projectPath=%scriptLocation%\..\..\..\src\PrivateCloud.Server
set packagePath=%projectPath%\bin\packages
set binaryPath=%packagePath%\windows

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

cd "%scriptLocation%"
git pull

::https://learn.microsoft.com/en-us/dotnet/core/rid-catalog
dotnet publish "%projectPath%" -o "%binaryPath%" -r win-x64 -c Release --sc /p:PublishSingleFile=true

set versionPath="%binaryPath%\version.txt"
set /p version=<"%versionPath%"
set filename=%packagePath%\server.privatecloud.win64.%version%.zip

powershell Compress-Archive -Force -Path "%binaryPath%\*" -DestinationPath "%filename%"

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

::pause 