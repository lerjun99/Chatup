window.getScrollData = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return null;

    return {
        scrollTop: el.scrollTop,
        scrollHeight: el.scrollHeight,
        clientHeight: el.clientHeight
    };
};

// OTP page: move focus to the next digit box (replaces a JS "eval" call, which the CSP blocks)
window.focusOtpBox = function (index) {
    const boxes = document.getElementsByClassName('otp-box');
    if (boxes[index]) boxes[index].focus();
};
