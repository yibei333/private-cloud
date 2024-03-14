@echo off
%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit
cd /d "%~dp0"

git pull
dotnet publish "%cd%\..\src\PrivateCloud.Server" -c Release -o "%cd%\..\bin"

sc create private.cloud binPath="%cd%\..\bin\PrivateCloud.Server.exe" start=auto
sc start private.cloud

pause