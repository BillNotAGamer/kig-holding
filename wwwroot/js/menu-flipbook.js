(function () {
    "use strict";

    function onReady(callback) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", callback, { once: true });
            return;
        }

        callback();
    }

    function debounce(callback, delay) {
        var timerId = 0;

        return function () {
            var args = arguments;

            window.clearTimeout(timerId);
            timerId = window.setTimeout(function () {
                callback.apply(null, args);
            }, delay);
        };
    }

    function normalizePage(page, index) {
        if (!page || typeof page.imageUrl !== "string") {
            return null;
        }

        var imageUrl = page.imageUrl.trim();
        if (!imageUrl) {
            return null;
        }

        var pageNumber = Number(page.pageNumber);

        return {
            imageUrl: imageUrl,
            altText: typeof page.altText === "string" ? page.altText.trim() : "",
            pageNumber: Number.isFinite(pageNumber) ? pageNumber : index + 1
        };
    }

    function parsePages(jsonNode) {
        if (!jsonNode) {
            return [];
        }

        try {
            var parsed = JSON.parse(jsonNode.textContent || "[]");
            if (!Array.isArray(parsed)) {
                return [];
            }

            return parsed
                .map(function (page, index) {
                    return normalizePage(page, index);
                })
                .filter(function (page) {
                    return page !== null;
                });
        } catch (_error) {
            return [];
        }
    }

    function setButtonState(button, isDisabled) {
        if (!button) {
            return;
        }

        button.disabled = isDisabled;
        button.setAttribute("aria-disabled", isDisabled ? "true" : "false");
    }

    function updateIndicators(currentNode, totalNode, currentPage, totalPages) {
        if (currentNode) {
            currentNode.textContent = String(currentPage);
        }

        if (totalNode) {
            totalNode.textContent = String(totalPages);
        }
    }

    function createElement(tagName, className, textContent) {
        var node = document.createElement(tagName);

        if (className) {
            node.className = className;
        }

        if (typeof textContent === "string") {
            node.textContent = textContent;
        }

        return node;
    }

    function renderEmptyState(stage, message) {
        stage.innerHTML = "";
        stage.classList.add("menu-pageflip-book--empty");
        stage.classList.remove("menu-pageflip-book--fallback");

        var panel = createElement("div", "menu-pageflip-empty");
        panel.appendChild(createElement("span", "menu-pageflip-empty__kicker", "Đang cập nhật"));
        panel.appendChild(createElement("h3", "menu-pageflip-empty__title", "Chưa có trang thực đơn để hiển thị"));
        panel.appendChild(createElement("p", "menu-pageflip-empty__copy", message));

        stage.appendChild(panel);
    }

    function renderFallbackGallery(stage, pages) {
        stage.innerHTML = "";
        stage.classList.add("menu-pageflip-book--fallback");
        stage.classList.remove("menu-pageflip-book--empty");

        var list = createElement("div", "menu-pageflip-fallback");

        pages.forEach(function (page, index) {
            var figure = createElement("figure", "menu-pageflip-fallback__item");
            var image = document.createElement("img");
            var captionText = "Trang " + String(page.pageNumber).padStart(2, "0");
            var altText = page.altText || ("Trang thực đơn " + (index + 1));

            image.className = "menu-pageflip-fallback__image";
            image.src = page.imageUrl;
            image.alt = altText;
            image.loading = index === 0 ? "eager" : "lazy";
            image.decoding = "async";

            figure.appendChild(image);

            if (page.altText) {
                captionText += ": " + page.altText;
            }

            figure.appendChild(createElement("figcaption", "menu-pageflip-fallback__caption", captionText));
            list.appendChild(figure);
        });

        stage.appendChild(list);
    }

    onReady(function () {
        var stage = document.getElementById("menu_group_flipbook");
        if (!stage) {
            return;
        }

        var dataNode = document.getElementById("menuFlipbookPages");
        var previousButton = document.getElementById("menu_flipbook_prev");
        var nextButton = document.getElementById("menu_flipbook_next");
        var currentIndicator = document.getElementById("menu_flipbook_current");
        var totalIndicator = document.getElementById("menu_flipbook_total");
        var pages = parsePages(dataNode);
        var totalPages = pages.length;
        var pageFlip = null;

        function syncUi(currentIndex) {
            var index = typeof currentIndex === "number"
                ? currentIndex
                : pageFlip && typeof pageFlip.getCurrentPageIndex === "function"
                    ? pageFlip.getCurrentPageIndex()
                    : 0;

            var displayPage = totalPages > 0
                ? Math.min(totalPages, Math.max(1, index + 1))
                : 0;

            updateIndicators(currentIndicator, totalIndicator, displayPage, totalPages);
            setButtonState(previousButton, index <= 0);
            setButtonState(nextButton, totalPages === 0 || index >= totalPages - 1);
        }

        updateIndicators(currentIndicator, totalIndicator, totalPages > 0 ? 1 : 0, totalPages);

        if (totalPages === 0) {
            renderEmptyState(stage, "Menu hiện chưa có hình ảnh hiển thị. Vui lòng quay lại sau hoặc liên hệ nhà hàng để được hỗ trợ.");
            setButtonState(previousButton, true);
            setButtonState(nextButton, true);
            return;
        }

        if (!window.St || typeof window.St.PageFlip !== "function") {
            renderFallbackGallery(stage, pages);
            setButtonState(previousButton, true);
            setButtonState(nextButton, true);
            return;
        }

        try {
            pageFlip = new window.St.PageFlip(stage, {
                width: 720,
                height: 1018,
                size: "stretch",
                minWidth: 260,
                maxWidth: 540,
                minHeight: 368,
                maxHeight: 764,
                drawShadow: true,
                flippingTime: 850,
                usePortrait: true,
                startPage: 0,
                autoSize: true,
                maxShadowOpacity: 0.35,
                showCover: false,
                mobileScrollSupport: true,
                swipeDistance: 24,
                clickEventForward: true,
                useMouseEvents: true,
                disableFlipByClick: false
            });

            pageFlip.on("init", function (event) {
                var pageIndex = event && event.data && typeof event.data.page === "number"
                    ? event.data.page
                    : 0;

                syncUi(pageIndex);
            });

            pageFlip.on("flip", function (event) {
                syncUi(event && typeof event.data === "number" ? event.data : undefined);
            });

            pageFlip.on("update", function (event) {
                var pageIndex = event && event.data && typeof event.data.page === "number"
                    ? event.data.page
                    : undefined;

                syncUi(pageIndex);
            });

            pageFlip.on("changeOrientation", function () {
                syncUi();
            });

            pageFlip.loadFromImages(
                pages.map(function (page) {
                    return page.imageUrl;
                })
            );

            window.setTimeout(syncUi, 60);
        } catch (error) {
            if (pageFlip && typeof pageFlip.destroy === "function") {
                try {
                    pageFlip.destroy();
                } catch (_destroyError) {
                    // Ignore cleanup errors and continue to fallback rendering.
                }
            }

            renderFallbackGallery(stage, pages);
            setButtonState(previousButton, true);
            setButtonState(nextButton, true);

            if (window.console && typeof window.console.error === "function") {
                window.console.error("StPageFlip initialization failed for the menu viewer.", error);
            }

            return;
        }

        if (previousButton) {
            previousButton.addEventListener("click", function () {
                if (!pageFlip) {
                    return;
                }

                try {
                    pageFlip.flipPrev();
                } catch (_error) {
                    // Ignore navigation errors and keep the current page state.
                }
            });
        }

        if (nextButton) {
            nextButton.addEventListener("click", function () {
                if (!pageFlip) {
                    return;
                }

                try {
                    pageFlip.flipNext();
                } catch (_error) {
                    // Ignore navigation errors and keep the current page state.
                }
            });
        }

        window.addEventListener("resize", debounce(function () {
            if (!pageFlip || typeof pageFlip.update !== "function") {
                return;
            }

            try {
                pageFlip.update();
                syncUi();
            } catch (_error) {
                // Ignore resize update failures to avoid noisy console output.
            }
        }, 140), { passive: true });

        syncUi();
    });
})();
