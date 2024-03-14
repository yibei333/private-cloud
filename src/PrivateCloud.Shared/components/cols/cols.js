export default {
    props: {
        smCols: {
            type: String,
            default: ''
        },
        mediumCols: {
            type: String,
            default: ''
        },
        largeCols: {
            type: String,
            default: ''
        },
        xLargeCols: {
            type: String,
            default: ''
        },
        xxLargeCols: {
            type: String,
            default: ''
        },
        gap: {
            type: String,
            default: ''
        },
    },
    watch: {
        '$root.size'() {
            this.size = this.$root.size;
            this.setStyle();
        }
    },
    data() {
        return {
            size: 0,
            offset: false
        }
    },
    mounted() {
        this.size = this.$root.size;
        this.setStyle();
    },
    methods: {
        setStyle() {
            let cols = '';
            if (this.size == 5) cols = this.xxLargeCols || this.xLargeCols || this.largeCols || this.mediumCols || this.smCols;
            else if (this.size == 4) cols = this.xLargeCols || this.largeCols || this.mediumCols || this.smCols;
            else if (this.size == 3) cols = this.largeCols || this.xLargeCols || this.mediumCols || this.smCols;
            else if (this.size == 2) cols = this.mediumCols || this.largeCols || this.xLargeCols || this.smCols;
            else cols = this.smCols || this.mediumCols || this.largeCols || this.xLargeCols;

            let style = `
                grid-template-columns: ${cols};
                grid-gap: ${this.gap};
                padding: ${this.gap};
            `;
            this.$refs.cols.style = style;
        }
    }
}