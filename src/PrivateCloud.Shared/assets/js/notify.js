class notify {
    notify(message, options, type) {
        let notifyContainer = document.body.querySelector('#notifyContainer');
        if (!notifyContainer) {
            notifyContainer = document.createElement('div');
            document.body.appendChild(notifyContainer);
            notifyContainer.id = 'notifyContainer';
            notifyContainer.className = 'notify-container';
        }
        let stopTime = options?.closeSeconds ?? 10;
        this.addMessageElement(notifyContainer, message, type, stopTime);
    }

    addMessageElement(notifyContainer, message, type, stopTime) {
        let messageElement = document.createElement('div');
        messageElement.className = 'message show';
        notifyContainer.appendChild(messageElement);
        let timer;
        if (stopTime > 0) {
            timer = setTimeout(() => {
                this.closeMessage(messageElement);
                clearTimeout(timer);
            }, stopTime * 1000);
        }
        messageElement.onmousedown = () => {
            if (timer) clearTimeout(timer);
        };

        let icon = '';
        let title = '';
        let className = '';
        if (type == 1) {
            icon = '<svg viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"><path d="M512 64C264.6 64 64 264.6 64 512s200.6 448 448 448 448-200.6 448-448S759.4 64 512 64z m0 820c-205.4 0-372-166.6-372-372s166.6-372 372-372 372 166.6 372 372-166.6 372-372 372z" p-id="1452"></path><path d="M512 336m-48 0a48 48 0 1 0 96 0 48 48 0 1 0-96 0Z"></path><path d="M536 448h-48c-4.4 0-8 3.6-8 8v272c0 4.4 3.6 8 8 8h48c4.4 0 8-3.6 8-8V456c0-4.4-3.6-8-8-8z"></path></svg>';
            title = '提示';
            className = 'info';
        } else if (type == 2) {
            icon = '<svg viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"><path d="M668.1 374.6L448 594.8l-92.2-92.1c-12.5-12.5-32.7-12.5-45.2 0s-12.5 32.7 0 45.2l114.8 114.8c12.5 12.5 32.8 12.5 45.3 0L713.4 420c12.5-12.5 12.5-32.8 0-45.3-12.5-12.6-32.8-12.6-45.3-0.1z"></path><path d="M828.8 195.2C744.8 111.2 630.8 64 512 64c-88.6 0-175.2 26.3-248.9 75.5-73.7 49.2-131.1 119.2-165 201.1-33.9 81.9-42.8 171.9-25.5 258.8 17.3 86.9 60 166.7 122.6 229.4s142.5 105.3 229.4 122.6c86.9 17.3 177 8.4 258.8-25.5 81.9-33.9 151.8-91.3 201.1-165C933.7 687.2 960 600.6 960 512c0-118.8-47.2-232.8-131.2-316.8z m-45.3 588.3C711.5 855.5 613.8 896 512 896c-75.9 0-150.2-22.5-213.3-64.7-63.2-42.2-112.4-102.2-141.5-172.3-29.1-70.2-36.7-147.4-21.9-221.9 14.8-74.5 51.4-142.9 105.1-196.6 53.7-53.7 122.1-90.3 196.6-105.1 74.5-14.8 151.7-7.2 221.9 21.9 70.2 29.1 130.1 78.3 172.3 141.4C873.5 361.8 896 436.1 896 512c0 101.8-40.5 199.5-112.5 271.5z"></path></svg>';
            title = '成功';
            className = 'success';
        } else if (type == 3) {
            icon = '<svg viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"><path d="M512 64C264.6 64 64 264.6 64 512s200.6 448 448 448 448-200.6 448-448S759.4 64 512 64z m0 820c-205.4 0-372-166.6-372-372s166.6-372 372-372 372 166.6 372 372-166.6 372-372 372z"></path><path d="M512 660m-48 0a48 48 0 1 0 96 0 48 48 0 1 0-96 0Z"></path><path d="M482 572h60c5.5 0 10-3.2 10-7.1V323.1c0-3.9-4.5-7.1-10-7.1h-60c-5.5 0-10 3.2-10 7.1v241.8c0 3.9 4.5 7.1 10 7.1z"></path></svg>';
            title = '警告';
            className = 'warning';
        } else {
            icon = '<svg viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"><path d="M512 938.6496a426.6496 426.6496 0 1 1 0-853.2992 426.6496 426.6496 0 0 1 0 853.2992z m0-366.2848l105.5744 105.5744a42.6496 42.6496 0 0 0 60.3648-60.3648L572.3648 512l105.5744-105.5744a42.6496 42.6496 0 0 0-60.3648-60.3648L512 451.6352 406.4256 346.112a42.6496 42.6496 0 1 0-60.3648 60.3648L451.6352 512 346.112 617.5744a42.6496 42.6496 0 1 0 60.3648 60.3648L512 572.3648z"></path></svg>';
            title = '错误';
            className = 'error';
        }

        let titleElement = document.createElement('div');
        messageElement.appendChild(titleElement);
        titleElement.className = `title ${className}`;
        titleElement.innerHTML = `
            <div class='title-content'>
                ${icon}
                ${title}
            </div>
        `;

        let closeButton = document.createElement('button');
        titleElement.appendChild(closeButton);
        closeButton.className = 'close-button';
        closeButton.title = '关闭';
        closeButton.innerHTML = '<svg viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg"><path d="M570.514286 512l292.571428-292.571429c14.628571-14.628571 14.628571-43.885714 0-58.514285-14.628571-14.628571-43.885714-14.628571-58.514285 0l-292.571429 292.571428-292.571429-292.571428c-14.628571-14.628571-43.885714-14.628571-58.514285 0-21.942857 14.628571-21.942857 43.885714 0 58.514285l292.571428 292.571429-292.571428 292.571429c-14.628571 14.628571-14.628571 43.885714 0 58.514285 14.628571 14.628571 43.885714 14.628571 58.514285 0l292.571429-292.571428 292.571429 292.571428c14.628571 14.628571 43.885714 14.628571 58.514285 0 14.628571-14.628571 14.628571-43.885714 0-58.514285l-292.571428-292.571429z"></path></svg>';

        messageElement.innerHTML += `
            <div class='content'>
                ${message}
            </div>
        `;
        messageElement.querySelector('.close-button').addEventListener('click', (e) => {
            e.preventDefault();
            this.closeMessage(messageElement);
        });
    }

    closeMessage(messageElement) {
        messageElement.classList.remove('show');
        messageElement.classList.add('close');
        let closeTimer = setTimeout(() => {
            messageElement.remove();
            clearTimeout(closeTimer);
        }, 300);
    }

    info(message, options) { this.notify(message, options, 1) }
    success(message, options) { this.notify(message, options, 2) }
    warning(message, options) { this.notify(message, options, 3) }
    error(message, options) { this.notify(message, options, 4) }

    confirm(message, options) {
        return new Promise((resolve) => {
            let overlayElement = document.createElement('div');
            overlayElement.className = "confirm-overlay"
            let confirmContent = document.createElement('div');
            confirmContent.className = 'confirm-content show';
            overlayElement.appendChild(confirmContent);
            document.body.appendChild(overlayElement);
            let contentElement = document.createElement('div');
            confirmContent.appendChild(contentElement);
            contentElement.className = 'content';
            contentElement.innerHTML = message;
            let operationContainer = document.createElement('div');
            operationContainer.className = 'operation-container';
            confirmContent.appendChild(operationContainer);
            if (options?.hideCancle != true) {
                let cancleButton = document.createElement('button');
                cancleButton.innerText = '取消';
                cancleButton.className = 'cancle';
                cancleButton.title = '取消';
                cancleButton.type = 'button';
                cancleButton.onclick = () => {
                    let timer = setTimeout(() => {
                        clearTimeout(timer);
                        confirmContent.classList.remove('show');
                        confirmContent.classList.add('hide');
                        if (options?.beforeClose) options?.beforeClose(false, overlayElement);
                        resolve(false);
                        overlayElement.remove();
                    }, 300);
                }
                operationContainer.appendChild(cancleButton);
            }
            let okButton = document.createElement('button');
            okButton.innerText = '确定';
            okButton.title = '确定';
            okButton.type = 'button';
            okButton.className = 'ok';
            okButton.onclick = () => {
                let timer = setTimeout(() => {
                    clearTimeout(timer);
                    confirmContent.classList.remove('show');
                    confirmContent.classList.add('hide');
                    if (options?.beforeClose) options?.beforeClose(true, overlayElement);
                    resolve(true);
                    overlayElement.remove();
                }, 300);
            }
            operationContainer.appendChild(okButton);
            confirmContent.querySelector('input')?.focus();
        });
    }
}