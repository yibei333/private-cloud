@echo off
cd /d "%~dp0"

set projectPath=%cd%\..\..\..\src\PrivateCloud.Maui
set binaryPath=%projectPath%\bin\packages\windows
set innoSetupPath=%cd%\..\..\InnoSetup6/ISCC.exe
set versionPath=%cd%\..\..\version.txt
set /p version=<"%versionPath%"

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

cd "%cd%"
git pull

dotnet publish "%projectPath%" -o "%binaryPath%" -f net8.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None --sc

copy EnsureWebview2RuntimeInstalled.exe "%binaryPath%"

"%innoSetupPath%" /DMyAppVersion=%version% installer.iss

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

::pause 