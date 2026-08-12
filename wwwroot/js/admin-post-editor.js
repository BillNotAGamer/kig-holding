(function () {
    const visualMode = "0";
    const htmlMode = "1";
    const boundAttribute = "data-admin-post-editor-bound";
    const htmlTextareaClass = "admin-post-content-editor__textarea--html";
    const visualConfirmMessage = "Nội dung hiện tại đang ở chế độ HTML. Nếu chuyển sang Soạn thảo, mã HTML sẽ được xem như văn bản thông thường khi lưu. Bạn có muốn tiếp tục?";

    const editors = document.querySelectorAll("[data-admin-post-editor]");

    editors.forEach((editor) => {
        if (editor.hasAttribute(boundAttribute)) {
            return;
        }

        editor.setAttribute(boundAttribute, "true");

        const modeInput = editor.querySelector("[data-post-editor-mode-input]");
        const textarea = editor.querySelector("[data-post-editor-content]");
        const tabs = Array.from(editor.querySelectorAll("[data-post-editor-tab]"));
        const panels = Array.from(editor.querySelectorAll("[role='tabpanel']"));

        if (!modeInput || !textarea || tabs.length === 0 || panels.length === 0) {
            return;
        }

        const normalizeMode = (mode) => mode === htmlMode ? htmlMode : visualMode;

        const activateMode = (mode, options) => {
            const nextMode = normalizeMode(mode);
            const currentMode = normalizeMode(modeInput.value);
            const shouldPrompt = options?.prompt !== false
                && currentMode === htmlMode
                && nextMode === visualMode
                && textarea.value.trim().length > 0;

            if (shouldPrompt && !window.confirm(visualConfirmMessage)) {
                syncUi(currentMode, true);
                return;
            }

            modeInput.value = nextMode;
            syncUi(nextMode, options?.focus === true);
        };

        const syncUi = (mode, focusSelectedTab) => {
            const activeMode = normalizeMode(mode);
            const isHtmlMode = activeMode === htmlMode;

            editor.dataset.contentMode = activeMode;
            textarea.classList.toggle(htmlTextareaClass, isHtmlMode);
            textarea.spellcheck = !isHtmlMode;

            tabs.forEach((tab) => {
                const selected = normalizeMode(tab.getAttribute("data-post-editor-tab")) === activeMode;
                tab.setAttribute("aria-selected", selected ? "true" : "false");
                tab.tabIndex = selected ? 0 : -1;

                if (selected && focusSelectedTab) {
                    tab.focus({ preventScroll: true });
                }
            });

            panels.forEach((panel) => {
                const tab = tabs.find((candidate) => candidate.getAttribute("aria-controls") === panel.id);
                const selected = tab && normalizeMode(tab.getAttribute("data-post-editor-tab")) === activeMode;
                panel.hidden = !selected;
            });
        };

        tabs.forEach((tab, index) => {
            tab.addEventListener("click", () => {
                activateMode(tab.getAttribute("data-post-editor-tab"), { focus: false });
            });

            tab.addEventListener("keydown", (event) => {
                const keyHandlers = {
                    ArrowRight: () => tabs[(index + 1) % tabs.length],
                    ArrowLeft: () => tabs[(index - 1 + tabs.length) % tabs.length],
                    Home: () => tabs[0],
                    End: () => tabs[tabs.length - 1]
                };

                const getNextTab = keyHandlers[event.key];
                if (!getNextTab) {
                    return;
                }

                event.preventDefault();
                activateMode(getNextTab().getAttribute("data-post-editor-tab"), { focus: true });
            });
        });

        activateMode(editor.getAttribute("data-admin-post-editor-initial-mode") || modeInput.value, {
            prompt: false,
            focus: false
        });
    });
})();
