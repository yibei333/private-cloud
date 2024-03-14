export default {
    props: {
        show: {
            type: Boolean,
            default: false
        },
        async: {
            type: Boolean,
            default: false
        },
        hideCancle: {
            type: Boolean,
            default: false
        },
        hideSubmit: {
            type: Boolean,
            default: false
        },
        title: {
            type: String,
            default: ''
        },
        cancleText: {
            type: String,
            default: '取消'
        },
        submitText: {
            type: String,
            default: '提交'
        },
        size: {
            type: String,
            default: 'medium'
        }
    },
    emits: ["update:show", "submit"],
    watch: {
        show() {
            if (this.show) {
                this.showing = true;
                let timer = setTimeout(() => {
                    this.innerClassName = 'show';
                    clearTimeout(timer)
                }, 1);
            } else {
                this.innerClassName = 'hide';
                let timer = setTimeout(() => {
                    this.showing = false;
                    clearTimeout(timer)
                }, 250);
            }
        }
    },
    data() {
        return {
            innerClassName: '',
            showing: false,
            loading: false,
        }
    },
    methods: {
        cancle() {
            this.$emit('update:show', false);
        },
        submit() {
            if (this.async) {
                this.loading = true;
            }
            this.$emit('submit', (close = true) => {
                if (close) this.$emit('update:show', false);
                if (this.async) this.loading = false;
            });
        }
    }
}