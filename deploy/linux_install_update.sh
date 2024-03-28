#! /bin/bash

position="$(cd "$(dirname "$0")" && pwd)"
cd $position

log(){
    echo -e "\033[32m $1 \033[0m"
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
    if ["$remoteVersion" = "0"];then
        log 'retry get remote version from github'
        getRemoteVersion 'https://raw.githubusercontent.com/yibei333/private-cloud/main/pack/version.txt'
    fi

    if ["$remoteVersion" = "0"];then
        log 'unable to get remote version'
        exit 1
    fi
    log 'remote version is $remoteVersion'

    if ["$remoteVersion" = "$localVersion"];then
        log 'already update to date'
        exit 0
    fi
}

getLocalVersion(){
    log 'getting local version'
    if test -f $1; then
        read -r localVersion < $1
    fi
	log "local version is $localVersion"
}

getRemoteVersion(){
    curl --ssl-no-revoke -L -f -#  --connect-timeout 10 -m 10 -o temp.txt $1
    if [$? -eq 0];then
        read -r remoteVersion < temp.txt
        removeFile temp.txt
    else
        log 'curl failed'
    fi
}

downloadPackage(){
    log "start downloading package with version:$remoteVersion"
    packageDownloaded=0
    downloadFile "https://gitee.com/developer333/private-cloud/releases/download/$remoteVersion/server.privatecloud.linux64.$remoteVersion.zip"
    if [$packageDownloaded -eq 0];then
        log 'retry get package from github'
        downloadFile "https://github.com/yibei333/private-cloud/releases/download/$remoteVersion/server.privatecloud.linux64.$remoteVersion.zip"
    fi
    if [$packageDownloaded -eq 0];then
        log 'log download package failed'
        exit 1
    fi
}

downloadFile(){
    log "url is $1"
    curl --ssl-no-revoke -L -f -# --connect-timeout 10 -m 300 -o package.tar.gz $1
    if [$? -eq 0];then
        log 'download success'
    else
        log 'curl failed'
        removeFile package.tar.gz
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

startProcess(){
    sudo chmod +x "$position/bin/PrivateCloud.Server"
    nohup "$position/bin/PrivateCloud.Server" > /dev/null 2>&1 &
}


localVersion="0"
remoteVersion="0"

checkVersion
downloadPackage
stopProcess
unPackPackage
startProcess
removeFile package.tar.gz
log 'complete'