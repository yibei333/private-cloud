export default {
    data() {
        return {
            currentVersion: '0',
            lastVersion: '0',
            platform: '',
            last: null,
            isMostNew: true,
            progress: null,
            downloadDisabled: false,
        }
    },
    mounted() {
        this.getAppInfo();
    },
    methods: {
        getAppInfo() {
            if (!this.http.isClient) return;
            this.http.getAppInfo().then(res => {
                this.currentVersion = res.version;
                this.platform = res.platform;
                this.getLastVersion();
            });
        },
        getLastVersion(callback) {
            this.http.get({ url: `${this.http.baseUrl}/api/version/last/${this.platform}`, loadingCallback: callback }).then(res => {
                if (!res.data) {
                    this.lastVersion = this.currentVersion;
                    this.notify.success('已经是最新版本了');
                }
                else {
                    this.last = res.data;
                    this.lastVersion = res.data.version;
                    this.isMostNew = (this.lastVersion == this.currentVersion);
                    if (this.isMostNew) this.notify.success('已经是最新版本了');
                }
            });
        },
        download() {
            if (!this.last) return;
            this.startDownload();
            this.http.downloadUrl({ url: this.last.giteeUrl, name: this.last.name, loadingCallback: this.downloaded, progress: p => this.progress = p, notifyError: false }).then(() => {
                this.progress = null;
                this.notify.success('下载成功');
            }).catch(() => {
                this.startDownload();
                this.http.downloadUrl({ url: this.last.githubUrl, name: this.last.name, loadingCallback: this.downloaded, progress: p => this.progress = p }).then(() => {
                    this.progress = null;
                    this.notify.success('下载成功');
                }).catch(() => {
                    this.downloaded();
                });
            });
        },
        startDownload() {
            this.downloadDisabled = true;
            this.$refs.downloadButton.loading = true;
        },
        downloaded() {
            this.downloadDisabled = false;
            this.$refs.downloadButton.loading = false;
            this.progress = null;
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}