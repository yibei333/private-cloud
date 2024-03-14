import './infinite-scroll.js'

export default {
    components: {
        infiniteLoading: V3InfiniteLoading.default
    },
    emits: ["load", "retry", "reset"],
    props: ["identifier"],
    watch: {
        identifier() {
            this.id = this.identifier;
        }
    },
    data() {
        return {
            id: ''
        }
    },
    mounted() {
        this.id = this.identifier;
    },
    methods: {
        load(action) {
            this.$emit('load', action);
        },
        retry() {
            this.$emit('retry');
        },
        reset() {
            this.id = null;
            this.id = this.identifier;
            this.$emit('reset');
        }
    }
}