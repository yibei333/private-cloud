export default {
    data() {
        return {
            lockBase64: staticImages.lock,
            folderBase64: staticImages.folder,
            queryRequest: {},
            items: []
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/medialib`;
        this.query();
    },
    methods: {
        query() {
            this.http.get({ url: `${this.api}/authed` }).then(res => this.items = res.data);
        },
        jump(item) {
            this.$router.push({ name: 'file', params: { id: getFileIdByIdPath(item.idPath) } });
        }
    },
}