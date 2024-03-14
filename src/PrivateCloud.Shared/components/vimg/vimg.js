import { httpService } from '/services/httpService.js'

export default {
    props: {
        height: {
            type: String,
            default: '32px'
        },
        width: {
            type: String,
            default: '32px'
        },
        baseUrl: {
            type: String,
            default: null
        },
        idPath: {
            type: String,
            default: null
        },
        hasThumb: {
            type: Boolean,
            default: false
        },
        hasLargeThumb: {
            type: Boolean,
            default: false
        },
        icon: {
            type: String,
            default: ''
        },
        isEncrypt: {
            type: Boolean,
            default: false
        },
        mediaLibName: {
            type: String,
            default: null
        }
    },
    emits: ["click", "mediaLibPasswordCallback"],
    watch: {
        '$root.size'() {
            this.size = this.$root.size;
            this.setSizeName();
        }
    },
    data() {
        return {
            lockBase64: null,
            size: 0,
            sizeName: '',
            service: null,
            loaded: false,
            key: '',
            iv: '',
            url: '',
            largeUrl: '',
            showLarge: false
        }
    },
    unmounted() {
        if (this.url != '' && this.url?.startsWith('blob')) {
            try {
                URL.revokeObjectURL(this.url);
            } catch { }
        }
        if (this.largeUrl != '' && this.url?.startsWith('blob')) {
            try {
                URL.revokeObjectURL(this.largeUrl);
            } catch { }
        }
    },
    mounted() {
        this.lockBase64 = staticImages.lock;
        this.size = this.$root.size;
        this.setSizeName();
        this.service = new httpService(this.baseUrl);
        this.getBlobUrl();
        document.addEventListener('mousedown', e => {
            if (!this.$refs.largeImage || !this.$refs.image.contains(e.target) && !this.$refs.largeImage.contains(e.target)) this.showLarge = false;
        });
    },
    methods: {
        setSizeName() {
            if (this.size == 1) this.sizeName = 'small';
            else if (this.size == 2) this.sizeName = 'medium';
            else if (this.size == 3) this.sizeName = 'large';
            else if (this.size == 4) this.sizeName = 'xlarge';
            else if (this.size == 5) this.sizeName = 'xxlarge';
        },
        click(event) {
            event.stopPropagation();
            this.$emit('click');
        },
        mousedown() {
            if (!this.hasThumb) return;
            if (!this.showLarge) {
                if (this.largeUrl == '') {
                    this.getLargeBlobUrl();
                    return;
                }
            }
            this.showLarge = !this.showLarge;
        },
        async getBlobUrl() {
            if (!this.hasThumb) {
                this.url = staticImages[this.icon];
                this.loaded = true;
                return;
            }

            await this.getKey();
            let options = {
                url: `${this.baseUrl}/api/file/thumb/0/${this.idPath}`,
                idPath: this.idPath,
                isEncrypt: this.isEncrypt && this.hasThumb,
                mediaLibName: this.mediaLibName,
                key: this.key,
                iv: this.iv,
                mediaLibPasswordCallback: () => this.$emit('mediaLibPasswordCallback')
            };
            this.service.getBlobUrl(options).then(res => {
                this.url = res;
                this.loaded = true;
            });
        },
        async getLargeBlobUrl() {
            if (!this.hasLargeThumb) {
                if (this.hasThumb) {
                    this.largeUrl = this.url;
                    this.showLarge = true;
                    this.loaded = true;
                }
                return;
            }

            await this.getKey();
            let options = {
                url: `${this.baseUrl}/api/file/thumb/1/${this.idPath}`,
                idPath: this.idPath,
                isEncrypt: this.isEncrypt && this.hasThumb,
                mediaLibName: this.mediaLibName,
                key: this.key,
                iv: this.iv,
                mediaLibPasswordCallback: () => this.$emit('mediaLibPasswordCallback')
            };
            this.service.getBlobUrl(options).then(res => {
                this.largeUrl = res;
                this.showLarge = true;
            });
        },
        async getKey() {
            if (this.key != '') return;
            if (!this.isEncrypt) return;
            if (!this.hasThumb) return;
            let response = await this.service.get({
                url: `${this.baseUrl}/api/file/key/${this.idPath}`,
                idPath: this.idPath,
                mediaLibName: this.mediaLibName,
                mediaLibPasswordCallback: () => this.$emit('mediaLibPasswordCallback')
            });
            let tempKey = getCryptoId();
            this.key = aesDecrypt(response.data.key, tempKey);
            this.iv = response.data.iv;
        }
    }
}