export default {
    data() {
        return {
            isClient: false,
            showNetworkModal: false,
            notices: [],
            options: {
                name: '',
                password: '',
                networks: [
                    { type: 1, value: '' },
                    { type: 2, value: '' },
                ]
            },
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/login`;
        this.isClient = this.http.isClient;
        this.init();
    },
    methods: {
        init() {
            this.getNotices();
            this.getOptions();
        },
        getNotices() {
            if (this.isClient) return;
            this.http.get({ url: `${this.api}/notice` }).then(res => this.notices = res.data);
        },
        async getOptions() {
            try {
                let optionsJson = localStorage.getItem('options');
                if (!optionsJson) return;
                this.options = JSON.parse(optionsJson);
                let savePasswordStorage = localStorage.getItem('savepassword');
                this.options.savepassword = Boolean(parseInt(savePasswordStorage));
                if (this.options.savepassword) {
                    if (this.http.getLoginPassword) this.options.password = await this.http.getLoginPassword(this.options.name);
                }
            } catch { }
        },
        async setOptions() {
            if (!this.isClient) return;
            let networks = this.options.networks.filter(x => x.isSelected);
            if (networks.length > 0) {
                this.http.baseUrl = networks[0].value;
                this.api = `${this.http.baseUrl}/api/login`;
                localStorage.setItem('api', this.http.baseUrl);
            }

            if (this.options.savepassword && this.http.setLoginPassword) this.http.setLoginPassword(this.options.name, this.options.password);
            let clonedOption = clone(this.options);
            clonedOption.password = '';
            let json = JSON.stringify(clonedOption);
            localStorage.setItem('options', json);
        },
        async login(loadingCallback) {
            localStorage.setItem('savepassword', this.options.savepassword ? 1 : 0);
            if (!this.options) {
                this.notify.error('参数错误');
                loadingCallback();
                return;
            }

            if (this.isClient) {
                let networks = this.options.networks.filter(x => x.isSelected);
                let api = networks.length > 0 ? networks[0] : null;
                if (!api) {
                    this.notify.warning('请选择一个网络');
                    this.showNetworkModal = true;
                    loadingCallback();
                    return;
                }
                if (!api.value) {
                    this.showNetworkModal = true;
                    this.notify.warning('接口地址不能为空');
                    loadingCallback();
                    return;
                }
            }

            if (this.$refs.fields.verify()) {
                await this.setOptions();
                this.http.post({ url: `${this.api}`, data: { name: this.options.name, password: this.options.password }, loadingCallback: loadingCallback, timeout: 5 }).then(res => {
                    localStorage.setItem('user', JSON.stringify(res.data));
                    location.replace("/index.html");
                });
            } else {
                loadingCallback();
                return;
            }
        },
        selectNetwork(item) {
            item.isSelected = true;
            let selectedItems = this.options.networks.filter(x => x.isSelected);
            if (selectedItems && selectedItems.length > 0) {
                for (let i = 0; i < selectedItems.length; i++) {
                    if (selectedItems[i].type != item.type) selectedItems[i].isSelected = false;
                }
            }
        },
        confirmNetwork(callback) {
            if (!this.options) {
                this.notify.error('参数错误');
                callback(false);
                return;
            }

            if (this.isClient) {
                for (let i = 0; i < this.options.networks.length; i++) {
                    let network = this.options.networks[i];
                    if (network.value && network.value != '' && !network.value.startsWith('http://') && !network.value.startsWith('https://')) {
                        network.value = `http://${network.value}`;
                    }
                }
                let networks = this.options.networks.filter(x => x.isSelected);
                let api = networks.length > 0 ? networks[0] : null;
                if (!api) {
                    this.notify.warning('请选择一个网络');
                    this.showNetworkModal = true;
                    callback(false);
                    return;
                }
                if (!api.value) {
                    this.notify.warning('接口地址不能为空');
                    callback(false);
                    return;
                }
            }
            callback();
        },
    },
}