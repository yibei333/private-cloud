export default {
    props: ['class'],
    watch: {
        class() {
            this.setInnerClassName();
        }
    },
    data() {
        return {
            componentName: '',
            notify: {},
            className: '',
        }
    },
    mounted() {
        this.componentName = this.$.type.meta.name;
        this.notify = new notify();
        this.setInnerClassName();
    },
    methods: {
        setInnerClassName() {
            this.className = `${this.componentName || ''} ${this.class || ''}`;
        }
    }
}