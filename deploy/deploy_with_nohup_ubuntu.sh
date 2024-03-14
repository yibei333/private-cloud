#! /bin/bash

position="$(cd "$(dirname "$0")" && pwd)"
cd $position
git pull

p="$(ps -ef|grep [P]rivateCloud.Server|awk '{print $2}')"
if [[ ! -z $p ]];then
    kill $p
fi

dotnet publish "$position/../src/PrivateCloud.Server" -c Release -r linux-x64 -o "$position/../bin"
sudo chmod +x "$position/../bin/PrivateCloud.Server"
nohup "$position/../bin/PrivateCloud.Server" > /dev/null 2>&1 &
