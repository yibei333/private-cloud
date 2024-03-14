
export default {
    data() {
        return {
            queryRequest: {
                pageIndex: 1,
                pageSize: 20,
                version: ''
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
            platformOptions: []
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/upgrade`;
        this.queryOptions();
        this.query();
    },
    methods: {
        queryOptions() {
            this.http.get({ url: `${this.api}/options` }).then(res => {
                this.platformOptions = res.data.filter(x => x.name == 'Platforms')[0].options.map(x => { return { text: x.key, value: x.value } });
            });
        },
        query() {
            this.http.get({ url: `${this.api}`, data: this.queryRequest }).then(res => {
                this.items = res.data;
                this.queryRequest.pageIndex = res.pageIndex;
                this.queryRequest.pageSize = res.pageSize;
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
            this.addRequest = {};
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
                .confirm(`确定要删除【${item.platformName}】版本【${item.version}】?`)
                .then(res => {
                    if (res) this.http.delete({ url: `${this.api}/${item.id}` }).then(() => this.query());
                });
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}