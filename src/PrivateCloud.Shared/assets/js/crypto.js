function getCryptoId() {
    try {
        return JSON.parse(localStorage.getItem('user')).cryptoId;
    } catch (e) {
        location.replace('/index.html#/login');
    }
}

function aesEncrypt(strData, keyString, ivString = "0000000000000000") {
    let key = getAesCryptoKey(keyString);
    let iv = getAesCryptoIV(ivString);
    return CryptoJS.AES.encrypt(strData, key, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    }).toString();
}

function aesDecrypt(encryptedBase64String, keyString, ivString = "0000000000000000") {
    let key = getAesCryptoKey(keyString);
    let iv = getAesCryptoIV(ivString);
    let decrypted = CryptoJS.AES.decrypt(encryptedBase64String, key, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    });
    return CryptoJS.enc.Utf8.stringify(decrypted);
}

async function aesEncryptBlob(blob, keyString, ivString = "0000000000000000") {
    let key = getAesCryptoKey(keyString);
    let iv = getAesCryptoIV(ivString);
    let buffer = await blob.arrayBuffer();
    let wordArray = CryptoJS.lib.WordArray.create(buffer);
    let encrypted = CryptoJS.AES.encrypt(wordArray, key, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    });
    let encryptedWords = CryptoJS.enc.Base64.parse(encrypted.toString());
    let typedArray = convertWordArrayToUint8Array(encryptedWords);
    return new Blob([typedArray]);
}

async function aesDecryptBlob(blob, keyString, ivString = "0000000000000000") {
    let base64 = await blobToBase64(blob)
    let key = getAesCryptoKey(keyString);
    let iv = getAesCryptoIV(ivString);
    let decrypted = CryptoJS.AES.decrypt(base64, key, {
        iv: iv,
        mode: CryptoJS.mode.CBC,
        padding: CryptoJS.pad.Pkcs7
    });
    let typedArray = convertWordArrayToUint8Array(decrypted);
    return new Blob([typedArray], { type: blob.type });
}

async function blobToBase64(blob) {
    let buffer = await blob.arrayBuffer();
    let bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) binary += String.fromCharCode(bytes[i]);
    return window.btoa(binary);
}

function getAesCryptoKey(keyString) {
    let uint8Array = new TextEncoder().encode(keyString);
    let arrayLength = uint8Array.length;
    if (arrayLength == 16 || arrayLength == 24 || arrayLength == 32) return CryptoJS.lib.WordArray.create(uint8Array);

    let array = Array.from(uint8Array);
    let length = 32;
    if (arrayLength < 16) length = 16;
    else if (arrayLength > 16 && arrayLength < 24) length = 24;

    if (arrayLength > 32) {
        array = array.slice(0, 32);
    } else {
        for (let index = 0; index < (length - arrayLength); index++) {
            array.push(0);
        }
    }
    return CryptoJS.lib.WordArray.create(new Uint8Array(array));
}

function getAesCryptoIV(ivString) {
    let uint8Array = new TextEncoder().encode(ivString);
    let arrayLength = uint8Array.length;
    if (arrayLength == 16) return CryptoJS.lib.WordArray.create(uint8Array);
    let array = Array.from(uint8Array);
    if (arrayLength > 16) {
        array = array.slice(0, 16);
    } else {
        for (let index = 0; index < (16 - arrayLength); index++) {
            array.push(0);
        }
    }
    return CryptoJS.lib.WordArray.create(new Uint8Array(array));
}

function convertWordArrayToUint8Array(wordArray) {
    let arrayOfWords = wordArray.words ?? [];
    let length = wordArray.sigBytes ?? arrayOfWords.length * 4;
    let uInt8Array = new Uint8Array(length);
    let index = 0;
    for (let i = 0; i < length; i++) {
        let word = arrayOfWords[i];
        uInt8Array[index++] = word >> 24;
        uInt8Array[index++] = (word >> 16) & 0xff;
        uInt8Array[index++] = (word >> 8) & 0xff;
        uInt8Array[index++] = word & 0xff;
    }
    return uInt8Array;
}