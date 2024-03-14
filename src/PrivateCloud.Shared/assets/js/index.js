import { httpService } from "/services/httpService.js";

let routes = pagesConfig.filter(x => !x.isContainer).map(x => {
    return {
        path: x.path ?? `/${x.name}`,
        name: x.name,
        component: () => importPage(x.name, x.components),
        meta: { id: x.id },
        props: x.hasProps ?? false
    }
});
routes.splice(0, 0, { path: '/', redirect: '/mediaLib' },);

let routerData = VueRouter.createRouter({
    history: VueRouter.createWebHashHistory(),
    routes,
});

routerData.beforeEach((to, from) => {
    let fromName = from?.name;
    let toName = to?.name;

    //load to resource
    if (toName) {
        pageLoading();
        let toId = `section_${toName}`;
        let toElement = document.body.querySelector(`#${toId}`);
        if (!toElement) {
            let section = document.createElement('section');
            document.body.appendChild(section);
            section.id = toId;

            let metas = pagesConfig.filter(x => x.name == toName);
            let meta = (!metas || metas.length <= 0) ? null : metas[0];
            if (meta) {
                //static scripts
                let staticScripts = meta.scripts || [];
                for (let i = 0; i < staticScripts.length; i++) {
                    let staticScript = document.createElement('script');
                    getResourceUrl(staticScripts[i]).then(url => {
                        staticScript.src = url;
                    });
                    section.appendChild(staticScript);
                }
            }
        }
    }

    //unload from resource
    if (fromName && fromName != '/') {
        let fromId = `#section_${fromName}`;
        let fromElement = document.body.querySelector(fromId);
        if (fromElement) {
            let fromTimer = setTimeout(() => {
                fromElement.remove();
                clearTimeout(fromTimer);
            }, 300);
        }
    }
});

routerData.afterEach((to, from) => {
    let fromId = from?.meta?.id ?? -1;
    let toId = to?.meta?.id ?? -1;
    if (toId == -1 || fromId == -1) to.meta.transition = 'fade';
    else if (fromId > toId) to.meta.transition = 'to-small';
    else if (fromId < toId) to.meta.transition = 'to-big';
    else to.meta.transition = '';
    pageLoading(true);
    let http = new httpService();
    if (http.setWebHandleBack) http.setWebHandleBack(to?.name);
});

let app = Vue.createApp({
    components: {
        layout: Vue.defineAsyncComponent(async () => {
            let layout = await import(`/components/layout/layout.js`);
            layout.default.template = await getResourceContent(`/components/layout/layout.html`);
            return layout;
        }),
    },
    data() {
        return {
            size: 4,
            name: ''
        }
    },
    mounted() {
        window.onresize = this.setSize;
        this.setSize();
        this.$router.afterEach((to, from) => {
            this.name = to.name;
        });
    },
    methods: {
        setSize() {
            let width = window.innerWidth;

            if (width < 768) {
                this.size = 1;
            } else if (width < 1024) {
                this.size = 2;
            } else if (width < 1366) {
                this.size = 3;
            } else if (width < 1920) {
                this.size = 4;
            } else this.size = 5;
        },
    }
});
app.use(routerData);
app.mount('#vue-app');