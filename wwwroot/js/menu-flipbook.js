(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var bookNode = document.getElementById("menu_group_flipbook");
        if (!bookNode) {
            return;
        }

        // Verify the options variable exists
        if (!window.option_menu_group_flipbook || !Array.isArray(window.option_menu_group_flipbook.source)) {
            console.error("DearFlip configuration option_menu_group_flipbook or source is missing.");
            return;
        }

        // Check if DearFlip assets loaded correctly.
        // DFLIP is the standard global object registered by the DearFlip plugin.
        if (typeof window.DFLIP === "undefined" && typeof window.dFlip === "undefined" && typeof jQuery.fn.flipBook === "undefined") {
            console.warn("DearFlip script could not be loaded from CDN. Activating premium responsive fallback gallery.");

            var fallbackContainer = document.createElement("div");
            fallbackContainer.className = "flex flex-col gap-6 max-w-4xl mx-auto p-4";

            window.option_menu_group_flipbook.source.forEach(function (src, index) {
                var imgWrapper = document.createElement("div");
                imgWrapper.className = "border border-white/5 rounded-2xl overflow-hidden bg-black/40 p-3 shadow-xl";

                var img = document.createElement("img");
                img.src = src;
                img.className = "w-full h-auto object-contain rounded-xl mx-auto block max-h-[85svh]";
                img.alt = "Trang thực đơn " + (index + 1);
                img.loading = index === 0 ? "eager" : "lazy";

                imgWrapper.appendChild(img);
                fallbackContainer.appendChild(imgWrapper);
            });

            bookNode.parentNode.appendChild(fallbackContainer);
            bookNode.remove();
        } else {
            console.log("DearFlip flipbook successfully initialized.");
        }
    });
})();
