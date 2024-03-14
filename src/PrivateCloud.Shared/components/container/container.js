export default {
    watch: {
        '$root.size'() {
            this.size = this.$root.size;
            this.setClassName();
        }
    },
    data() {
        return {
            size: 0,
            innerClass: '',
        }
    },
    mounted() {
        this.size = this.$root.size;
        this.setClassName();
    },
    methods: {
        setClassName() {
            let name = '';
            if (this.size == 1) name += ' small';
            else if (this.size == 2) name += ' medium';
            else if (this.size == 3) name += ' large';
            else if (this.size == 4) name += ' xlarge';
            else if (this.size == 5) name += ' xxlarge';
            this.innerClass = name;
        }
    }
}