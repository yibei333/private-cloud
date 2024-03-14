const platforms = [
    { key: 'android', value: 1 },
    { key: 'ios', value: 2 },
    { key: 'iphone', value: 2 },
    { key: 'windows', value: 3 },
    { key: 'mac os', value: 4 },
    { key: 'linux', value: 5 },
];

function getPlatform() {
    let ua = navigator.userAgent.toLowerCase();
    let platform = platforms.filter(x => ua.indexOf(x.key) > -1);
    if (!platform || platform.length <= 0) return 0;
    return platform[0].value;
}

window.onbeforeunload = (e) => {
    if (sessionStorage.getItem('fileOperating')) {
        return false;
    }
};

window.onload = () => {
    sessionStorage.removeItem('fileOperating');
}

function toHex(source) {
    let result = '';
    for (let i = 0; i < source.length; i++) {
        result += source.charCodeAt(i).toString(16);
    }
    return result;
}

function fromHex(hex) {
    if (hex.length % 2 != 0) throw '格式错误';

    let num;
    let arr = [];
    for (let i = 0; i < hex.length; i = i + 2) {
        num = parseInt(hex.substr(i, 2), 16);
        arr.push(String.fromCharCode(num));
    }
    return arr.join('');
}

function clone(obj) {
    if (!obj) return obj;
    return JSON.parse(JSON.stringify(obj));
}

Date.prototype.format = function (format = 'yyyy-MM-dd HH:mm:ss.S') {
    let obj = {
        "y+": this.getFullYear(),
        "M+": this.getMonth() + 1,
        "d+": this.getDate(),
        "H+": this.getHours(),
        "m+": this.getMinutes(),
        "s+": this.getSeconds(),
        "q+": Math.floor((this.getMonth() + 3) / 3),
        "S": this.getMilliseconds()
    };
    for (let key in obj) {
        var match = format.match(key);
        if (match) {
            let matchString = match[0];
            let matchStringLength = matchString.length;
            format = format.replace(match[0], obj[key].toString().padStart(matchStringLength, '0'));
        }
    }
    return format;
}

Date.prototype.formatDate = function () {
    return this.format("yyyy-MM-dd");
}

Date.prototype.formatDateTime = function () {
    return this.format("yyyy-MM-dd HH:mm:ss");
}

function randomCode(length) {
    let array = [];
    for (let i = 0; i < length; i++) {
        array.push(Math.round(Math.random() * 9));
    }
    return array.join('');
}

function imgToBase64(file) {
    return new Promise((resolve) => {
        var reader = new FileReader();
        reader.onload = (res) => resolve(res.target.result);
        reader.readAsDataURL(file);
    });
}

function getUser() {
    try {
        let userJson = localStorage.getItem('user');
        if (userJson) {
            return JSON.parse(userJson);
        }
    } catch { }
    return null;
}

function getToken() {
    return getUser()?.token;
}

function queryString(key) {
    var query = window.location.search.substring(1);
    var map = query.split("&");
    for (var i = 0; i < map.length; i++) {
        var pair = map[i].split("=");
        if (pair[0] == key) {
            return pair[1];
        }
    }
}

function toQueryString(obj, prefixt = '?', ignoreEmptyString = true) {
    if (!obj || !(obj instanceof Object)) return '';

    let array = [];
    for (let key in obj) {
        let value = obj[key];
        if (!value || (ignoreEmptyString && value == '')) continue;
        array.push(`${key}=${value}`)
    }
    if (array.length <= 0) return '';
    return `${prefixt}${array.join('&')}`;
}