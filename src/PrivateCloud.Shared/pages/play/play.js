
export default {
    watch: {
        id() {
            sessionStorage.setItem("playid", this.id);
            this.init();
        }
    },
    data() {
        return {
            id: null,
            key: '',
            iv: null,
            file: {},
            videoPlayer: null,
            txt: null,
            url: null,
            posterUrl: null,
            loading: true,
            headers: {},
            positionTimer: null,
            maxImgScaleSize: 5,
            minImgScaleSize: .4,
            imgTouchPoint: null,
            imgTouchStartScale: null,
            imgStyle: {
                scale: 1,
                rotate: '0deg',
                translate: '0px 0px',
                maxWidth: '100%',
                maxHeight: '100%',
            },
            operationLoading: false,
            operationShow: false,
            downloadingProgress: null,
            downloading: false,
        }
    },
    beforeRouteLeave() {
        this.setHistory(this.getElementPostion());
        if (this.positionTimer) clearInterval(this.positionTimer);
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/file`;
    },
    activated() {
        this.id = sessionStorage.getItem("playid");
        this.positionTimer = setInterval(() => this.setHistory(this.getElementPostion()), 2000);
    },
    methods: {
        async init() {
            if (this.url) URL.revokeObjectURL(this.url);
            if (this.posterUrl) URL.revokeObjectURL(this.posterUrl);

            this.key = '';
            this.iv = null;
            this.file = {};
            this.txt = null;
            this.url = null;
            this.posterUrl = null;
            this.loading = true;
            await this.query();
            if (!this.file.current) return;
            this.setHistory(this.file.current.position, true);
            this.headers = this.http.getHeaders({ idPath: this.id });
            await this.setSource();
            this.loading = false;
            if (this.file.current.playType == 'video') this.loadVideo();
            else if (this.file.current.playType == 'img') this.imgReset();
        },
        query() {
            return this.http.get({ isEncrypt: true, idPath: this.id, url: `${this.api}/file/${this.id}` }).then(res => this.file = res.data);
        },
        async setSource() {
            if (this.file.current.playType == '') return;
            if (this.file.current.isEncrypt) {
                await this.getKey();
                if (this.file.current.playType == 'video') {
                    if (this.file.current.hasThumb) this.posterUrl = await this.http.getBlobUrl({ isEncrypt: true, idPath: this.id, key: this.key, iv: this.iv, url: `${this.api}/thumb/0/${this.id}` });
                    this.url = `${this.api}/m3u8/${this.file.current.id}?Token=${this.headers.Token}&MediaLibToken=${this.headers.MediaLibToken}`;
                    this.file.current.type = 'm3u8';
                } else {
                    let url = `${this.api}/${this.id}`;
                    let blob = await this.http.getBlob({ isEncrypt: true, idPath: this.id, key: this.key, iv: this.iv, url: url });
                    if (this.file.current.playType == 'txt') {
                        this.txt = await blob.text();
                    }
                    else if (this.file.current.playType == 'img' || this.file.current.playType == 'audio') {
                        this.url = URL.createObjectURL(blob);
                    }
                    else { }//more...
                }
            } else {
                if (this.file.current.playType == 'video') {
                    if (this.file.current.hasThumb) this.posterUrl = await this.http.getBlobUrl({ idPath: this.id, url: `${this.api}/thumb/0/${this.id}` });
                    this.url = `${this.api}/${this.id}?Token=${this.headers.Token}&MediaLibToken=${this.headers.MediaLibToken}`;
                } else {
                    let url = `${this.api}/${this.id}`;
                    let blob = await this.http.getBlob({ idPath: this.id, url: url });
                    if (this.file.current.playType == 'txt') {
                        this.txt = await blob.text();
                    }
                    else if (this.file.current.playType == 'img' || this.file.current.playType == 'audio') {
                        this.url = URL.createObjectURL(blob);
                    }
                    else { }//more...
                }
            }
        },
        async getKey() {
            if (this.key != '') return;
            if (!this.file.current.isEncrypt) return;
            let response = await this.http.get({ url: `${this.api}/key/${this.id}`, idPath: this.id });
            let tempKey = getCryptoId();
            this.key = aesDecrypt(response.data.key, tempKey);
            this.iv = response.data.iv;
        },
        async loadVideo() {
            await new Promise((resolve) => {
                let timer = setInterval(() => {
                    if (this.$refs.video) {
                        resolve();
                        clearTimeout(timer);
                    }
                }, 10);
            });
            let config = {
                el: this.$refs.video,
                url: this.url,
                poster: this.posterUrl,
                width: '100%',
                height: '100%',
                playbackRate: [0.1, 0.5, 0.75, 1, 1.5, 2, 5],
                cssFullscreen: true,
                rotateFullscreen: true,
                plugins: [],
                autoplay: true,
                imgTouchPoint: null
            };
            if (this.file.current.position) {
                config.startTime = Number.parseFloat(this.file.current.position);
            }
            if (this.file.current.type == 'mp4') {
                config.plugins.push(window.Mp4Plugin);
                config.mp4plugin = {
                    reqOptions: {
                        mode: 'cors',
                        method: 'GET',
                        headers: this.headers
                    }
                }
            }
            else if (this.file.current.type == 'm3u8') {
                config.plugins.push(window.HlsJsPlugin);
                config.hlsJsPlugin = {
                    hlsOpts: {
                        xhrSetup: (xhr, url) => {
                            xhr.setRequestHeader("Token", this.headers.Token);
                            xhr.setRequestHeader("MediaLibToken", this.headers.MediaLibToken);
                        },
                    },
                };
            }
            this.videoPlayer = new window.Player(config);
            this.videoPlayer.on('error', (error) => {
                console.log(111,error);
            });
            this.videoPlayer.on('fullscreen_change',isFullScreen=>{
                if(isFullScreen) {
                    if(this.http.fullScreen) this.http.fullScreen();
                }
                else{
                    if(this.http.exitFullScreen) this.http.exitFullScreen();
                }
            });
        },
        goBack() {
            this.$router.go(-1);
        },
        pre() {
            this.id = this.file.pre.idPath;
        },
        next() {
            this.id = this.file.next.idPath;
        },
        getElementPostion() {
            return this.videoPlayer?.currentTime;
        },
        setHistory(position, init) {
            if (!this.file.current) return;
            let history = {
                name: this.file.current.name,
                position: position
            };
            if (init) {
                this.addHistory(history);
                return;
            }

            if (!position) return;
            if (this.file.current.playType == 'video') {
                this.addHistory(history);
            }
        },
        addHistory(request) {
            this.http.post({ url: `${this.http.baseUrl}/api/history/${this.id}`, data: request, idPath: this.file.current.idPath });
        },
        openBrowser() {
            this.http.openBrowser(this.id);
        },
        imgReset() {
            this.imgStyle = {
                scale: 1,
                rotate: "0deg",
                translate: '0px 0px',
                maxWidth: '100%',
                maxHeight: '100%',
            };
        },
        setImgScale(scale) {
            scale = Math.min(this.maxImgScaleSize, scale);
            scale = Math.max(this.minImgScaleSize, scale);
            this.imgStyle.scale = scale;
            this.setImgMove(0, 0);
        },
        imgMouseScroll(event) {
            let delta = (event.wheelDelta && (event.wheelDelta > 0 ? 1 : -1)) || (event.detail && (event.detail > 0 ? -1 : 1));
            let unit = (delta > 0 ? 1 : -1) * 0.04;
            let scale = this.imgStyle.scale + unit;
            this.setImgScale(scale);
        },
        imgScaleUp() {
            this.setImgScale(this.imgStyle.scale + .2);
        },
        imgScaleDown() {
            this.setImgScale(this.imgStyle.scale - .2);
        },
        imgClockwiseRotate() {
            let angle = Number.parseInt(this.imgStyle.rotate) + 90;
            if (angle >= 360) angle = 0;
            this.setImgRotate(angle);
        },
        imgAnticlockwiseRotate() {
            let angle = Number.parseInt(this.imgStyle.rotate) - 90;
            if (angle < 0) angle = 270;
            this.setImgRotate(angle);
        },
        setImgRotate(angle) {
            this.imgStyle.rotate = `${angle}deg`;
            let isReverse = (angle == 90 || angle == 270);
            if (isReverse) {
                this.imgStyle.maxWidth = `${this.$refs.imgContainer.clientHeight}px`;
                this.imgStyle.maxHeight = `${this.$refs.imgContainer.clientWidth}px`;
            } else {
                this.imgStyle.maxWidth = '100%';
                this.imgStyle.maxHeight = '100%';
            }
            this.setImgMove(0, 0);
        },
        imgTouchStart(event) {
            if (event.targetTouches.length == 2) {
                let point1 = { X: event.targetTouches[0].clientX, Y: event.targetTouches[0].clientY }
                let point2 = { X: event.targetTouches[1].clientX, Y: event.targetTouches[1].clientY }
                this.imgTouchStartScale = Math.sqrt(Math.pow(Math.abs(point1.X - point2.X), 2) + Math.pow(Math.abs(point1.Y - point2.Y), 2));
            }
            else if (event.targetTouches.length === 1) {
                if (this.imgStyle.scale <= 1) {
                    this.imgTouchPoint = null;
                    return;
                }
                this.imgTouchPoint = { X: event.targetTouches[0].clientX, Y: event.targetTouches[0].clientY };
            }
        },
        imgTouchEnd(event) {
            this.imgTouchPoint = null;
            this.imgTouchStartScale = null;
        },
        imgTouchMove(event) {
            if (event.targetTouches.length == 2) {
                if (!this.imgTouchStartScale) return;

                let scaleStartPoint = { X: event.targetTouches[0].clientX, Y: event.targetTouches[0].clientY };
                let scaleEndPoint = { X: event.targetTouches[1].clientX, Y: event.targetTouches[1].clientY };
                let scaleEndValue = Math.sqrt(Math.pow(Math.abs(scaleStartPoint.X - scaleEndPoint.X), 2) + Math.pow(Math.abs(scaleStartPoint.Y - scaleEndPoint.Y), 2));
                let unit = (scaleEndValue - this.imgTouchStartScale) / this.imgStyle.scale / 100;
                let scale = this.imgStyle.scale + unit;
                this.setImgScale(scale);
                this.imgTouchStartScale = scaleEndValue;
            }
            else if (event.targetTouches.length === 1) {
                if (!this.imgTouchPoint) return;

                let point = { X: event.targetTouches[0].clientX, Y: event.targetTouches[0].clientY };
                let movePoint = { X: point.X - this.imgTouchPoint.X, Y: point.Y - this.imgTouchPoint.Y };
                this.setImgMove(movePoint.X, movePoint.Y);
                this.imgTouchPoint = point;
            }
        },
        setImgMove(moveX, moveY) {
            let containerSize = { width: this.$refs.imgContainer.clientWidth, height: this.$refs.imgContainer.clientHeight };
            let size = { width: this.$refs.img.clientWidth, height: this.$refs.img.clientHeight };
            let angle = Number.parseInt(this.imgStyle.rotate);
            if (angle == 90 || angle == 270) size = { height: this.$refs.img.clientWidth, width: this.$refs.img.clientHeight };
            let actualSize = { width: size.width * this.imgStyle.scale, height: size.height * this.imgStyle.scale }
            let maxMoveSize = { X: (actualSize.width - containerSize.width) / 2, Y: (actualSize.height - containerSize.height) / 2 };
            let translate = this.imgStyle.translate.split(' ');
            let x = Number.parseFloat(translate[0]);
            let y = Number.parseFloat(translate[1]);

            //x
            if (actualSize.width > containerSize.width) {
                if (moveX > 0) {
                    x = Math.min(x + moveX, maxMoveSize.X);
                } else if (moveX < 0) {
                    x = Math.max(x + moveX, -maxMoveSize.X);
                } else {
                    x = Math.min(x, maxMoveSize.X);
                    x = Math.max(x, -maxMoveSize.X);
                }
            } else x = 0;

            //y
            if (actualSize.height > containerSize.height) {
                if (moveY > 0) {
                    y = Math.min(y + moveY, maxMoveSize.Y);
                } else if (moveY < 0) {
                    y = Math.max(y + moveY, -maxMoveSize.Y);
                } else {
                    y = Math.min(y, maxMoveSize.Y);
                    y = Math.max(y, -maxMoveSize.Y);
                }
            } else y = 0;
            this.imgStyle.translate = `${x}px ${y}px`;
        },
        openBrowser() {
            this.operationShow = false;
            this.http.openBrowser(this.file.current.idPath);
        },
        favorite() {
            this.operationLoading = true;
            let option =
            {
                url: `${this.http.baseUrl}/api/favorite/${this.file.current.idPath}`,
                data: { idPath: this.file.current.idPath, isFolder: false, name: this.file.current.name },
                idPath: this.file.current.idPath,
                loadingCallback: () => this.operationLoading = false
            };
            this.http.post(option).then((res) => {
                this.file.current.isFavorited = true;
                this.file.current.favoritedId = res.data;
                this.operationShow = false;
            });
        },
        unFavorite() {
            this.operationLoading = true;
            let option =
            {
                url: `${this.http.baseUrl}/api/favorite/${this.file.current.favoritedId}`,
                idPath: this.file.current.idPath,
                loadingCallback: () => this.operationLoading = false
            }
            this.http.delete(option).then(() => {
                this.file.current.isFavorited = false;
                this.file.current.favoritedId = '00000000-0000-0000-0000-000000000000';
                this.operationShow = false;
            });
        },
        remove() {
            this.operationShow = false;
            this.notify.confirm(`确定要删除文件【${this.file.current.name}】`).then(res => {
                if (res) {
                    let option =
                    {
                        url: `${this.api}/${this.file.current.idPath}/${this.file.current.isFolder}`,
                        idPath: this.file.current.idPath,
                    }
                    this.http.delete(option).then(() => {
                        if (this.file.hasPre) this.pre();
                        else if (this.file.hasNext) this.next();
                        else this.goBack();
                    });
                }
            });
        },
        download() {
            if (this.downloading) {
                this.notify.warning('请等待下载完成');
                return;
            }
            this.operationShow = false;
            this.http.download({ idPath: this.file.current.idPath, name: this.file.current.name, bigFile: this.file.current.bigFile }, null, p => {
                sessionStorage.setItem('fileOperating', true);
                this.downloading = true;
                this.downloadingProgress = p;
            }).then(() => {
                this.downloading = false;
                this.downloadingProgress = null;
                sessionStorage.removeItem('fileOperating');
            });
        },
        goBackFolder() {
            this.operationShow = false;
            sessionStorage.setItem('fileid', this.file.current.parentIdPath);
            this.$router.push({ name: 'file', params: { id: new Date().getTime() } });
        },
        refresh() {
            location.reload();
        }
    }
}