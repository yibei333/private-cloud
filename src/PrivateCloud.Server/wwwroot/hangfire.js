$('.navbar-right').hide();

window.addEventListener('message', (e) => {
    sessionStorage.setItem('token', e.data.token);
    setAjaxHeader();
    setUrlToken();
}, false);

window.onload = () => {
    if (window.parent) {
        window.parent.postMessage('loaded', "*");
    }
};

function setAjaxHeader() {
    let token = sessionStorage.getItem('token');
    if (!token) return;
    $.ajaxSetup({
        beforeSend: function (xhr, settings) {
            xhr.setRequestHeader('Token', token);
        },
        error: function (xhr, status, error) {
            if (status == 401) {
                window.parent.postMessage('unauthorized', "*");
            }
        }
    });
}

function setUrlToken() {
    let token = sessionStorage.getItem('token');

    let hrefs = document.querySelectorAll("[href]");
    for (let i = 0; i < hrefs.length; i++) {
        let href = hrefs[i].href;
        if (href?.startsWith('http') || href?.startsWith('/')) {
            if (href.indexOf('Token=') <= -1) hrefs[i].href = (href.indexOf('?') > -1) ? `${href}&Token=${token}` : `${href}?Token=${token}`;
        }
    }

    let srcs = document.querySelectorAll("[src]");
    for (let i = 0; i < srcs.length; i++) {
        let src = srcs[i].src;
        if (src?.startsWith('http') || src?.startsWith('/')) {
            if (src.indexOf('Token=') <= -1) srcs[i].src = (src.indexOf('?') > -1) ? `${src}&Token=${token}` : `${src}?Token=${token}`;
        }
    }
}

setAjaxHeader();
setUrlToken();