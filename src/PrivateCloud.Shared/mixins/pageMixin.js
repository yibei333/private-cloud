import { httpService } from "/services/httpService.js";

export default {
    data() {
        return {
            pageName: '',
            appid:'',
            api: null,
            http: {},
            notify: {}
        }
    },
    activated() {
        this.setTitle();
    },
    mounted() {
        this.notify = new notify();
        this.pageName = this.$.type.meta.name;
        this.appid = localStorage.getItem('appid');
        this.http = new httpService();
    },
    methods: {
        setTitle() {
            let pageResult = pagesConfig.filter(x => x.name == this.pageName);
            let title = (pageResult && pageResult.length > 0) ? `私有云-${pageResult[0].text}` : `私有云`;
            let titleElements = document.head.getElementsByTagName('title');
            if (titleElements && titleElements.length > 0) {
                titleElements[0].innerText = title;
            } else {
                let titleElement = document.createElement('title');
                titleElement.innerText = title;
                document.head.appendChild(titleElement);
            }
        },
    },
}