export default {
    props: ["id"],
    watch: {
        id() {
            this.idPath = sessionStorage.getItem('fileid');
            if (!this.idPath) {
                this.notify.warning('找不到文件,请重新选择一个媒体库');
                this.$router.push({ name: 'mediaLib' });
                return;
            }
            this.init();
        }
    },
    data() {
        return {
            idPath: null,
            folderLoaded: false,
            folder: {},
            request: {},
            pageCount: 0,
            items: [],
            operationShow: false,
            operationLoading: false,
            operationOptions: [],
            operateItem: null,
            showRetry: false,
            addFolderRequest: {},
            addFolderShow: false,
            uploading: false,
            uploadingProgress: null,
            foreverRecordShow: false,
            foreverRecordUrl: null,
            renameShow: false,
            renameRequest: {},
            downloading: false,
            downloadingProgress: null,
            keyShow: false,
            key: null,
            detailShow: false
        }
    },
    beforeRouteLeave(to, from) {
        let scrollTop = document.querySelector('.file-page .pull-refresh-content')?.scrollTop ?? 0;
        sessionStorage.setItem('fileScrollTop', scrollTop);
    },
    activated() {
        let timer = setTimeout(() => {
            let top = Number.parseFloat(sessionStorage.getItem('fileScrollTop')) || 0;
            let content = document.querySelector('.file-page .pull-refresh-content');
            if (content) content.scrollTop = top;
            clearTimeout(timer);
        }, 50);
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/file`;
        this.idPath = sessionStorage.getItem('fileid');
        this.init();
    },
    methods: {
        async init(name = null) {
            sessionStorage.removeItem('fileScrollTop');
            this.showRetry = false;
            this.request = {
                idPath: this.idPath,
                name: name,
                pageIndex: 1,
                pageSize: 20
            }
            this.items = [];
            this.folderLoaded = false;
            await this.getFolder();
            if (this.folderLoaded) this.$refs.infiniteScroll.reset();
        },
        getFolder() {
            if (this.folderLoaded) return;
            let option = {
                url: `${this.api}/folder/${this.idPath}`,
                idPath: this.idPath,
                mediaLibPasswordCallback: this.init
            };
            return this.http.get(option).then(async res => {
                this.folder = res.data;
                this.folderLoaded = true;
            }).catch(() => this.showRetry = true);
        },
        async getEntries() {
            let option = {
                url: `${this.api}/entries`,
                data: this.request,
                idPath: this.request.idPath,
                mediaLibPasswordCallback: this.init
            };
            return this.http.get(option).then(res => {
                this.pageCount = res.pageCount;
                let data = res.data;
                for (let i = 0; i < data.length; i++) {
                    this.items.push(data[i]);
                }
                this.request.pageIndex++;
                return res.data;
            });
        },
        loadMore(state) {
            if (!this.folderLoaded) return;
            this.getEntries().then(data => {
                if (data.length >= this.request.pageSize) state.loaded();
                else state.complete();
            }).catch(() => state.error());
        },
        showOperation(item) {
            this.operateItem = item;
            this.operationShow = true;
            this.operationOptions = [];
            this.operationOptions.push({ id: 0, name: '详情' });
            if (item.isFavorited) this.operationOptions.push({ id: 1, name: '取消收藏' });
            else this.operationOptions.push({ id: 2, name: '收藏' });
            if (!item.isFolder) {
                this.operationOptions.push({ id: 3, name: '预览' });
                this.operationOptions.push({ id: 4, name: '尝试用浏览器打开' });
                if (!item.isEncrypt) this.operationOptions.push({ id: 5, name: '永久链接' });
                this.operationOptions.push({ id: 6, name: '下载' });
            }
            this.operationOptions.push({ id: 7, name: '重命名' });
            if (item.isEncrypt) this.operationOptions.push({ id: 8, name: '查看key' });
            this.operationOptions.push({ id: 9, name: '删除', danger: true });
            this.operationOptions.push({ id: 10, name: '取消' });
        },
        operate(item) {
            if (item.id == 0) {
                this.detailShow = true;
                this.operationShow = false;
            }
            else if (item.id == 1) this.unFavorite();
            else if (item.id == 2) this.favorite();
            else if (item.id === 3) {
                this.jump(false, this.operateItem.idPath);
                this.operationShow = false;
            }
            else if (item.id === 4) this.openBrowser();
            else if (item.id === 5) this.foreverRecord();
            else if (item.id === 6) this.download();
            else if (item.id === 7) this.showRename();
            else if (item.id === 8) this.getKey();
            else if (item.id === 9) this.remove();
            else if (item.id === 10) this.operationShow = false;
            else this.notify.warning(`不支持的操作`);
        },
        favorite() {
            this.operationLoading = true;
            let option =
            {
                url: `${this.http.baseUrl}/api/favorite/${this.operateItem.idPath}`,
                data: { idPath: this.operateItem.idPath, isFolder: this.operateItem.isFolder, name: this.operateItem.name },
                idPath: this.operateItem.idPath,
                loadingCallback: () => this.operationLoading = false
            };
            this.http.post(option).then((res) => {
                this.operateItem.isFavorited = true;
                this.operateItem.favoritedId = res.data;
                this.operationShow = false;
            });
        },
        unFavorite() {
            this.operationLoading = true;
            let option =
            {
                url: `${this.http.baseUrl}/api/favorite/${this.operateItem.favoritedId}`,
                idPath: this.operateItem.idPath,
                loadingCallback: () => this.operationLoading = false
            }
            this.http.delete(option).then(() => {
                this.operateItem.isFavorited = false;
                this.operateItem.favoritedId = '00000000-0000-0000-0000-000000000000';
                this.operationShow = false;
            });
        },
        foreverRecord() {
            this.operationLoading = true;
            let option =
            {
                url: `${this.http.baseUrl}/api/foreverRecord/${this.operateItem.idPath}`,
                idPath: this.operateItem.idPath,
                loadingCallback: () => this.operationLoading = false
            };
            this.http.get(option).then((res) => {
                this.operationShow = false;
                this.foreverRecordUrl = `${this.http.baseUrl}/api/foreverRecord/file/${res.data}`;
                this.foreverRecordShow = true;
            });
        },
        copyToClipboard(text) {
            this.http.setClipboard(text);
        },
        openBrowser() {
            this.operationShow = false;
            this.http.openBrowser(this.operateItem.idPath);
        },
        showRename() {
            this.renameRequest = {
                isFolder: this.operateItem.isFolder,
                idPath: this.operateItem.idPath,
                name: this.operateItem.name
            };
            this.renameShow = true;
            this.operationShow = false;
        },
        rename(loadingCallback) {
            let option =
            {
                url: `${this.api}/rename/${this.renameRequest.idPath}`,
                data: this.renameRequest,
                idPath: this.renameRequest.idPath,
                loadingCallback: loadingCallback
            };
            this.http.put(option).then((res) => {
                this.operateItem.name = this.renameRequest.name;
                this.operateItem.idPath = res.data;
            });
        },
        download() {
            if (this.downloading) {
                this.notify.warning('请等待下载完成');
                return;
            }
            this.operationShow = false;
            this.http.download({ idPath: this.operateItem.idPath, name: this.operateItem.name, bigFile: this.operateItem.bigFile }, null, p => {
                sessionStorage.setItem('fileOperating', true);
                this.downloading = true;
                this.downloadingProgress = p;
            }).then(() => {
                this.downloading = false;
                this.downloadingProgress = null;
                sessionStorage.removeItem('fileOperating');
            });
        },
        getKey() {
            this.operationLoading = true;
            let option =
            {
                url: `${this.api}/key/${this.operateItem.idPath}`,
                idPath: this.operateItem.idPath,
                loadingCallback: () => this.operationLoading = false
            };

            this.http.get(option).then((res) => {
                this.operationShow = false;
                let tempKey = getCryptoId();
                let key = aesDecrypt(res.data.key, tempKey);
                let iv = res.data.iv;
                this.key = JSON.stringify({ key: key, iv: iv });
                this.keyShow = true;
            });
        },
        remove() {
            this.operationShow = false;
            this.notify.confirm(`确定要删除${(this.operateItem.isFolder ? '文件夹' : '文件')}【${this.operateItem.name}】`).then(res => {
                if (res) {
                    let option =
                    {
                        url: `${this.api}/${this.operateItem.idPath}/${this.operateItem.isFolder}`,
                        idPath: this.operateItem.idPath,
                    }
                    this.http.delete(option).then(() => {
                        let index = this.items.indexOf(this.operateItem);
                        this.items.splice(index, 1);
                    });
                }
            });
        },
        jump(isFolder, idPath, encrypting = false) {
            this.operationShow = false;
            if (encrypting) return;
            if (isFolder) {
                sessionStorage.setItem('fileid', idPath);
                this.$router.push({ name: 'file', params: { id: new Date().getTime() } });
            } else {
                sessionStorage.setItem('playid', idPath);
                this.$router.push({ name: 'play' });
            }
        },
        showAddFolder() {
            this.addFolderRequest = {
                idPath: this.idPath,
                name: ''
            };
            this.addFolderShow = true;
            this.$refs.addDropdown.active = false;
        },
        addFolder(callback) {
            let option =
            {
                url: `${this.api}/folder`,
                data: this.addFolderRequest,
                idPath: this.addFolderRequest.idPath,
                loadingCallback: callback
            };
            this.http.post(option).then(() => this.init());
        },
        upload(isFolder) {
            if (this.uploading) {
                this.notify.warning('请等待上传完成');
                return;
            }

            let options = {
                api: `${this.api}`,
                idPath: this.idPath,
                uploading: () => {
                    this.uploadingProgress = 0;
                    this.uploading = true;
                    this.$refs.addDropdown.active = false;
                    sessionStorage.setItem('fileOperating', true);
                },
                uploaded: () => {
                    this.init();
                    this.uploading = false;
                    this.uploadingProgress = null;
                    sessionStorage.removeItem('fileOperating');
                },
                cancle: () => {
                    this.uploading = false;
                    this.uploadingProgress = null;
                    sessionStorage.removeItem('fileOperating');
                },
                progress: (p) => this.uploadingProgress = p,
            };

            if (isFolder) this.http.uploadFolder(options);
            else this.http.uploadFiles(options);
        },
        uploadFolder() {
            this.upload(true);
        },
        uploadFiles() {
            this.upload(false);
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}