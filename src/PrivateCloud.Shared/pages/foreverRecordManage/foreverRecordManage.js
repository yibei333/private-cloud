
export default {
    data() {
        return {
            queryRequest: {
                pageIndex: 1,
                pageSize: 20,
                name: ''
            },
            items: [],
            pageData: {
                total: 0,
                pageCount: 0
            },
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/foreverRecord`;
        this.query();
    },
    methods: {
        query() {
            this.http.get({ url: `${this.api}`, data: this.queryRequest }).then(res => {
                this.items = res.data;
                this.queryRequest.pageIndex = res.pageIndex;
                this.queryRequest.pageSize = res.pageSize;
                this.pageData.total = res.total;
                this.pageData.pageCount = res.pageCount;
            });
        },
        remove(item) {
            this.notify
                .confirm(`确定要删除【${item.name}】的永久链接?`)
                .then(res => {
                    if (res) this.http.delete({ url: `${this.api}/${item.id}` }).then(() => this.query());
                });
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}