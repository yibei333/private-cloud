### 介绍
这是一个.net项目,可以将你的个人电脑作为家庭文件服务器使用（也许不止于此），包含一些基础操作（添加，删除，加密等），也包含一些拓展功能（收藏，历史，预览）。

### 截图


### 起步

#### 1.服务端

##### 1.1.windows
条件：win10及以上,安装了git,dotnet sdk,下载[ffmpeg](https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip)。
找一个文件位置打开cmd执行如下命令
```shell
git clone https://github.com/yibei333/private-cloud.git
cd private-cloud/deploy
deploy_as_windows_service.bat
```
启动后打开页面->http://localhost:9090,根据登录页面提示完成ffmpeg的配置以及初始用户的密码修改。
如后面代码有更新,执行private-cloud/deploy/update_windows_service.bat即可。

##### 1.2.linux
在部署前需要安装git,dotnet sdk,下载[ffmpeg](https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz)。
我只是在ubuntu中做了测试，低版本有sqlite引用的问题，需要20.04版本及以上。
其他发行版或者docker中未进行测试。
执行如下命令
```shell
git clone https://github.com/yibei333/private-cloud.git
sudo chmod +x ./private-cloud/deploy/deploy_with_nohup_ubuntu.sh
./private-cloud/deploy/deploy_with_nohup_ubuntu.sh
```
启动后需要修改appsettings.Other.json->FfmpegBinaryPath的值，默认配置的exe文件，重新运行deploy_with_nohup_ubuntu.sh脚本完成重启。
启动后打开页面->http://localhost:9090,根据登录页面提示完成ffmpeg的配置以及初始用户的密码修改。
如后面代码有更新,执行private-cloud/deploy/deploy_with_nohup_ubuntu.sh即可。
###### 注意事项：ffmpeg需要将文件权限提升为可执行
#### 2.客户端
请自行用visual studio或者其他方式发布包。
现提供[windows](https://github.com/yibei333/private-cloud-release-demo1.0/raw/main/files/package/windows/PrivateCloud.Maui_1.0.0.0.zip)和[android](https://github.com/yibei333/private-cloud-release-demo1.0/raw/main/files/package/android/com.yibei.privatecloud.apk)的示例包（需科学上网）,windows需要安装示例包中的证书（安装证书->本地计算机->将所有的证书都放入下列存储->受信任的根证书颁发机构）

### 已知问题
1.客户端不支持全屏[#15472](https://github.com/dotnet/maui/pull/15472)