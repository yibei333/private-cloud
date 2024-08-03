
export default {
    data() {
        return {
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
            yesOrNoOptions: [
                { text: '是', value: true },
                { text: '否', value: false },
            ]
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/user`;
        this.query();
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
        showAddModal() {
            this.addRequest = {
                isForbidden: false
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
                .confirm(`确定要删除用户【${item.name}】?`)
                .then(res => {
                    if (res) this.http.delete({ url: `${this.api}/${item.id}` }).then(() => this.query());
                });
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}