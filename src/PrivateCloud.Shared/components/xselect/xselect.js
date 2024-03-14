export default {
    props: {
        id: {
            type: String,
            default: ''
        },
        readonly: {
            type: Boolean,
            default: false
        },
        disabled: {
            type: Boolean,
            default: false
        },
        modelValue: {},
        options: {
            type: Array,
            default: []
        }
    },
    emits: ["update:modelValue", "change"],
    watch: {
        modelValue() {
            this.setValue();
        }
    },
    computed: {
        text: function () {
            let options = this.options.filter(x => x.value == this.value);
            let txt = (options && options.length > 0) ? options[0].text : '';
            if (this.readonly || this.disabled) return txt;
            if (txt == '') return '请选择';
            return txt;
        }
    },
    data() {
        return {
            value: null
        }
    },
    mounted() {
        this.setValue();
    },
    methods: {
        setValue() {
            this.value = this.modelValue;
        },
        change(item) {
            this.$emit('update:modelValue', item.value);
            this.$emit('change');
            this.$refs.input.focus();
            this.$refs.select.active = false;
        },
    }
}