
export default {
    data() {
        return {
            url: null
        }
    },
    mounted() {
        let user = getUser();
        if(user.expire<=new Date().getTime()) {
            location.replace('/index.html#/login');
            return;
        }
        this.url = `${this.http.baseUrl}/hangfire?Token=${user.token}`;
        window.addEventListener('message', (e) => {
            if (e?.data == 'loaded') {
                if (this.$refs.frame?.contentWindow?.window) this.$refs.frame.contentWindow.window.postMessage(user, "*");
            } else if (e?.data == 'unauthorized') {
                location.replace('/index.html#/login');
            }
        });
    },
    methods: {
        goBack() {
            this.$router.go(-1);
        },
    },
}