
export default {
    data() {
        return {
            user: {},
            isAdmin: false
        }
    },
    mounted() {
        this.getUserInfo();
    },
    methods: {
        getUserInfo() {
            this.user = getUser();
            if (!this.user) {
                location.replace('/index.html#/login');
                return;
            }
            this.isAdmin = this.user.roles.indexOf('Admin') > -1;
        },
        nav(name) {
            this.$router.push({ name: name });
        }
    },
}