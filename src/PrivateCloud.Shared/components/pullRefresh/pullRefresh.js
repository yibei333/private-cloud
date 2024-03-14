import './pull-refresh.js'

export default {
    emits: ["refresh"],
    mounted() {
        this.$refs.pullRefresh.parentNode.classList.add('overflow-hidden');
    },
    activated() {
        this.init();
    },
    methods: {
        init() {
            PullToRefresh.destroyAll();
            let that = this;
            PullToRefresh.init({
                mainElement: this.$refs.pullRefresh,
                instructionsPullToRefresh: '下拉刷新',
                instructionsReleaseToRefresh: '释放刷新',
                instructionsRefreshing: '刷新中',
                onRefresh() { that.refresh() },
                onPulling() { that.pulling() },
                shouldPullToRefresh() {
                    if (!that.$refs.pullRefresh) return false;
                    return that.$refs.pullRefresh.scrollTop <= 10;
                }
            });
        },
        refresh() {
            this.$emit('refresh');
            let timer = setTimeout(() => {
                this.$refs.pullRefresh.classList.remove('pulling');
                this.$refs.pullRefresh.classList.remove('pulling-overflow-hidden');
                clearTimeout(timer);
            }, 500);
        },
        pulling() {
            let node = this.$refs.pullRefresh;
            if (!node) return;
            let parentNode = this.$refs.pullRefresh.parentNode;
            if (node.scrollHeight <= node.clientHeight || (node.scrollHeight < parentNode.clientHeight && (node.clientHeight + 80) > parentNode.clientHeight)) {
                this.$refs.pullRefresh.classList.add('pulling-overflow-hidden');
            } else this.$refs.pullRefresh.classList.add('pulling');
        },
    }
}