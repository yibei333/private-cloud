# 介绍
这是一个将你的个人电脑作为家庭文件服务器的项目，它包含一些基础操作（添加，删除，加密等），也包含一些拓展功能（收藏，历史，预览）。
采用asp.net core作为服务端api（同时包含web页面），maui blazor+vue3作为客户端。

# 截图
<img src="https://gitee.com/developer333/private-cloud/raw/main/screenshot/login.png">
<br>
<img src="https://gitee.com/developer333/private-cloud/raw/main/screenshot/medialib.png">
<br>
<img src="https://gitee.com/developer333/private-cloud/raw/main/screenshot/file.png">
<br>
<img src="https://gitee.com/developer333/private-cloud/raw/main/screenshot/play.png">
<br>
<img src="https://gitee.com/developer333/private-cloud/raw/main/screenshot/user.png">
<br>
<img src="https://gitee.com/developer333/private-cloud/raw/main/screenshot/about.png">

# 起步

## 1.安装服务端（支持windows+linux）

### 1.1.windows（需要系统为win10及以上）
* 找一个放置程序的目录下载并运行[deploy/windows_install_update_restart.bat脚本](https://gitee.com/developer333/private-cloud/raw/main/deploy/windows_install_update_restart.bat)。
* 首次安装会自动下载[ffmpeg](https://gitee.com/developer333/private-cloud/releases/download/1.0/ffmpeg.windows.zip)程序。
* 安装完成后会添加一个名称为private.cloud的服务，并且会自动打开```http://localhost:9090```。
* [deploy/windows_install_update_restart.bat脚本](https://gitee.com/developer333/private-cloud/raw/main/deploy/windows_install_update_restart.bat)提供了安装，更新，重启的功能。
* 如果需要删除服务可以手动操作也可以运行[deploy/windows_remove_service.bat脚本](https://gitee.com/developer333/private-cloud/raw/main/deploy/windows_remove_service.bat)。
* 打开```http://localhost:9090```会自动跳转到登录页，页面有初始密码提示，登录成功后可以到[管理->用户管理]页面更改密码。

### 1.2.linux
目前只是在ubuntu20.04中做了测试，低版本可能有sqlite引用的问题，其他发行版或者docker中未进行测试。

* 找一个放置程序的目录下载并运行[deploy/linux_install_update_restart.sh脚本](https://gitee.com/developer333/private-cloud/raw/main/deploy/linux_install_update_restart.sh)。
* 首次运行脚本会自动下载[ffmpeg](https://gitee.com/developer333/private-cloud/releases/download/1.0/ffmpeg.linux.tar.xz)程序。
* 脚本运行完成后会监听9090端口（采用nohup的方式运行了一个守护进程）。
* [deploy/linux_install_update_restart.sh脚本](https://gitee.com/developer333/private-cloud/raw/main/deploy/linux_install_update_restart.sh)提供了启动，更新，重启的功能。
* 如果需要停止程序可以手动操作也可以运行[deploy/linux_stop.sh脚本](https://gitee.com/developer333/private-cloud/raw/main/deploy/linux_stop.sh)。
* 打开```http://yourhost:9090```会自动跳转到登录页，页面有初始密码提示，登录成功后可以到[管理->用户管理]页面更改密码。

## 2.安装客户端
* ios和mac的支持要看maui的支持情况，并未测试。
* 目前支持android和windows客户端。
    * [windows客户端](https://github.com/yibei333/private-cloud/releases/download/1.0/clients.privatecloud.win64.1.0.exe)
    * [android客户端](https://github.com/yibei333/private-cloud/releases/download/1.0/clients.privatecloud.android.1.0.apk)
    * [国内windows客户端](https://gitee.com/developer333/private-cloud/releases/download/1.0/clients.privatecloud.win64.1.0.exe)
    * [国内android客户端](https://gitee.com/developer333/private-cloud/releases/download/1.0/clients.privatecloud.android.1.0.apk)

# 打包
* pack文件夹提供了一键打包（包含linux服务端,windows服务端,android客户端,windows客户端）的依赖的命令，可参考。
* 每次打包需要更改pack/version.txt的版本号。

# 已知问题

1. 部分mp4不支持播放,[参考xgplayer](https://v3.h5player.bytedance.com/guide/extends/about_format.html)
