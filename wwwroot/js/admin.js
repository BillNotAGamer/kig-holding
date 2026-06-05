(function () {
    function formatBytes(bytes) {
        if (!Number.isFinite(bytes) || bytes <= 0) {
            return "0 B";
        }

        var units = ["B", "KB", "MB", "GB"];
        var size = bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.length - 1) {
            size /= 1024;
            unitIndex += 1;
        }

        var precision = unitIndex === 0 ? 0 : unitIndex === 1 ? 1 : 2;
        return size.toFixed(precision) + " " + units[unitIndex];
    }

    function initAdminDrawer() {
        var drawer = document.querySelector("[data-admin-drawer]");
        var overlay = document.querySelector("[data-admin-menu-overlay]");
        var openButtons = Array.from(document.querySelectorAll("[data-admin-menu-open]"));
        var closeButtons = Array.from(document.querySelectorAll("[data-admin-menu-close]"));

        if (!drawer || !openButtons.length) {
            return;
        }

        var desktopQuery = window.matchMedia("(min-width: 1024px)");
        var isOpen = false;

        var setOpen = function (nextOpen, focusDrawer) {
            isOpen = Boolean(nextOpen);
            drawer.classList.toggle("-translate-x-full", !isOpen);
            drawer.classList.toggle("translate-x-0", isOpen);

            if (overlay) {
                overlay.classList.toggle("hidden", !isOpen);
            }

            openButtons.forEach(function (button) {
                button.setAttribute("aria-expanded", isOpen ? "true" : "false");
            });

            document.body.classList.toggle("admin-drawer-open", isOpen);

            if (isOpen && focusDrawer) {
                var focusTarget = drawer.querySelector("[data-admin-menu-close], a, button");
                window.setTimeout(function () {
                    focusTarget?.focus();
                }, 40);
            }
        };

        openButtons.forEach(function (button) {
            button.addEventListener("click", function () {
                setOpen(true, true);
            });
        });

        closeButtons.forEach(function (button) {
            button.addEventListener("click", function () {
                setOpen(false);
            });
        });

        overlay?.addEventListener("click", function () {
            setOpen(false);
        });

        drawer.querySelectorAll("a").forEach(function (link) {
            link.addEventListener("click", function () {
                if (!desktopQuery.matches) {
                    setOpen(false);
                }
            });
        });

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && isOpen) {
                setOpen(false);
                openButtons[0]?.focus();
            }
        });

        var closeOnDesktop = function () {
            if (desktopQuery.matches) {
                setOpen(false);
            }
        };

        if (typeof desktopQuery.addEventListener === "function") {
            desktopQuery.addEventListener("change", closeOnDesktop);
        } else if (typeof desktopQuery.addListener === "function") {
            desktopQuery.addListener(closeOnDesktop);
        }

        setOpen(false);
    }

    function initImagePreviews() {
        var inputs = document.querySelectorAll("[data-admin-image-preview]");
        if (!inputs.length) {
            return;
        }

        inputs.forEach(function (input) {
            var targetSelector = input.getAttribute("data-admin-image-preview-target");
            var metaSelector = input.getAttribute("data-admin-image-preview-meta");
            var target = targetSelector ? document.querySelector(targetSelector) : null;
            var meta = metaSelector ? document.querySelector(metaSelector) : null;
            var panel = target ? target.closest(".admin-upload__preview") : null;

            var clearPreview = function () {
                if (target) {
                    target.removeAttribute("src");
                    target.hidden = true;
                }

                if (meta) {
                    meta.textContent = "";
                }

                if (panel) {
                    panel.hidden = true;
                }
            };

            var updatePreview = function () {
                var files = input.files ? Array.from(input.files) : [];
                var file = files[0];

                if (!file) {
                    clearPreview();
                    return;
                }

                if (meta) {
                    meta.textContent = file.name + " • " + formatBytes(file.size);
                }

                if (!target) {
                    return;
                }

                var reader = new FileReader();
                reader.onload = function (event) {
                    var result = event && event.target ? event.target.result : null;
                    if (!result) {
                        clearPreview();
                        return;
                    }

                    target.src = String(result);
                    target.alt = "Xem trước: " + file.name;
                    target.hidden = false;

                    if (panel) {
                        panel.hidden = false;
                    }
                };

                reader.onerror = clearPreview;
                reader.readAsDataURL(file);
            };

            input.addEventListener("change", updatePreview);
            clearPreview();
        });
    }

    function initFileLists() {
        var inputs = document.querySelectorAll("[data-admin-file-count], [data-admin-file-list]");
        if (!inputs.length) {
            return;
        }

        inputs.forEach(function (input) {
            var countSelector = input.getAttribute("data-admin-file-count");
            var listSelector = input.getAttribute("data-admin-file-list");
            var countElement = countSelector ? document.querySelector(countSelector) : null;
            var listElement = listSelector ? document.querySelector(listSelector) : null;

            var updateList = function () {
                var files = input.files ? Array.from(input.files) : [];

                if (countElement) {
                    countElement.textContent = files.length
                        ? "Đã chọn " + files.length + " file"
                        : "Chưa chọn file nào.";
                }

                if (!listElement) {
                    return;
                }

                listElement.innerHTML = "";

                if (!files.length) {
                    listElement.hidden = true;
                    return;
                }

                files.forEach(function (file) {
                    var item = document.createElement("li");
                    item.className = "rounded-xl border border-brand-border bg-brand-white px-3 py-2 text-sm text-brand-gray";
                    item.textContent = file.name + " • " + formatBytes(file.size);
                    listElement.appendChild(item);
                });

                listElement.hidden = false;
            };

            input.addEventListener("change", updateList);
            updateList();
        });
    }

    initAdminDrawer();
    initImagePreviews();
    initFileLists();
})();
