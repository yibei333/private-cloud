#! /bin/bash

position="$(cd "$(dirname "$0")" && pwd)"
cd $position

log(){
    echo -e "\033[32m$1\033[0m"
}

removeFile(){
    if [ -f $1 ];then
        rm $1
    fi
}

checkVersion(){
	getLocalVersion ./bin/version.txt

    log 'getting remote version'
    getRemoteVersion 'https://gitee.com/developer333/private-cloud/raw/main/pack/version.txt'
    if [ "$remoteVersion" = "0" ];then
        log 'retry get remote version from github'
        getRemoteVersion 'https://raw.githubusercontent.com/yibei333/private-cloud/main/pack/version.txt'
    fi

    if [ "$remoteVersion" = "0" ];then
        log 'unable to get remote version'
        exit 1
    fi
    log "remote version is $remoteVersion"

    if [ "$remoteVersion" = "$localVersion" ];then
        log 'already update to date'
        log 'restart process'
        restartProcess
        exit 0
    fi
}

getLocalVersion(){
    log 'getting local version'
    if [ -f $1 ]; then
        read -r localVersion < $1
    fi
	log "local version is $localVersion"
}

getRemoteVersion(){
    curl --ssl-no-revoke -L -f -#  --connect-timeout 10 -m 10 -o temp.txt $1
    if [ $? -eq 0 ];then
        read -r remoteVersion < temp.txt
        removeFile temp.txt
    else
        log 'curl failed'
    fi
}

downloadPackage(){
    log "start downloading package with version:$remoteVersion"
    downloadFile "https://gitee.com/developer333/private-cloud/releases/download/$remoteVersion/server.privatecloud.linux64.$remoteVersion.tar.gz" "package.tar.gz"
    if [ $fileDownloaded -eq 0 ];then
        log 'retry get package from github'
        downloadFile "https://github.com/yibei333/private-cloud/releases/download/$remoteVersion/server.privatecloud.linux64.$remoteVersion.tar.gz" "package.tar.gz"
    fi
    if [ $fileDownloaded -eq 0 ];then
        log 'download package failed'
        exit 1
    fi
}

downloadFile(){
    log "download url is $1"
    curl --ssl-no-revoke -L -f -# --connect-timeout 10 -m 300 -o $2 $1
    if [ $? -eq 0 ];then
        log 'download success'
        fileDownloaded=1
    else
        log 'curl failed'
        removeFile $2
        fileDownloaded=0
    fi
}

stopProcess(){
    p="$(ps -ef|grep [P]rivateCloud.Server|awk '{print $2}')"
    if [[ ! -z $p ]];then
        kill $p
    fi
}

unPackPackage(){
    log 'unpack package'
    if [ -f 'bin\appsettings.Other.json' ];then
        copy 'bin\appsettings.Other.json' 'bin\appsettings.Other.json.bak'
    fi

    mkdir -p bin
    tar -xf package.tar.gz -C bin

    if [ -f 'bin\appsettings.Other.json.bak' ];then
        copy 'bin\appsettings.Other.json.bak' 'bin\appsettings.Other.json'
    fi
}

setFfmpeg(){
    log "start downloading ffmepg"
    downloadFile "https://gitee.com/developer333/private-cloud/releases/download/$remoteVersion/ffmpeg.linux.tar.xz" "ffmpeg.linux.tar.xz"
    if [ $fileDownloaded -eq 0 ];then
        log 'retry get ffmpeg from github'
        downloadFile "https://github.com/yibei333/private-cloud/releases/download/$remoteVersion/ffmpeg.linux.tar.xz" "ffmpeg.linux.tar.xz"
    fi
    if [ $fileDownloaded -eq 0 ];then
        log 'download ffmpeg failed'
        exit 1
    fi

    mkdir -p bin/data/ffmpeg
    tar -xf ffmpeg.linux.tar.xz -C bin/data/ffmpeg
    sudo chmod +x bin/data/ffmpeg/ffmpeg
}

startProcess(){
    sudo chmod +x "$position/bin/PrivateCloud.Server"
    nohup "$position/bin/PrivateCloud.Server" > /dev/null 2>&1 &
}

restartProcess(){
    stopProcess
    nohup "$position/bin/PrivateCloud.Server" > /dev/null 2>&1 &
}

clean(){
    removeFile temp.txt
    removeFile package.tar.gz
    removeFile ffmpeg.linux.tar.xz
}

localVersion="0"
remoteVersion="0"
fileDownloaded=0

checkVersion
downloadPackage
stopProcess
unPackPackage
setFfmpeg
startProcess
clean
log 'complete'