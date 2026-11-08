(function () {
    function initSlider(root) {
        if (root.dataset.kbbCookSliderInitialized === 'true') {
            return;
        }

        const section = root.closest('.kbb-slider');
        const slides = Array.from(root.querySelectorAll('[data-kbb-cook-slide]'));
        const packages = section
            ? Array.from(section.querySelectorAll('[data-kbb-cook-package]'))
            : [];
        const dots = Array.from(root.querySelectorAll('[data-kbb-cook-slider-dot]'));
        const previousButton = root.querySelector('[data-kbb-cook-slider-previous]');
        const nextButton = root.querySelector('[data-kbb-cook-slider-next]');
        const status = root.parentElement?.querySelector('[data-kbb-cook-slider-status]');

        if (slides.length !== 2 || packages.length !== slides.length || dots.length !== slides.length) {
            return;
        }

        root.dataset.kbbCookSliderInitialized = 'true';

        const autoplayDelay = 6000;
        const reducedMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');

        let currentIndex = slides.findIndex((slide) => slide.dataset.kbbCookSlideActive === 'true');
        let autoplayId = null;
        let isHovered = false;
        let hasFocusWithin = false;
        let isDocumentVisible = !document.hidden;

        if (currentIndex < 0) {
            currentIndex = 0;
        }

        function setActive(index, announce = true) {
            currentIndex = (index + slides.length) % slides.length;

            slides.forEach((slide, slideIndex) => {
                const isActive = slideIndex === currentIndex;
                slide.classList.toggle('is-active', isActive);
                slide.dataset.kbbCookSlideActive = isActive ? 'true' : 'false';
                if (isActive) {
                    slide.removeAttribute('aria-hidden');
                } else {
                    slide.setAttribute('aria-hidden', 'true');
                }
            });

            packages.forEach((packageElement, packageIndex) => {
                const isActive = packageIndex === currentIndex;
                packageElement.classList.toggle('is-active', isActive);
                packageElement.setAttribute('aria-pressed', isActive ? 'true' : 'false');
            });

            dots.forEach((dot, dotIndex) => {
                const isActive = dotIndex === currentIndex;
                dot.classList.toggle('is-active', isActive);
                if (isActive) {
                    dot.setAttribute('aria-current', 'true');
                } else {
                    dot.removeAttribute('aria-current');
                }
            });

            if (status && announce) {
                status.textContent = currentIndex === 0
                    ? 'Đang xem gói tiêu chuẩn 278K'
                    : 'Đang xem gói thượng hạng 458K';
            }
        }

        function clearAutoplay() {
            if (autoplayId !== null) {
                window.clearTimeout(autoplayId);
                autoplayId = null;
            }
        }

        function canAutoplay() {
            return !reducedMotionQuery.matches
                && !isHovered
                && !hasFocusWithin
                && isDocumentVisible;
        }

        function scheduleAutoplay() {
            clearAutoplay();

            if (!canAutoplay()) {
                return;
            }

            autoplayId = window.setTimeout(() => {
                setActive(currentIndex + 1, false);
                scheduleAutoplay();
            }, autoplayDelay);
        }

        function selectManually(index) {
            setActive(index);
            scheduleAutoplay();
        }

        function moveManually(amount) {
            selectManually(currentIndex + amount);
        }

        previousButton?.addEventListener('click', () => moveManually(-1));
        nextButton?.addEventListener('click', () => moveManually(1));
        dots.forEach((dot, index) => dot.addEventListener('click', () => selectManually(index)));
        packages.forEach((packageElement, index) => {
            packageElement.addEventListener('click', () => selectManually(index));
        });

        root.addEventListener('keydown', (event) => {
            if (event.key === 'ArrowLeft') {
                event.preventDefault();
                moveManually(-1);
            } else if (event.key === 'ArrowRight') {
                event.preventDefault();
                moveManually(1);
            } else if (event.key === 'Home') {
                event.preventDefault();
                selectManually(0);
            } else if (event.key === 'End') {
                event.preventDefault();
                selectManually(slides.length - 1);
            }
        });

        section.addEventListener('mouseenter', () => {
            isHovered = true;
            clearAutoplay();
        });

        section.addEventListener('mouseleave', () => {
            isHovered = false;
            scheduleAutoplay();
        });

        section.addEventListener('focusin', () => {
            hasFocusWithin = true;
            clearAutoplay();
        });

        section.addEventListener('focusout', (event) => {
            if (event.relatedTarget instanceof Node && section.contains(event.relatedTarget)) {
                return;
            }

            hasFocusWithin = false;
            scheduleAutoplay();
        });

        document.addEventListener('visibilitychange', () => {
            isDocumentVisible = !document.hidden;
            scheduleAutoplay();
        });

        function handleReducedMotionChange() {
            scheduleAutoplay();
        }

        if (typeof reducedMotionQuery.addEventListener === 'function') {
            reducedMotionQuery.addEventListener('change', handleReducedMotionChange);
        } else if (typeof reducedMotionQuery.addListener === 'function') {
            reducedMotionQuery.addListener(handleReducedMotionChange);
        }

        setActive(currentIndex, false);
        scheduleAutoplay();
    }

    function init() {
        document.querySelectorAll('[data-kbb-cook-slider]').forEach(initSlider);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init, { once: true });
    } else {
        init();
    }
})();
