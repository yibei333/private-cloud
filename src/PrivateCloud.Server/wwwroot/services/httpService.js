export class httpService {
    baseUrl = '';
    appid;
    isClient;
    notify;
    static confirming;

    constructor() {
        this.notify = new notify();
        this.appid = localStorage.getItem('appid');
        this.isClient = false;
    }

    getMediaLibIdFromIdPath(idPath) {
        return fromHex(idPath).split(';')[0];
    }

    getHeaders(options) {
        let headers = {};

        let token = getToken();
        if (token) headers['Token'] = token;

        if (options.headers && options.headers.length > 0) {
            options.headers.forEach(item => {
                if (item.value) headers[item.name] = item.value;
            });
        }

        if (options.idPath) {
            let id = this.getMediaLibIdFromIdPath(options.idPath);
            let mediaLibToken = sessionStorage.getItem(`MediaLibToken_${id}`);
            headers["MediaLibToken"] = mediaLibToken;
        }

        return headers;
    }

    cleanStorage() {
        localStorage.removeItem('user');
    }

    handleRequestError(error, options) {
        if (!error || !error.response) {
            this.notify.error(error?.message ?? 'error response');
        }
        else if (error.response.status === 401) {
            this.cleanStorage();
            location.replace('/index.html#/login');
            return;
        }
        else if (error.response.status === 402) {
            if (!httpService.confirming) {
                httpService.confirming = true;
                let value = null;
                let id = this.getMediaLibIdFromIdPath(options.idPath);
                let html = `
                <div class='mediaLibPassowrdConfirm'>
                    <label for='mediaLibPassowrdConfirmInput'>请输入${(options.mediaLibName ? '媒体库【' + options.mediaLibName + '】的' : '')}密码</label>
                    <form onsubmit='return false'>
                        <input id='mediaLibPassowrdConfirmInput' onkeyup="if(event.keyCode==13) event.target.parentNode.parentNode.parentNode.parentNode.querySelector('.ok').click()" class="input" type="password" autocomplete="off" />
                    </form>
                </div>
                `;
                this.notify
                    .confirm(html, { beforeClose: (_, element) => value = element.querySelector('.input').value })
                    .then((res) => {
                        httpService.confirming = false;
                        if (res) {
                            if (!value) {
                                if (options.mediaLibPasswordCallback) options.mediaLibPasswordCallback();
                                this.notify.error('密码不能为空');
                                return;
                            }
                            this.post({
                                url: `${this.baseUrl}/api/medialib/token`,
                                data: {
                                    id: id,
                                    token: aesEncrypt(value, getUser().cryptoId)
                                }
                            }).then(res => {
                                sessionStorage.setItem(`MediaLibToken_${id}`, res.data);
                                if (options.mediaLibPasswordCallback) options.mediaLibPasswordCallback();
                            }).catch(() => {
                                if (options.mediaLibPasswordCallback) options.mediaLibPasswordCallback();
                            });
                        }
                    });
            }
        }
        else if (error.response.status === 403) {
            this.notify.error("access denied");
        }
        else {
            if (options.notifyError != false) this.notify.error(error.response?.data?.description ?? error.message);
        }
        console.error('request failed', error);
    }

    request(method, options) {
        return new Promise((resolve, reject) => {
            if (method == 'get') {
                options.url = `${options.url}${toQueryString(options.data)}`;
                options.data = null;
            }
          
            let requestOption = {
                method: method,
                url: options.url,
                data: options.data,
                headers: this.getHeaders(options),
                responseType: options?.responseType ?? 'json',
                timeout: options.timeout * 1000 | 0,
                onUploadProgress: e => {
                    if (options?.progress) {
                        let progress = Math.round((e.loaded * 100) / e.total);
                        options.progress(progress);
                    }
                },
                onDownloadProgress: e => {
                    if (options?.progress) {
                        let progress = Math.round((e.loaded * 100) / e.total);
                        options.progress(progress);
                    }
                }
            };

            axios(requestOption).then(response => {
                if (options.notifySuccess) this.notify.success(response.data?.description ?? '成功');
                if (options.loadingCallback) options.loadingCallback();
                resolve(response.data);
            }).catch((error) => {
                if (options.loadingCallback) options.loadingCallback(false);
                reject(error);
                this.handleRequestError(error, options);
            });
        });
    }

    get = (options) => this.request('get', options);

    post = (options) => this.request('post', options);

    put = (options) => this.request('put', options);

    delete = (options) => this.request('delete', options);

    getBlob = async (options) => {
        options.responseType = 'blob';
        let response = this.request('get', options);
        if (!options.isEncrypt) return response;
        let blob = await response;
        return await aesDecryptBlob(blob, options.key, options.iv);
    }

    getBlobUrl = async (options) => {
        let blob = await this.getBlob(options);
        return window.URL.createObjectURL(blob);
    }

    downloadBlob(blob, saveName) {
        if (window.navigator && window.navigator.msSaveOrOpenBlob) {
            window.navigator.msSaveOrOpenBlob(blob, saveName)
        } else {
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = saveName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        }
    }

    async download(request, loadingCallback, progress) {
        if (request.bigFile) {
            this.notify.info('尝试用浏览器下载大文件');
            this.openBrowser(request.idPath);
            return new Promise((resolve) => resolve());
        }

        let blob = await this.getBlob({
            url: `${this.baseUrl}/api/file/${request.idPath}`,
            idPath: request.idPath,
            loadingCallback: loadingCallback,
            progress: progress
        });
        return this.downloadBlob(blob, request.name);
    }

    upload(isFolder, uploadOptions) {
        let that = this;
        let input = document.createElement('input');
        input.type = 'file';
        input.setAttribute('multiple', '');
        if (isFolder) {
            input.setAttribute('webkitdirectory', '');
        }
        input.addEventListener('change', function () {
            if (input.files.length <= 0) return;

            let formData = new FormData();
            for (let i = 0; i < input.files.length; i++) {
                formData.append(`Files`, input.files[i]);
            }

            let options = {
                url: `${uploadOptions.api}/upload/${uploadOptions.idPath}`,
                data: formData,
                idPath: uploadOptions.idPath,
                loadingCallback: uploadOptions.uploaded,
                progress: uploadOptions.progress,
                notifySuccess: true
            };
            uploadOptions.uploading();
            that.post(options);
        });
        input.click();
    }

    uploadFolder(uploadOptions) {
        this.upload(true, uploadOptions);
    }

    uploadFiles(uploadOptions) {
        this.upload(false, uploadOptions);
    }

    openBrowser(idPath) {
        let id = this.getMediaLibIdFromIdPath(idPath);
        let mediaLibToken = sessionStorage.getItem(`MediaLibToken_${id}`);
        window.open(`${this.baseUrl}/api/file/${idPath}?Token=${getToken()}&MediaLibToken=${mediaLibToken}&tryOpen=true`, '_blank');
    }

    setClipboard(text) {
        navigator
            .clipboard
            .writeText(text)
            .then(() => this.notify.success('拷贝成功'))
            .catch((error) => {
                this.notify.error(`拷贝失败:${error}`);
                console.log(error);
            });
    }
}