export class httpService {
    baseUrl;
    appid;
    isClient;
    notify;
    static confirming;
    assemblyName = 'PrivateCloud.Maui';

    constructor() {
        this.baseUrl = localStorage.getItem('api');
        this.notify = new notify();
        this.appid = localStorage.getItem('appid');
        this.isClient = true;
    }

    getMediaLibIdFromIdPath(idPath) {
        return fromHex(idPath).split(';')[0];
    }

    getHeaders(options) {
        let headers = {};
        let keyValueHeaders = this.getKeyValueHeaders(options);

        for (let i = 0; i < keyValueHeaders.length; i++) {
            headers[keyValueHeaders[i].key] = keyValueHeaders[i].value;
        }
        return headers;
    }

    getKeyValueHeaders(options) {
        let headers = [];

        let token = getToken();
        if (token) headers.push({ key: 'Token', value: token });

        if (options.headers && options.headers.length > 0) {
            options.headers.forEach(item => {
                if (item.value) headers.push({ key: item.name, value: item.value });
            });
        }

        if (options.idPath) {
            let id = this.getMediaLibIdFromIdPath(options.idPath);
            let mediaLibToken = sessionStorage.getItem(`MediaLibToken_${id}`);
            headers.push({ key: 'MediaLibToken', value: mediaLibToken });
        }

        return headers;
    }

    cleanStorage() {
        localStorage.removeItem('user');
    }

    handleRequestError(response, options) {
        if (!response) {
            this.notify.error('no response');
        }
        else if (response.code === 401) {
            this.cleanStorage();
            location.replace('/index.html#/login');
            return;
        }
        else if (response.code === 402) {
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
        else if (response.code === 403) {
            this.notify.error("access denied");
        }
        else {
            if (options.notifyError != false) {
                if (options.method == 'blob') {
                    this.notify.error(response.message);
                } else {
                    let data = (response.data) ? JSON.parse(response.data) : null;
                    this.notify.error(data?.description ?? response.message);
                }
            }
        }
        console.error('request failed', response);
    }

    createRequestOption(method, options) {
        let json = null;
        if (options?.data) json = JSON.stringify(options.data);
        let optionInstance = null;
        let hasProgress = false;
        if (options?.progress) {
            optionInstance = DotNet.createJSObjectReference(options);
            hasProgress = true;
        }

        return {
            url: method == 'get' ? `${options.url}${toQueryString(options.data)}` : options.url,
            method: method,
            data: json,
            name: options.name,
            headers: this.getKeyValueHeaders(options),
            optionInstance: optionInstance,
            hasProgress: hasProgress,
            timeout: options.timeout | null
        };
    }

    request(method, options) {
        return new Promise((resolve, reject) => {
            DotNet.invokeMethodAsync(this.assemblyName, "HttpRequest", this.createRequestOption(method, options))
                .then(response => {
                    if (response && response.code >= 200 && response.code < 300) {
                        let data = (response.data) ? JSON.parse(response.data) : null;
                        if (options.notifySuccess) this.notify.success(data?.description ?? '成功');
                        if (options.loadingCallback) options.loadingCallback();
                        resolve(data);
                    }
                    else {
                        console.log('request options', options);
                        if (options.loadingCallback) options.loadingCallback(false);
                        reject(response);
                        this.handleRequestError(response, options);
                    }
                });
        });
    }

    get = (options) => this.request('get', options);

    post = (options) => this.request('post', options);

    put = (options) => this.request('put', options);

    delete = (options) => this.request('delete', options);

    getBlob = (options) => {
        return new Promise((resolve, reject) => {
            DotNet.invokeMethodAsync(this.assemblyName, "GetBlob", this.createRequestOption('blob', options))
                .then(response => {
                    if (response && response.code >= 200 && response.code < 300) {
                        let data = (response.data) ? new Blob([response.data]) : null;
                        if (options.notifySuccess) this.notify.success('成功');
                        if (options.loadingCallback) options.loadingCallback();
                        if (!options.isEncrypt) resolve(data);
                        else {
                            aesDecryptBlob(data, options.key, options.iv).then(blob => resolve(blob));
                        }
                    }
                    else {
                        console.log('request options', options);
                        if (options.loadingCallback) options.loadingCallback(false);
                        reject(response);
                        this.handleRequestError(response, options);
                    }
                });
        });
    }

    getBlobUrl = (options) => this.getBlob(options).then(blob => window.URL.createObjectURL(blob));

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
        let url = `${this.baseUrl}/api/file/${request.idPath}`;
        let options = { url: url, progress: progress, idPath: request.idPath, name: request.name, loadingCallback: loadingCallback };
        return this.downloadUrl(options);
    }

    async downloadUrl(options) {
        return new Promise((resolve, reject) => {
            DotNet.invokeMethodAsync(this.assemblyName, "Download", this.createRequestOption('get', options))
                .then(response => {
                    if (response && response.code >= 200 && response.code < 300) {
                        if (options.loadingCallback) options.loadingCallback();
                        if (response?.message && response?.message != '') this.notify.info(response.message);
                        resolve(response);
                    }
                    else {
                        console.log('request options', options);
                        if (options.loadingCallback) options.loadingCallback(false);
                        reject(response);
                        this.handleRequestError(response, options);
                    }
                });
        });
    }

    upload(isFolder, uploadOptions) {
        let options = {
            url: `${uploadOptions.api}/upload/${uploadOptions.idPath}`,
            idPath: uploadOptions.idPath,
            loadingCallback: uploadOptions.uploaded,
            progress: uploadOptions.progress,
            notifySuccess: true
        };
        uploadOptions.uploading();

        return new Promise((resolve, reject) => {
            DotNet.invokeMethodAsync(this.assemblyName, isFolder ? "UploadFolder" : "UploadFiles", this.createRequestOption('post', options))
                .then(response => {
                    if (response && response.code >= 200 && response.code < 300) {
                        if (response.message == 'cancle') {
                            uploadOptions.cancle();
                            reject(response);
                            return;
                        }

                        let data = (response.data) ? JSON.parse(response.data) : null;
                        if (options.notifySuccess) this.notify.success(data?.description ?? '成功');
                        if (options.loadingCallback) options.loadingCallback();
                        resolve(data);
                    }
                    else {
                        console.log('request options', options);
                        if (options.loadingCallback) options.loadingCallback(false);
                        reject(response);
                        this.handleRequestError(response, options);
                    }
                });
        });
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
        let url = `${this.baseUrl}/api/file/${idPath}?Token=${getToken()}&MediaLibToken=${mediaLibToken}&tryOpen=true`;
        DotNet.invokeMethodAsync(this.assemblyName, "OpenBrowser", url);
    };

    setWebHandleBack(name) {
        let isHandle = (name == 'file' || name == 'play');
        DotNet.invokeMethodAsync(this.assemblyName, "SetWebHandleBack", isHandle);
    }

    getLoginPassword(name) {
        return DotNet.invokeMethodAsync(this.assemblyName, "GetLoginPassword", name);
    }

    setLoginPassword(name, password) {
        DotNet.invokeMethodAsync(this.assemblyName, "SetLoginPassword", { key: name, value: password });
    }

    getAppInfo() {
        return DotNet.invokeMethodAsync(this.assemblyName, "GetAppInfo");
    }

    setClipboard(text) {
        DotNet.invokeMethodAsync(this.assemblyName, "SetClipboard", text).then(response => {
            if (response.success) this.notify.success('拷贝成功');
            else this.notify.error(`拷贝失败:${response.description}`);
        });
    }

    fullScreen() {
        DotNet.invokeMethodAsync(this.assemblyName, "FullScreen");
    }

    exitFullScreen() {
        DotNet.invokeMethodAsync(this.assemblyName, "ExitFullScreen");
    }
}