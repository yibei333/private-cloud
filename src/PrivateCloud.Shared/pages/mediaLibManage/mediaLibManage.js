export default {
    props: ["goAdd"],
    data() {
        return {
            folderBase64: staticImages.folder,
            queryRequest: {
                index: 0,
                size: 20,
                name: ''
            },
            items: [],
            pageData: {
                total: 0,
                pageCount: 0
            },
            detailModalShow: false,
            detail: {},
            addModalShow: false,
            addRequest: {},
            editModalShow: false,
            editRequest: {},
            encryptModalShow: false,
            encryptRequest: {},
            decryptModalShow: false,
            decryptRequest: {},
            modifyPasswordModalShow: false,
            modifyPasswordRequest: {},
            uploadInputElement: null
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/mediaLib`;
    },
    activated() {
        this.query();
        if(this.goAdd) this.showAddModal();
    },
    methods: {
        query() {
            this.http.get({ url: `${this.api}`, data: this.queryRequest }).then(res => {
                this.items = res.data;
                this.queryRequest.index = res.index;
                this.queryRequest.size = res.size;
                this.pageData.total = res.total;
                this.pageData.pageCount = res.pageCount;
            });
        },
        queryDetail(id) {
            return this.http.get({ url: `${this.api}/${id}` });
        },
        showDetailModal(item) {
            this.queryDetail(item.id).then(res => {
                this.detail = res.data;
                this.detailModalShow = true;
            });
        },
        upload(item) {
            this.$refs.addFieldsThumb?.focus();
            this.$refs.editFieldsThumb?.focus();

            if (!this.uploadInputElement) {
                this.uploadInputElement = document.createElement('input');
                this.uploadInputElement.type = 'file';
            }

            let that = this;
            this.uploadInputElement.addEventListener('change', function () {
                let files = that.uploadInputElement.files;
                if (files.length > 0) {
                    imgToBase64(files[0]).then(base64 => item.thumb = base64);
                }
            }, { once: true });
            this.uploadInputElement.click();
        },
        pathChange(item) {
            let path = item.path?.replaceAll('\\', '/');
            if (!path) return;
            item.name = path.substring(path.lastIndexOf('/') + 1);
        },
        showAddModal() {
            this.addRequest = {
            };
            this.addModalShow = true;
        },
        add(loadingCallback) {
            if (!this.$refs.addFields.verify()) {
                loadingCallback(false);
                return;
            }
            this.http.post({ url: `${this.api}`, data: this.addRequest, loadingCallback: loadingCallback }).then(() => this.query());
        },
        showEditModal(item) {
            this.queryDetail(item.id).then(res => {
                this.editRequest = res.data;
                this.editModalShow = true;
            });
        },
        edit(loadingCallback) {
            if (!this.$refs.editFields.verify()) {
                loadingCallback(false);
                return;
            }
            this.http.put({ url: `${this.api}/${this.editRequest.id}`, data: this.editRequest, loadingCallback: loadingCallback }).then(() => this.query());
        },
        remove(item) {
            this.notify
                .confirm(`确定要删除媒体库【${item.name}】?`)
                .then(res => {
                    if (res) this.http.delete({ url: `${this.api}/${item.id}` }).then(() => this.query());
                });
        },
        showEncryptModal(item) {
            this.encryptRequest = { id: item.id, showConfirm: !item.isEncrypt, password: '', confirmPassword: '' };
            this.encryptModalShow = true;
        },
        encrypt(loadingCallback) {
            if (!this.$refs.encryptFields.verify()) {
                loadingCallback(false);
                return;
            }

            if (this.encryptRequest.showConfirm && this.encryptRequest.password != this.encryptRequest.confirmPassword) {
                this.notify.warning('密码不一致');
                loadingCallback(false);
                return;
            }
            let data = { id: this.encryptRequest.id, password: aesEncrypt(this.encryptRequest.password, getUser().cryptoId) };
            this.http.put({ url: `${this.api}/${data.id}/encrypt`, data: data, loadingCallback: loadingCallback }).then(() => this.query());
        },
        showDecryptModal(item) {
            this.decryptRequest = { id: item.id, password: '' };
            this.decryptModalShow = true;
        },
        decrypt(loadingCallback) {
            if (!this.$refs.decryptFields.verify()) {
                loadingCallback(false);
                return;
            }

            let data = { id: this.decryptRequest.id, password: aesEncrypt(this.decryptRequest.password, getUser().cryptoId) };
            this.http.put({ url: `${this.api}/${data.id}/decrypt`, data: data, loadingCallback: loadingCallback }).then(() => this.query());
        },
        showModifyPasswordModal(item) {
            this.modifyPasswordRequest = { id: item.id, password: '', newPassword: '', confirmPassword: '' };
            this.modifyPasswordModalShow = true;
        },
        modifyPassword(loadingCallback) {
            if (!this.$refs.modifyPasswordFields.verify()) {
                loadingCallback(false);
                return;

            }

            if (this.modifyPasswordRequest.newPassword != this.modifyPasswordRequest.confirmPassword) {
                this.notify.warning('密码不一致');
                loadingCallback(false);
                return;
            }

            let data = {
                id: this.modifyPasswordRequest.id,
                password: aesEncrypt(this.modifyPasswordRequest.password, getUser().cryptoId),
                newPassword: aesEncrypt(this.modifyPasswordRequest.newPassword, getUser().cryptoId)
            };
            return this.http.put({ url: `${this.api}/${data.id}/modifyPassword`, data: data, loadingCallback: loadingCallback }).then(() => this.query());
        },
        clean(loadingCallback) {
            this.http.post({ url: `${this.api}/clean`, loadingCallback: loadingCallback }).then(() => this.notify.success('清理成功'));
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}