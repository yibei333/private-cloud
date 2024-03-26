@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

set projectPath=%cd%\..\..\..\src\PrivateCloud.Maui
set binaryPath=%projectPath%\bin\packages\windows

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

cd "%cd%"
git pull

dotnet publish "%projectPath%" -o "%binaryPath%" -f net8.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None --sc

copy EnsureWebview2RuntimeInstalled.exe "%binaryPath%"

set /p version=<"../../version.txt"

"InnoSetup6/ISCC.exe" /DMyAppVersion=%version% installer.iss

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

pause 