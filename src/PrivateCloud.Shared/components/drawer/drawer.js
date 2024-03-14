export default {
    props: {
        active: {
            type: Boolean,
            default: false
        },
        loading: {
            type: Boolean,
            default: false
        },
    },
    emits: ['update:active'],
    watch: {
        active() {
            this.setShow();
        },
    },
    data() {
        return {
            contentClassName: '',
            show: false,
        }
    },
    methods: {
        setShow() {
            if (this.active) {
                this.show = true;
                let timer = setTimeout(() => {
                    this.contentClassName = 'active';
                    clearTimeout(timer);
                }, .1);
            }
            else {
                this.contentClassName = 'hide';
                let timer = setTimeout(() => {
                    this.show = false;
                    clearTimeout(timer);
                }, 300);
            }
        },
        outside() {
            if (!this.loading) this.$emit('update:active', false);
        }
    }
}