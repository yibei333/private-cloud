@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

set projectPath=%cd%\..\..\..\src\PrivateCloud.Maui
set packagePath=%projectPath%\bin\packages
set binaryPath=%packagePath%\android
set versionPath=%cd%\..\..\version.txt
set /p version=<"%versionPath%"

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

cd "%cd%"
git pull

dotnet publish "%projectPath%" -o "%binaryPath%" -f net8.0-android -c Release -p:AndroidSigningKeyStore="%cd%\demo.keystore" -p:AndroidSigningKeyAlias=private.cloud -p:AndroidSigningKeyPass=demo123 -p:AndroidSigningStorePass=demo123

copy %binaryPath%\com.yibei.privatecloud-Signed.apk "%packagePath%\clients.privatecloud.android.%version%.apk"

IF EXIST "%binaryPath%" (
    rd /s /q "%binaryPath%"
) 

pause 