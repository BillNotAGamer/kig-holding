(function () {
    "use strict";

    var DEFAULT_PAGE_WIDTH = 1270;
    var DEFAULT_PAGE_HEIGHT = 2048;
    var DEFAULT_PAGE_RATIO = DEFAULT_PAGE_WIDTH / DEFAULT_PAGE_HEIGHT;
    var PAGE_PRELOAD_RADIUS = 2;
    var FALLBACK_PRELOAD_RADIUS = 1;
    var preloadedImageSources = Object.create(null);

    var VIEWER_HINTS = {
        pageFlip: "K\u00e9o, vu\u1ed1t ho\u1eb7c d\u00f9ng n\u00fat \u0111i\u1ec1u h\u01b0\u1edbng \u0111\u1ec3 l\u1eadt trang. D\u00f9ng n\u00fat Ph\u00f3ng to \u0111\u1ec3 xem \u1ea3nh g\u1ed1c.",
        fallback: "D\u00f9ng n\u00fat \u0111i\u1ec1u h\u01b0\u1edbng \u0111\u1ec3 xem t\u1eebng trang."
    };

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

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
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

    function warmImageSource(imageUrl) {
        if (!imageUrl || preloadedImageSources[imageUrl]) {
            return;
        }

        var probe = new Image();

        preloadedImageSources[imageUrl] = "pending";
        probe.decoding = "async";
        probe.onload = function () {
            preloadedImageSources[imageUrl] = "loaded";
        };
        probe.onerror = function () {
            preloadedImageSources[imageUrl] = "error";
        };
        probe.src = imageUrl;
    }

    function preloadAdjacentPages(pages, centerIndex, radius) {
        var start = clamp(centerIndex - radius, 0, pages.length - 1);
        var end = clamp(centerIndex + radius, 0, pages.length - 1);
        var pointer;

        for (pointer = start; pointer <= end; pointer += 1) {
            if (pointer !== centerIndex && pages[pointer] && pages[pointer].imageUrl) {
                warmImageSource(pages[pointer].imageUrl);
            }
        }
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
        var pageIndex = Number(page.pageIndex);

        return {
            imageUrl: imageUrl,
            altText: typeof page.altText === "string" ? page.altText.trim() : "",
            pageNumber: Number.isFinite(pageNumber) ? pageNumber : index + 1,
            pageIndex: Number.isFinite(pageIndex) ? pageIndex : index
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

    function getPageAltText(page, index) {
        return page.altText || ("Trang th\u1ef1c \u0111\u01a1n " + (index + 1));
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

    function clearNode(node) {
        if (!node) {
            return;
        }

        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
    }

    function getManagedImageSource(img) {
        return img ? img.getAttribute("data-src") || "" : "";
    }

    function updateImageState(media, state) {
        if (!media) {
            return;
        }

        media.classList.toggle("is-loading", state === "loading");
        media.classList.toggle("is-ready", state === "ready");
        media.classList.toggle("is-error", state === "error");
    }

    function createManagedImage(page, index, options) {
        var media = createElement("div", "menu-viewer__page-media");
        var image = document.createElement("img");
        var placeholder = createElement("div", "menu-viewer__image-placeholder");
        var placeholderTitle = createElement(
            "p",
            "menu-viewer__image-placeholder-title",
            "Kh\u00f4ng th\u1ec3 t\u1ea3i trang th\u1ef1c \u0111\u01a1n n\u00e0y");
        var placeholderCopy = createElement(
            "p",
            "menu-viewer__image-placeholder-copy",
            "B\u1ea1n v\u1eabn c\u00f3 th\u1ec3 chuy\u1ec3n sang trang kh\u00e1c ho\u1eb7c th\u1eed t\u1ea3i l\u1ea1i sau.");
        var shouldLoadNow = !!(options && options.immediate);
        var shouldPrioritize = !!(options && options.prioritize);

        image.className = options && options.imageClassName
            ? options.imageClassName
            : "menu-viewer__image";
        image.setAttribute("data-src", page.imageUrl);
        image.alt = getPageAltText(page, index);
        image.width = DEFAULT_PAGE_WIDTH;
        image.height = DEFAULT_PAGE_HEIGHT;
        image.decoding = "async";
        image.loading = shouldLoadNow ? "eager" : "lazy";
        image.draggable = false;
        image.setAttribute("draggable", "false");

        if (shouldPrioritize) {
            image.setAttribute("fetchpriority", "high");
        }

        image.addEventListener("dragstart", function (event) {
            event.preventDefault();
        });

        image.addEventListener("load", function () {
            updateImageState(media, "ready");

            if (typeof image.decode === "function") {
                image.decode().catch(function () {
                    return null;
                });
            }
        });

        image.addEventListener("error", function () {
            updateImageState(media, "error");
        });

        placeholder.appendChild(placeholderTitle);
        placeholder.appendChild(placeholderCopy);
        media.appendChild(image);
        media.appendChild(placeholder);

        updateImageState(media, shouldLoadNow ? "loading" : "");

        return {
            root: media,
            image: image,
            page: page,
            index: index
        };
    }

    function activateManagedImage(record) {
        if (!record || !record.image) {
            return Promise.resolve();
        }

        var image = record.image;
        var media = record.root;

        if (image.dataset.error === "true") {
            updateImageState(media, "error");
            return Promise.resolve();
        }

        if (!image.getAttribute("src")) {
            var source = getManagedImageSource(image);
            if (!source) {
                updateImageState(media, "error");
                return Promise.resolve();
            }

            updateImageState(media, "loading");
            image.setAttribute("src", source);
        }

        if (image.complete) {
            if (image.naturalWidth > 0) {
                updateImageState(media, "ready");

                if (typeof image.decode === "function") {
                    return image.decode().catch(function () {
                        return null;
                    });
                }

                return Promise.resolve();
            }

            image.dataset.error = "true";
            updateImageState(media, "error");
            return Promise.resolve();
        }

        return new Promise(function (resolve) {
            var handleLoad = function () {
                cleanup();
                updateImageState(media, "ready");

                if (typeof image.decode === "function") {
                    image.decode().catch(function () {
                        return null;
                    }).finally(resolve);
                    return;
                }

                resolve();
            };

            var handleError = function () {
                cleanup();
                image.dataset.error = "true";
                updateImageState(media, "error");
                resolve();
            };

            var cleanup = function () {
                image.removeEventListener("load", handleLoad);
                image.removeEventListener("error", handleError);
            };

            image.addEventListener("load", handleLoad, { once: true });
            image.addEventListener("error", handleError, { once: true });
        });
    }

    function hydrateAround(records, centerIndex, radius) {
        var promises = [];
        var start = clamp(centerIndex - radius, 0, records.length - 1);
        var end = clamp(centerIndex + radius, 0, records.length - 1);
        var pointer;

        for (pointer = start; pointer <= end; pointer += 1) {
            promises.push(activateManagedImage(records[pointer]));
        }

        return Promise.all(promises);
    }

    function createStaticFallbackViewer(options) {
        var host = options.host;
        var pages = options.pages;
        var currentIndex = clamp(options.startIndex || 0, 0, pages.length - 1);
        var root = createElement("div", "menu-viewer-fallback");
        var surface = createElement("div", "menu-viewer-fallback__page");

        root.appendChild(surface);
        clearNode(host);
        host.appendChild(root);

        function emitIndexChange() {
            if (typeof options.onIndexChange === "function") {
                options.onIndexChange(currentIndex);
            }
        }

        function render(index) {
            currentIndex = clamp(index, 0, pages.length - 1);
            clearNode(surface);

            var page = pages[currentIndex];
            var managed = createManagedImage(page, currentIndex, {
                immediate: true,
                prioritize: true,
                imageClassName: "menu-viewer__image menu-viewer-fallback__image"
            });

            surface.appendChild(managed.root);
            activateManagedImage(managed);
            preloadAdjacentPages(pages, currentIndex, FALLBACK_PRELOAD_RADIUS);
            emitIndexChange();
        }

        render(currentIndex);

        return {
            mount: function () {
                return Promise.resolve();
            },
            destroy: function () {
                clearNode(host);
            },
            update: function () {
                return null;
            },
            prev: function () {
                if (currentIndex > 0) {
                    render(currentIndex - 1);
                }
            },
            next: function () {
                if (currentIndex < pages.length - 1) {
                    render(currentIndex + 1);
                }
            },
            goTo: function (index) {
                render(index);
            },
            getIndex: function () {
                return currentIndex;
            },
            getHint: function () {
                return VIEWER_HINTS.fallback;
            },
            canOpenZoom: function () {
                return true;
            },
            openZoom: function () {
                if (typeof options.openZoom === "function") {
                    options.openZoom(pages[currentIndex], currentIndex);
                }
            }
        };
    }

    function resolveImageDimensions(imageUrl) {
        return new Promise(function (resolve) {
            var probe = new Image();
            var finalized = false;

            function finish(dimensions) {
                if (finalized) {
                    return;
                }

                finalized = true;
                resolve(dimensions);
            }

            probe.onload = function () {
                finish({
                    width: probe.naturalWidth || DEFAULT_PAGE_WIDTH,
                    height: probe.naturalHeight || DEFAULT_PAGE_HEIGHT
                });
            };

            probe.onerror = function () {
                finish(null);
            };

            probe.decoding = "async";
            probe.src = imageUrl;

            if (probe.complete && probe.naturalWidth > 0) {
                finish({
                    width: probe.naturalWidth,
                    height: probe.naturalHeight
                });
            }
        });
    }

    function resolveReferenceDimensions(pages) {
        var sequence = Promise.resolve(null);

        pages.forEach(function (page) {
            sequence = sequence.then(function (current) {
                if (current) {
                    return current;
                }

                return resolveImageDimensions(page.imageUrl);
            });
        });

        return sequence.then(function (dimensions) {
            return dimensions || {
                width: DEFAULT_PAGE_WIDTH,
                height: DEFAULT_PAGE_HEIGHT
            };
        });
    }

    function getHostContentWidth(host) {
        if (!host) {
            return Math.max(220, window.innerWidth || DEFAULT_PAGE_WIDTH);
        }

        var bounds = host.getBoundingClientRect();
        return Math.max(220, Math.floor(bounds.width || window.innerWidth || DEFAULT_PAGE_WIDTH));
    }

    function resolvePageFlipBounds(host) {
        var availableWidth = getHostContentWidth(host);
        var portraitWidth = clamp(availableWidth - 8, 220, 760);

        if (availableWidth < 900) {
            return {
                minWidth: portraitWidth,
                maxWidth: portraitWidth
            };
        }

        var maxWidth = clamp(Math.floor((availableWidth - 24) / 2), 280, 560);
        var minWidth = clamp(Math.floor((availableWidth - 72) / 2), 240, maxWidth);

        return {
            minWidth: minWidth,
            maxWidth: maxWidth
        };
    }

    function createPageFlipViewer(options) {
        if (!window.St || typeof window.St.PageFlip !== "function") {
            if (window.console && typeof window.console.warn === "function") {
                window.console.warn("StPageFlip is unavailable for the menu viewer. Falling back to the native pager.");
            }

            return Promise.resolve(createStaticFallbackViewer(options));
        }

        var host = options.host;
        var pages = options.pages;
        var currentIndex = clamp(options.startIndex || 0, 0, pages.length - 1);
        var pageFlip = null;
        var records = [];
        var pageElements = [];
        var stage = createElement("div", "menu-pageflip-book");

        clearNode(host);
        host.appendChild(stage);

        function emitIndexChange(nextIndex) {
            currentIndex = clamp(nextIndex, 0, pages.length - 1);
            hydrateAround(records, currentIndex, PAGE_PRELOAD_RADIUS);
            preloadAdjacentPages(pages, currentIndex, PAGE_PRELOAD_RADIUS);

            if (typeof options.onIndexChange === "function") {
                options.onIndexChange(currentIndex);
            }
        }

        function getCurrentPageIndex() {
            if (pageFlip && typeof pageFlip.getCurrentPageIndex === "function") {
                currentIndex = clamp(pageFlip.getCurrentPageIndex(), 0, pages.length - 1);
            }

            return currentIndex;
        }

        return resolveReferenceDimensions(pages).then(function (dimensions) {
            var ratio = dimensions.width > 0 && dimensions.height > 0
                ? dimensions.width / dimensions.height
                : DEFAULT_PAGE_RATIO;
            var bounds = resolvePageFlipBounds(host);

            stage.style.setProperty("--menu-viewer-page-ratio", String(ratio));

            pages.forEach(function (page, index) {
                var pageElement = createElement("div", "menu-flipbook-page");
                var surface = createElement("div", "menu-flipbook-page__surface");
                var managed = createManagedImage(page, index, {
                    immediate: Math.abs(index - currentIndex) <= PAGE_PRELOAD_RADIUS,
                    prioritize: index === currentIndex,
                    imageClassName: "menu-viewer__image menu-flipbook-page__image"
                });

                surface.appendChild(managed.root);
                pageElement.appendChild(surface);
                pageElements.push(pageElement);
                records.push(managed);
            });

            try {
                pageFlip = new window.St.PageFlip(stage, {
                    width: dimensions.width,
                    height: dimensions.height,
                    size: "stretch",
                    minWidth: bounds.minWidth,
                    maxWidth: bounds.maxWidth,
                    minHeight: Math.round(bounds.minWidth / ratio),
                    maxHeight: Math.round(bounds.maxWidth / ratio),
                    drawShadow: true,
                    flippingTime: 850,
                    usePortrait: true,
                    startPage: currentIndex,
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
                    var initialIndex = event && event.data && typeof event.data.page === "number"
                        ? event.data.page
                        : getCurrentPageIndex();

                    emitIndexChange(initialIndex);
                });

                pageFlip.on("flip", function (event) {
                    var nextIndex = event && typeof event.data === "number"
                        ? event.data
                        : getCurrentPageIndex();

                    emitIndexChange(nextIndex);
                });

                pageFlip.on("update", function () {
                    emitIndexChange(getCurrentPageIndex());
                });

                pageFlip.on("changeOrientation", function () {
                    emitIndexChange(getCurrentPageIndex());
                });

                pageFlip.loadFromHTML(pageElements);
            } catch (error) {
                if (pageFlip && typeof pageFlip.destroy === "function") {
                    try {
                        pageFlip.destroy();
                    } catch (_destroyError) {
                        pageFlip = null;
                    }
                }

                clearNode(host);

                if (window.console && typeof window.console.warn === "function") {
                    window.console.warn("StPageFlip failed to initialize in HTML mode. Falling back to the native pager.", error);
                }

                return createStaticFallbackViewer(options);
            }

            emitIndexChange(currentIndex);

            return {
                mount: function () {
                    if (pageFlip && typeof pageFlip.update === "function") {
                        pageFlip.update();
                    }

                    emitIndexChange(getCurrentPageIndex());
                    return Promise.resolve();
                },
                destroy: function () {
                    if (pageFlip && typeof pageFlip.destroy === "function") {
                        pageFlip.destroy();
                    }

                    clearNode(host);
                },
                update: function () {
                    if (pageFlip && typeof pageFlip.update === "function") {
                        pageFlip.update();
                    }

                    emitIndexChange(getCurrentPageIndex());
                },
                prev: function () {
                    if (pageFlip && currentIndex > 0) {
                        pageFlip.flipPrev();
                    }
                },
                next: function () {
                    if (pageFlip && currentIndex < pages.length - 1) {
                        pageFlip.flipNext();
                    }
                },
                goTo: function (index) {
                    currentIndex = clamp(index, 0, pages.length - 1);

                    if (pageFlip && typeof pageFlip.turnToPage === "function") {
                        pageFlip.turnToPage(currentIndex);
                    }

                    emitIndexChange(currentIndex);
                },
                getIndex: function () {
                    return getCurrentPageIndex();
                },
                getHint: function () {
                    return VIEWER_HINTS.pageFlip;
                },
                canOpenZoom: function () {
                    return true;
                },
                openZoom: function () {
                    if (typeof options.openZoom === "function") {
                        var resolvedIndex = getCurrentPageIndex();
                        options.openZoom(pages[resolvedIndex], resolvedIndex);
                    }
                }
            };
        });
    }

    function createEmptyState(stage, message) {
        clearNode(stage);
        stage.classList.add("menu-viewer__surface--empty");

        var panel = createElement("div", "menu-pageflip-empty");
        panel.appendChild(createElement("span", "menu-pageflip-empty__kicker", "\u0110ang c\u1eadp nh\u1eadt"));
        panel.appendChild(createElement("h3", "menu-pageflip-empty__title", "Ch\u01b0a c\u00f3 trang th\u1ef1c \u0111\u01a1n \u0111\u1ec3 hi\u1ec3n th\u1ecb"));
        panel.appendChild(createElement("p", "menu-pageflip-empty__copy", message));
        stage.appendChild(panel);
    }

    onReady(function () {
        var frame = document.querySelector("[data-menu-viewer-frame]");
        if (!frame) {
            return;
        }

        var dataNode = document.getElementById("menuFlipbookPages");
        var mobileHost = document.getElementById("menu_mobile_viewer");
        var desktopHost = document.getElementById("menu_desktop_viewer");
        var viewerHost = document.getElementById("menu_flipbook_viewer") || desktopHost || mobileHost;
        var previousButton = document.getElementById("menu_flipbook_prev");
        var nextButton = document.getElementById("menu_flipbook_next");
        var zoomButton = document.getElementById("menu_viewer_zoom_toggle");
        var currentIndicator = document.getElementById("menu_flipbook_current");
        var totalIndicator = document.getElementById("menu_flipbook_total");
        var hintNode = document.getElementById("menu_viewer_hint");
        var overlay = document.getElementById("menu_viewer_zoom_overlay");
        var overlayImage = document.getElementById("menu_viewer_overlay_image");
        var overlayCloseButton = document.getElementById("menu_viewer_overlay_close");
        var overlayDismissNodes = overlay
            ? overlay.querySelectorAll("[data-menu-viewer-overlay-close]")
            : [];
        var pages = parsePages(dataNode);
        var totalPages = pages.length;
        var activeViewer = null;
        var currentIndex = 0;
        var switchToken = 0;
        var activeViewportKey = "";
        var restoreFocusNode = null;

        if (!viewerHost) {
            return;
        }

        if (desktopHost && desktopHost !== viewerHost) {
            desktopHost.hidden = true;
        }

        if (mobileHost && mobileHost !== viewerHost) {
            mobileHost.hidden = true;
        }

        viewerHost.hidden = false;

        function updateUi() {
            var displayIndex = totalPages > 0
                ? clamp(currentIndex, 0, totalPages - 1) + 1
                : 0;

            updateIndicators(currentIndicator, totalIndicator, displayIndex, totalPages);
            setButtonState(previousButton, totalPages === 0 || currentIndex <= 0);
            setButtonState(nextButton, totalPages === 0 || currentIndex >= totalPages - 1);

            if (hintNode && activeViewer && typeof activeViewer.getHint === "function") {
                hintNode.textContent = activeViewer.getHint();
            }

            if (zoomButton && activeViewer && typeof activeViewer.canOpenZoom === "function") {
                zoomButton.hidden = !activeViewer.canOpenZoom();
            } else if (zoomButton) {
                zoomButton.hidden = true;
            }
        }

        function closeOverlay() {
            if (!overlay || overlay.hidden) {
                return;
            }

            overlay.hidden = true;
            overlay.setAttribute("aria-hidden", "true");
            document.body.classList.remove("menu-viewer-overlay-open");

            if (overlayImage) {
                overlayImage.removeAttribute("src");
                overlayImage.removeAttribute("fetchpriority");
            }

            document.removeEventListener("keydown", handleOverlayKeydown);

            if (restoreFocusNode && typeof restoreFocusNode.focus === "function") {
                restoreFocusNode.focus();
            }

            restoreFocusNode = null;
        }

        function handleOverlayKeydown(event) {
            if (event.key === "Escape") {
                event.preventDefault();
                closeOverlay();
            }
        }

        function openOverlay(page, index) {
            if (!overlay || !overlayImage || !page) {
                return;
            }

            restoreFocusNode = document.activeElement;
            overlay.hidden = false;
            overlay.setAttribute("aria-hidden", "false");
            document.body.classList.add("menu-viewer-overlay-open");
            overlayImage.alt = getPageAltText(page, index);
            overlayImage.loading = "eager";
            overlayImage.setAttribute("fetchpriority", "high");
            overlayImage.setAttribute("src", page.imageUrl);
            document.addEventListener("keydown", handleOverlayKeydown);

            if (overlayCloseButton && typeof overlayCloseButton.focus === "function") {
                overlayCloseButton.focus();
            }
        }

        function destroyActiveViewer() {
            closeOverlay();

            if (activeViewer && typeof activeViewer.destroy === "function") {
                activeViewer.destroy();
            }

            activeViewer = null;
        }

        function getViewportKey() {
            return [window.innerWidth || 0, window.innerHeight || 0].join("x");
        }

        function createViewer(startIndex) {
            return createPageFlipViewer({
                host: viewerHost,
                pages: pages,
                startIndex: startIndex,
                openZoom: openOverlay,
                onIndexChange: function (index) {
                    currentIndex = clamp(index, 0, totalPages - 1);
                    updateUi();
                }
            });
        }

        function applyViewer(force) {
            var nextViewportKey = getViewportKey();

            if (!force && activeViewer && activeViewportKey === nextViewportKey) {
                if (typeof activeViewer.update === "function") {
                    activeViewer.update();
                }

                updateUi();
                return Promise.resolve();
            }

            currentIndex = activeViewer && typeof activeViewer.getIndex === "function"
                ? clamp(activeViewer.getIndex(), 0, totalPages - 1)
                : clamp(currentIndex, 0, totalPages - 1);

            destroyActiveViewer();

            if (totalPages === 0) {
                createEmptyState(
                    viewerHost || frame,
                    "Menu hi\u1ec7n ch\u01b0a c\u00f3 h\u00ecnh \u1ea3nh hi\u1ec3n th\u1ecb. Vui l\u00f2ng quay l\u1ea1i sau ho\u1eb7c li\u00ean h\u1ec7 nh\u00e0 h\u00e0ng \u0111\u1ec3 \u0111\u01b0\u1ee3c h\u1ed7 tr\u1ee3.");
                setButtonState(previousButton, true);
                setButtonState(nextButton, true);
                if (zoomButton) {
                    zoomButton.hidden = true;
                }
                return Promise.resolve();
            }

            switchToken += 1;
            activeViewportKey = nextViewportKey;
            var token = switchToken;

            frame.setAttribute("aria-busy", "true");

            return createViewer(currentIndex)
                .then(function (viewer) {
                    if (token !== switchToken) {
                        if (viewer && typeof viewer.destroy === "function") {
                            viewer.destroy();
                        }
                        return null;
                    }

                    activeViewer = viewer;

                    if (viewer && typeof viewer.mount === "function") {
                        return viewer.mount().then(function () {
                            currentIndex = viewer && typeof viewer.getIndex === "function"
                                ? clamp(viewer.getIndex(), 0, totalPages - 1)
                                : currentIndex;
                            frame.removeAttribute("aria-busy");
                            updateUi();
                        });
                    }

                    frame.removeAttribute("aria-busy");
                    updateUi();
                    return null;
                })
                .catch(function (error) {
                    frame.removeAttribute("aria-busy");

                    if (window.console && typeof window.console.warn === "function") {
                        window.console.warn("Menu viewer initialization failed. Falling back to the native pager.", error);
                    }

                    destroyActiveViewer();
                    activeViewer = createStaticFallbackViewer({
                        host: viewerHost,
                        pages: pages,
                        startIndex: currentIndex,
                        openZoom: openOverlay,
                        onIndexChange: function (index) {
                            currentIndex = clamp(index, 0, totalPages - 1);
                            updateUi();
                        }
                    });
                    updateUi();
                });
        }

        if (previousButton) {
            previousButton.addEventListener("click", function () {
                if (!activeViewer) {
                    return;
                }

                activeViewer.prev();
            });
        }

        if (nextButton) {
            nextButton.addEventListener("click", function () {
                if (!activeViewer) {
                    return;
                }

                activeViewer.next();
            });
        }

        if (zoomButton) {
            zoomButton.addEventListener("click", function () {
                if (!activeViewer || typeof activeViewer.openZoom !== "function") {
                    return;
                }

                activeViewer.openZoom();
            });
        }

        if (overlayCloseButton) {
            overlayCloseButton.addEventListener("click", closeOverlay);
        }

        Array.prototype.forEach.call(overlayDismissNodes, function (node) {
            node.addEventListener("click", closeOverlay);
        });

        var onViewportChange = debounce(function () {
            applyViewer(false);
        }, 140);

        window.addEventListener("resize", onViewportChange, { passive: true });
        window.addEventListener("orientationchange", onViewportChange, { passive: true });

        applyViewer(true);
    });
})();
