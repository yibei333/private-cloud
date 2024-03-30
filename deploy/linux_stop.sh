#! /bin/bash

position="$(cd "$(dirname "$0")" && pwd)"
cd $position

log(){
    echo -e "\033[32m$1\033[0m"
}

stopProcess(){
    p="$(ps -ef|grep [P]rivateCloud.Server|awk '{print $2}')"
    if [[ ! -z $p ]];then
        kill $p
    fi
}

stopProcess
log 'complete'