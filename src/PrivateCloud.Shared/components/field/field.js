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
        label: {
            type: String,
            default: null
        },
        tip: {
            type: String,
            default: null
        },
        required: {
            type: Boolean,
            default: false
        },
        noPadding: {
            type: Boolean,
            default: false
        },
        type: {
            type: String,
            default: 'text'
        },
        placeholder: {
            type: String,
            default: null
        },
        modelValue: {},
        selectOptions: {
            type: Array,
            default: []
        }
    },
    emits: ["change", "update:modelValue", "enter"],
    watch: {
        modelValue() {
            this.setValue();
        }
    },
    data() {
        return {
            id: '',
            loading: false,
            className: 'btn',
            vplaceholder: '',
            error: null,
            value: null
        }
    },
    mounted() {
        this.id = `${this.label || randomCode(10)}_${new Date().getTime()}`;
        if (!this.readonly && !this.disabled) this.vplaceholder = this.placeholder || `请输入${this.label}`;
        this.setValue();
        if (this.$parent.componentName == 'fields') {
            this.$parent.register(this);
        }
    },
    methods: {
        setValue() {
            this.value = this.modelValue;
        },
        change() {
            this.$emit('update:modelValue', this.value);
            this.$emit('change');
            this.verify();
        },
        checkChanged(value) {
            this.$emit('update:modelValue', value);
            this.$emit('change');
        },
        verify() {
            if (this.required) {
                if (!this.value || this.value == '' || this.value == 0) {
                    this.error = `${this.label}不能为空`;
                    return false;
                }
                else this.error = null;
            }
            return true;
        },
    }
}