export default {
    data() {
        return {
            componentName: 'fields',
            children: []
        }
    },
    methods: {
        register(ref) {
            this.children.push(ref);
        },
        verify() {
            if (!this.children || this.children.length <= 0) return true;
            let flag = true;
            for (let i = 0; i < this.children.length; i++) {
                let verifyResult = this.children[i].verify();
                if (!verifyResult) flag = false;
            }
            return flag;
        }
    }
}