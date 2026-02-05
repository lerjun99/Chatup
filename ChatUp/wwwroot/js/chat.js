window.enablePasteImage = (element, dotnetHelper) => {
    element.addEventListener("paste", function (event) {
        if (!event.clipboardData) return;

        const items = event.clipboardData.items;
        for (let i = 0; i < items.length; i++) {
            if (items[i].type.indexOf("image") !== -1) {
                const file = items[i].getAsFile();
                const reader = new FileReader();
                reader.onload = function (e) {
                    dotnetHelper.invokeMethodAsync("ReceivePastedImage", e.target.result);
                };
                reader.readAsDataURL(file);
                event.preventDefault();
                break;
            }
        }
    });
};

window.blazorScrollToBottom = (element) => {
    if (element) {
        // Scroll after DOM updates
        requestAnimationFrame(() => {
            element.scrollTop = element.scrollHeight;
        });
    }
};
window.registerScrollUpEvent = (element, dotNetRef) => {
    element.addEventListener('scroll', () => {
        if (element.scrollTop === 0) {
            dotNetRef.invokeMethodAsync('LoadMoreMessages');
        }
    });
};

window.maintainScrollPosition = (element) => {
    // small offset to avoid retrigger
    element.scrollTop = 50;
}; 

window.notifyLogout = (userId) => {
        if (window.userStatusHubConnection) {
        window.userStatusHubConnection.invoke("UserStatusChanged", { UserId: userId, IsOnline: false });
        }
};
window.downloadFile = (filename, contentType, content) => {
    const blob = new Blob([content], { type: contentType });
    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();
};
document.addEventListener("DOMContentLoaded", function () {
    const rows = document.querySelectorAll(".swipe-row");
    rows.forEach(row => {
        let startX = 0;
        let currentX = 0;
        let isSwiping = false;

        row.addEventListener("touchstart", (e) => {
            startX = e.touches[0].clientX;
            isSwiping = true;
        });

        row.addEventListener("touchmove", (e) => {
            if (!isSwiping) return;
            currentX = e.touches[0].clientX;
            const diffX = startX - currentX;
            if (diffX > 30) row.classList.add("swiped");
            if (diffX < -30) row.classList.remove("swiped");
        });

        row.addEventListener("touchend", () => {
            isSwiping = false;
        });
    });
});
//window.readDroppedFiles = (dropArea, dotnetHelper) => {
//    dropArea.addEventListener('drop', async (e) => {
//        e.preventDefault();
//        const files = e.dataTransfer.files;
//        if (files.length > 0) {
//            const file = files[0];
//            const reader = new FileReader();
//            reader.onload = () => {
//                dotnetHelper.invokeMethodAsync('ReceiveDroppedFile', reader.result, file.name, file.type);
//            };
//            reader.readAsDataURL(file); // get Base64
//        }
//    });

//    dropArea.addEventListener('dragover', (e) => e.preventDefault());
//};
window.getDroppedFiles = async function (e) {
    try {
        const result = [];

        const files = e?.dataTransfer?.files;
        if (!files || files.length === 0)
            return result;

        for (let i = 0; i < files.length; i++) {
            const file = files[i];

            if (file.size > 10 * 1024 * 1024)
                continue;

            const base64 = await new Promise((resolve) => {
                const reader = new FileReader();
                reader.onloadend = () => {
                    const dataUrl = reader.result;
                    resolve(dataUrl.split(",")[1]);
                };
                reader.readAsDataURL(file);
            });

            result.push({
                name: file.name,
                type: file.type,
                base64: base64
            });
        }

        return result;
    } catch (err) {
        console.error("getDroppedFiles failed:", err);
        return [];
    }
};
window.getClipboardImage = async function () {
    try {
        const items = await navigator.clipboard.read();
        const result = [];

        for (const item of items) {
            for (const type of item.types) {
                if (type.startsWith("image/")) {
                    const blob = await item.getType(type);

                    const base64 = await new Promise((resolve) => {
                        const reader = new FileReader();
                        reader.onloadend = () =>
                            resolve(reader.result.split(",")[1]);
                        reader.readAsDataURL(blob);
                    });

                    result.push({
                        name: `clipboard_${Date.now()}.png`,
                        type: blob.type,
                        base64: base64
                    });
                }
            }
        }

        return result; // ALWAYS array
    } catch (err) {
        console.error("Clipboard error:", err);
        return [];
    }
};
window.triggerFileInput = (elementId) => {
    const input = document.getElementById(elementId);
    if (input) input.click();
};
async function downloadFileFromStream(fileName, dotNetStream) {
    const arrayBuffer = await dotNetStream.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);

    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName ?? "download";
    anchor.click();

    URL.revokeObjectURL(url);
}
window.registerDropZone = (element, dotnetRef) => {

    element.addEventListener("dragover", e => {
        e.preventDefault();
    });

    element.addEventListener("drop", async e => {
        e.preventDefault();

        const files = e.dataTransfer.files;
        if (!files || files.length === 0)
            return;

        for (let i = 0; i < files.length; i++) {
            const file = files[i];

            if (file.size > 10 * 1024 * 1024)
                continue;

            const base64 = await new Promise(resolve => {
                const reader = new FileReader();
                reader.onloadend = () =>
                    resolve(reader.result.split(",")[1]);
                reader.readAsDataURL(file);
            });

            dotnetRef.invokeMethodAsync(
                "ReceiveDroppedFile",
                base64,
                file.name,
                file.type
            );
        }
    });
};
window.initLazyImages = (container) => {
    const images = container.querySelectorAll('.lazy-image');

    if (!images || images.length === 0) {
        // No images, scroll immediately
        window.blazorScrollToBottom(container);
        return;
    }

    const observer = new IntersectionObserver((entries, obs) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                const src = img.getAttribute('data-src');
                if (src) {
                    img.src = src;
                    img.removeAttribute('data-src');

                    // Scroll to bottom when image loads
                    img.onload = img.onerror = () => {
                        window.blazorScrollToBottom(container);
                    };
                }
                obs.unobserve(img);
            }
        });
    }, {
        rootMargin: '100px'
    });

    images.forEach(img => observer.observe(img));

    // Initial scroll in case some images are already loaded
    window.blazorScrollToBottom(container);
};
window.setBlazoredTextEditorContent = (id, content) => {
    const editor = document.getElementById(id);
    if (editor) editor.innerHTML = content;
};

window.getBlazoredTextEditorContent = (id) => {
    const editor = document.getElementById(id);
    return editor ? editor.innerHTML : "";
};
window.createThumbnail = async function (base64, maxWidth) {
    return new Promise((resolve, reject) => {
        var img = new Image();
        img.onload = function () {
            var ratio = maxWidth / img.width;
            var canvas = document.createElement('canvas');
            canvas.width = maxWidth;
            canvas.height = img.height * ratio;
            var ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
            resolve(canvas.toDataURL());
        };
        img.onerror = reject;
        img.src = base64;
    });
};

window.autoResizeTextArea = (element) => {
    if (!element) return;
    element.style.height = 'auto';       // reset
    element.style.height = element.scrollHeight + 'px';
};
// wwwroot/js/site.js

window.startOnboarding = function (dotNetRef) {
    if (typeof introJs === "undefined") {
        console.error("Intro.js is not loaded");
        return;
    }

    const tour = introJs.tour();

    tour.setOptions({
        nextLabel: 'Next →',
        prevLabel: '← Back',
        doneLabel: 'Got it!',
        skipLabel: 'Skip',
        showProgress: true,
        showBullets: false,
        exitOnOverlayClick: false
    });

    tour.oncomplete(function () {
        dotNetRef.invokeMethodAsync("OnOnboardingFinished", true); // completed
    });

    tour.onexit(function () {
        dotNetRef.invokeMethodAsync("OnOnboardingFinished", false); // skipped
    });

    tour.start();
};
