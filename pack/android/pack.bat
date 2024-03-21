@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

cd "%cd%"
git pull

dotnet publish "%cd%\..\..\src\PrivateCloud.Maui" -o "%cd%\..\..\src\PrivateCloud.Maui\bin\packages\android" -f net8.0-android -c Release -p:AndroidSigningKeyStore="%cd%\private.cloud.keystore" -p:AndroidSigningKeyPass=demo123 -p:AndroidSigningStorePass=demo123

pause