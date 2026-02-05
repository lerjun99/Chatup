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
        element.scrollTop = element.scrollHeight;
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