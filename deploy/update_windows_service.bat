@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

git pull

sc stop private.cloud

dotnet publish "%cd%\..\src\PrivateCloud.Server" -c Release -o "%cd%\..\bin"

sc start private.cloud

pause