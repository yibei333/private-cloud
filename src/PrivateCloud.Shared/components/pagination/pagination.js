export default {
    props: {
        index: {
            type: Number,
            default: 0,
        },
        size: {
            type: Number,
            default: 20
        },
        sizes: {
            type: String,
            default: '10,20,50,100'
        },
        count: {
            type: Number,
            default: 0
        },
        pageCount: {
            type: Number,
            default: 0
        },
    },
    emits: [
        'changed',
        'update:index',
        'update:size',
    ],
    watch: {
        index() {
            if (this.index < 0) this.index = 0;
            else if (this.index > this.pageCount) this.index = this.pageCount;
            else this.calc();
        },
        size() {
            this.calc();
        },
        count() {
            this.calc();
        },
        sizes() {
            this.setsizes();
        },
        '$root.size'() {
            this.isSmall = this.$root.size == 1;
        }
    },
    data() {
        return {
            start: 0,
            end: 0,
            preDisabled: false,
            nextDisabled: false,
            numberSizes: [],
            isSmall: true
        }
    },
    mounted() {
        this.isSmall = this.$root.size == 1;
        this.calc();
        this.setsizes();
    },
    methods: {
        indexChanged(e) {
            let value = e.target.value;
            if (!value || value == '' || value < 0) {
                this.$emit('update:index', 0);
            }
            else if (value > this.pageCount) {
                this.$emit('update:index', this.pageCount);
            }
            else this.$emit('update:index', value);
            this.$emit('changed');
        },
        sizeChanged(size) {
            this.$emit('update:size', Number.parseInt(size));
            this.$emit('changed');
        },
        calc() {
            this.start = this.index * this.size + 1;
            this.end = (this.index) * this.size;
            if (this.end > this.count) this.end = this.count;
            this.preDisabled = this.pageCount <= 0 || this.index <= 0;
            this.nextDisabled = this.pageCount <= 0 || this.index >= this.pageCount;
        },
        pre() {
            this.$emit('update:index', this.index - 1);
            this.$emit('changed');
        },
        next() {
            this.$emit('update:index', this.index + 1);
            this.$emit('changed');
        },
        setsizes() {
            if (this.sizes && this.sizes != '') {
                this.numberSizes = this.sizes.split(',').map(x => Number.parseInt(x));
            } else {
                this.numberSizes = [10, 20, 50, 100];
            }
        }
    }
}