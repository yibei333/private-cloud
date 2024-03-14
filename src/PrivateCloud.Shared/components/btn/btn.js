export default {
    props: {
        async: {
            type: Boolean,
            default: false
        },
        disabled: {
            type: Boolean,
            default: false
        },
        text: {
            type: String,
            default: null
        },
        title: {
            type: String,
            default: null
        },
        size: {
            type: String,
            default: 'default'
        },
        width: {
            type: String,
        },
        height: {
            type: String,
        },
        type: {
            type: String,
            default: 'default'
        },
        fullwidth: {
            type: Boolean,
            default: false
        },
        fullheight: {
            type: Boolean,
            default: false
        },
        circle: {
            type: Boolean,
            default: false
        },
        stop: {
            type: Boolean,
            default: false
        },
    },
    emits: ['click'],
    data() {
        return {
            loading: false,
            innerClass: ''
        }
    },
    mounted() {
        if (this.type == 'primary') this.innerClass += ' primary';
        else if (this.type == 'info') this.innerClass += ' info';
        else if (this.type == 'success') this.innerClass += ' success';
        else if (this.type == 'warning') this.innerClass += ' warning';
        else if (this.type == 'danger') this.innerClass += ' danger';
        else this.innerClass += ' default';

        if (this.size == 'small') this.innerClass += ' small';
        else if (this.size == 'large') this.innerClass += ' large';

        if (this.fullwidth) this.innerClass += ' full-width';
        if (this.fullheight) this.innerClass += ' full-height';
        if (this.circle) this.innerClass += ' circle';
    },
    methods: {
        click(event) {
            if (this.stop) event.stopPropagation();
            if (this.async) {
                this.loading = true;
                this.$emit('click', () => {
                    if (this.async) this.loading = false;
                });
            } else this.$emit('click');
        }
    }
}