export default {
    data() {
        return {
            isLocal: false,
            version: 1.0,
            platform: '',
            last: 1.0,
            isMostNew: true,
            upgrade: null,
            progress: null
        }
    },
    mounted() {
        this.getAppInfo();
    },
    methods: {
        getAppInfo() {
            if (!this.http.isClient) return;
            let hostname = new URL(this.http.baseUrl).hostname;
            this.isLocal = (hostname === 'localhost' || hostname === '0.0.0.0' || hostname.startsWith('192.168.') || hostname.startsWith('127.0.') || hostname.startsWith('172.') || hostname.startsWith('10.'));

            this.http.getAppInfo().then(res => {
                this.version = Number.parseFloat(res.version);
                this.platform = res.platform;
                this.getLastVersion();
            });
        },
        getLastVersion(callback) {
            this.http.get({ url: `${this.http.baseUrl}/api/upgrade/last/${this.platform}`, loadingCallback: callback }).then(res => {
                if (!res.data) {
                    this.last = this.version;
                    this.notify.success('已经是最新版本了');
                }
                else {
                    this.upgrade = res.data;
                    this.last = Number.parseFloat(res.data.version);
                    this.isMostNew = (this.version >= this.last);
                    if (this.isMostNew) this.notify.success('已经是最新版本了');
                }
            });
        },
        download(callback) {
            if (!this.upgrade) return;
            let url = this.isLocal ? this.upgrade.localUrl : this.upgrade.url;
            if (!url) {
                this.notify.warning('找不到下载地址');
                return;
            }
            let name = `私有云.${this.last}.${this.upgrade.extension}`;
            this.http.downloadUrl({ url: url, name: name, loadingCallback: callback, progress: p => this.progress = p }).then(() => {
                this.progress = null;
                this.notify.success('下载成功');
            });
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}