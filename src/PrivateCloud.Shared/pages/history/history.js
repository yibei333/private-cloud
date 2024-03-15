export default {
    data() {
        return {
            request: {},
            pageCount: 0,
            items: [],
            operationShow: false,
            operationLoading: false,
            operationOptions: [],
            operateItem: null,
        }
    },
    mounted() {
        this.api = `${this.http.baseUrl}/api/history`;
        this.init();
    },
    methods: {
        async init(name = '') {
            this.request = {
                name: name,
                pageIndex: 1,
                pageSize: 20
            }
            this.items = [];
        },
        async getEntries() {
            let option = {
                url: this.api,
                data: this.request,
            };
            return this.http.get(option).then(res => {
                this.pageCount = res.pageCount;
                let data = res.data;
                for (let i = 0; i < data.length; i++) {
                    this.items.push(data[i]);
                }
                this.request.pageIndex++;
                return res.data;
            });
        },
        loadMore(state) {
            this.getEntries().then(data => {
                if (data.length >= this.request.pageSize) state.loaded();
                else state.complete();
            }).catch(() => state.error());
        },
        showOperation(item) {
            this.operateItem = item;
            this.operationShow = true;
            this.operationOptions = [];
            this.operationOptions.push({ id: 1, name: '跳转' });
            this.operationOptions.push({ id: 2, name: '删除', danger: true });
            this.operationOptions.push({ id: 3, name: '取消' });
        },
        operate(item) {
            if (item.id == 1) this.jump(this.operateItem.isFolder,this.operateItem.idPath);
            else if (item.id == 2) this.remove();
            else if (item.id === 3) this.operationShow = false;
            else this.notify.warning(`不支持的操作`);
        },
        remove() {
            this.operationShow = false;
            let option =
            {
                url: `${this.api}/${this.operateItem.id}`,
            }
            this.http.delete(option).then(() => {
                let index = this.items.indexOf(this.operateItem);
                this.items.splice(index, 1);
            });
        },
        jump(isFolder, idPath) {
            this.operationShow = false;
            if (isFolder) {
                this.$router.push({ name: 'file', params: { id: getFileIdByIdPath(idPath) } });
            } else {
                sessionStorage.setItem('playid', idPath);
                this.$router.push({ name: 'play' });
            }
        },
        goBack() {
            this.$router.go(-1);
        },
    },
}