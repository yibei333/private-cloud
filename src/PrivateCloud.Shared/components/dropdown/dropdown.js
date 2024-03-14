export default {
    props: {
        readonly: {
            type: Boolean,
            default: false
        },
        disabled: {
            type: Boolean,
            default: false
        },
        title: {
            type: String,
            default: ''
        },
        stop: {
            type: Boolean,
            default: false
        }
    },
    watch: {
        active() {
            this.contentClassName = this.active ? 'active' : 'hide'
        }
    },
    data() {
        return {
            active: false,
            contentClassName: ''
        }
    },
    mounted() {
        document.addEventListener('click', e => {
            if (!this.$refs.dropdown) return;
            if (!this.$refs.dropdown.contains(e.target)) this.active = false;
        }, true);
    },
    methods: {
        click(event) {
            if (this.stop) event.stopPropagation();
            if (this.disabled || this.readonly) return;
            this.active = !this.active;
        },
    }
}