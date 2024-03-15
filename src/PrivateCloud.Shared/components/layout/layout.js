import { httpService } from "/services/httpService.js";

export default {
    props: ["size", "name"],
    components: {
        dropdown: Vue.defineAsyncComponent(() => importComponent('dropdown', []))
    },
    data() {
        return {
            notify: {},
            useHeader: true,
            useFooter: true,
            largeItems: [],
            smallItems: [],
            user: null,
            isAdmin: false,
        }
    },
    watch: {
        name() {
            this.setLayout();
        },
        size() {
            this.setLayout();
        }
    },
    mounted() {
        this.notify = new notify();
        this.setUser();
        this.setItems();
    },
    methods: {
        setLayout() {

            if (this.size == 1) {
                this.useHeader = false;
                if (this.name == 'login' || this.name == 'notfound' || this.name == 'play' || this.name == 'history' || this.name == 'favorite' || this.name == 'about' || this.name == 'userManage' || this.name == 'taskManage' || this.name == 'mediaLibManage' || this.name == 'foreverRecordManage' || this.name == 'upgradeManage') {
                    this.useFooter = false;
                } else this.useFooter = true;
            } else {
                this.useFooter = false;
                if (this.name == 'login' || this.name == 'notfound' || this.name == 'play') {
                    this.useHeader = false;
                } else this.useHeader = true;
            }
        },
        setUser() {
            let userJson = localStorage.getItem('user');
            if (userJson) {
                this.user = JSON.parse(userJson);
            }
            if (this.name != 'login' && !this.user) {
                location.replace('/index.html#/login');
                return;
            }
            if (this.user.roles) this.isAdmin = this.user.roles.indexOf('Admin') >= 0;
        },
        setItems() {
            this.largeItems = pagesConfig.filter(x => (x.type == 0 || x.type == 1) && !x.parentId && (!x.isAdmin || (x.isAdmin && this.isAdmin)));
            if (new httpService().isClient) this.largeItems.push(pagesConfig.filter(x => x.name == 'about')[0]);
            for (let i = 0; i < this.largeItems.length; i++) {
                let item = this.largeItems[i];
                let children = pagesConfig.filter(x => item.id == x.parentId);
                if (children && children.length > 0) item.children = children;
            }
            this.smallItems = pagesConfig.filter(x => x.type == 0 || x.type == 2);
        },
        nav(name) {
            if (this.$refs.navdropdown && this.$refs.navdropdown.length > 0) this.$refs.navdropdown[0].active = false;
            if (name == 'file') {
                let fileId = getLastIdPath();
                if (fileId) {
                    this.$router.push({ name: name, params: { id: getFileIdByIdPath(fileId) } });
                } else {
                    this.notify.confirm('请选择一个媒体库', { hideCancle: true }).then(() => {
                        this.$router.push({ name: 'mediaLib' });
                    });
                }
                return;
            }
            this.$router.push({ name: name });
        },
        logout() {
            this.notify.confirm('确定要退出登录吗?').then(res => {
                if (res) {
                    localStorage.removeItem('user');
                    location.replace('/index.html#/login');
                }
            });
        }
    },
}