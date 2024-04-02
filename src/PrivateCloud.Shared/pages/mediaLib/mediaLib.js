export default {
    data() {
        return {
            lockBase64: staticImages.lock,
            folderBase64: staticImages.folder,
            queryRequest: {},
            items: [],
            isAdmin: false,
            inited: false
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/medialib`;
    },
    activated() {
        this.inited = false;
        this.init();
    },
    methods: {
        init() {
            this.isAdmin = getUserIsAdminRole();
            this.query();
        },
        query() {
            this.http.get({ url: `${this.api}/authed` }).then(res => {
                this.items = res.data;
                this.inited = true;
                if (this.items && this.items.length > 0) {
                    let interval = setInterval(() => {
                        if (this.$refs.pullRefresh) {
                            this.$refs.pullRefresh.init();
                            clearInterval(interval);
                        }
                    }, 10);
                }
            });
        },
        jump(item) {
            this.$router.push({ name: 'file', params: { id: getFileIdByIdPath(item.idPath) } });
        },
        goCreateMediaLib() {
            this.$router.push({ name: 'mediaLibManage', params: { goAdd: true } });
        }
    },
}