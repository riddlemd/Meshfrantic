window.scrollToBottom = (selector) => {
    const element = document.querySelector('.' + selector) || document.getElementById(selector);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};
