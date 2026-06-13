(() => {
    const counters = document.querySelectorAll("[data-count-up]");
    if (!counters.length) {
        return;
    }

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        return;
    }

    const formatter = new Intl.NumberFormat("vi-VN");
    const durationMs = 1750;

    const renderValue = (element, value) => {
        const prefix = element.dataset.countPrefix ?? "";
        const suffix = element.dataset.countSuffix ?? "";
        element.textContent = `${prefix}${formatter.format(value)}${suffix}`;
    };

    const animateCounter = (element) => {
        if (element.dataset.countAnimated === "true") {
            return;
        }

        element.dataset.countAnimated = "true";

        const target = Number.parseInt(element.dataset.countTo ?? "", 10);
        if (!Number.isFinite(target)) {
            return;
        }

        const startTime = performance.now();

        const tick = (timestamp) => {
            const elapsed = timestamp - startTime;
            const progress = Math.min(elapsed / durationMs, 1);
            const easedProgress = 1 - Math.pow(1 - progress, 3);
            const currentValue = Math.round(target * easedProgress);

            renderValue(element, currentValue);

            if (progress < 1) {
                window.requestAnimationFrame(tick);
                return;
            }

            renderValue(element, target);
        };

        renderValue(element, 0);
        window.requestAnimationFrame(tick);
    };

    counters.forEach((element) => {
        renderValue(element, 0);
    });

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) {
                return;
            }

            animateCounter(entry.target);
            observer.unobserve(entry.target);
        });
    }, {
        threshold: 0.35,
        rootMargin: "0px 0px -10% 0px"
    });

    counters.forEach((element) => {
        observer.observe(element);
    });
})();
