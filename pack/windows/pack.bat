@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

cd "%cd%"
git pull

dotnet publish "%cd%\..\..\src\PrivateCloud.Maui" -o "%cd%\..\..\src\PrivateCloud.Maui\bin\packages\windows" -f net8.0-windows10.0.19041.0 -c Release -p:RuntimeIdentifierOverride=win10-x64 -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true -p:RuntimePackAlwaysCopyLocal=true --sc

pause