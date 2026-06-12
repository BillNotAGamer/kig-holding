(function () {
    function initSlider(root) {
        const slides = Array.from(root.querySelectorAll('[data-kbb-cook-slide]'));

        if (slides.length < 2) {
            return;
        }

        const reducedMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
        const autoplayDelay = 3000;
        const swipeThreshold = 40;

        let currentIndex = slides.findIndex((slide) => slide.dataset.kbbCookSlideActive === 'true');
        let autoplayId = null;
        let isHovered = false;
        let isVisible = document.visibilityState !== 'hidden';
        let pointerState = null;

        if (currentIndex < 0) {
            currentIndex = 0;
        }

        function clearAutoplay() {
            if (autoplayId !== null) {
                window.clearTimeout(autoplayId);
                autoplayId = null;
            }
        }

        function setSlideState(index) {
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
        }

        function scheduleAutoplay() {
            clearAutoplay();

            if (reducedMotionQuery.matches || isHovered || !isVisible) {
                return;
            }

            autoplayId = window.setTimeout(() => {
                setSlideState(currentIndex + 1);
                scheduleAutoplay();
            }, autoplayDelay);
        }

        function restartAutoplay() {
            scheduleAutoplay();
        }

        function handleManualDirection(direction) {
            if (direction === 0) {
                return;
            }

            setSlideState(currentIndex + direction);
            restartAutoplay();
        }

        function resetPointerState() {
            pointerState = null;
        }

        function onPointerDown(event) {
            if (event.pointerType === 'mouse' && event.button !== 0) {
                return;
            }

            pointerState = {
                id: event.pointerId,
                startX: event.clientX,
                startY: event.clientY,
                lastX: event.clientX,
                lastY: event.clientY
            };

            if (typeof root.setPointerCapture === 'function') {
                try {
                    root.setPointerCapture(event.pointerId);
                } catch {
                    // ignore capture failures
                }
            }
        }

        function onPointerMove(event) {
            if (!pointerState || event.pointerId !== pointerState.id) {
                return;
            }

            pointerState.lastX = event.clientX;
            pointerState.lastY = event.clientY;
        }

        function finishPointer(event) {
            if (!pointerState || event.pointerId !== pointerState.id) {
                return;
            }

            const endX = pointerState.lastX ?? event.clientX;
            const endY = pointerState.lastY ?? event.clientY;
            const deltaX = endX - pointerState.startX;
            const deltaY = endY - pointerState.startY;

            if (typeof root.releasePointerCapture === 'function') {
                try {
                    root.releasePointerCapture(event.pointerId);
                } catch {
                    // ignore release failures
                }
            }

            resetPointerState();

            if (Math.abs(deltaX) < swipeThreshold || Math.abs(deltaX) <= Math.abs(deltaY)) {
                return;
            }

            handleManualDirection(deltaX < 0 ? 1 : -1);
        }

        function syncReducedMotion() {
            root.classList.toggle('is-reduced-motion', reducedMotionQuery.matches);

            if (reducedMotionQuery.matches) {
                clearAutoplay();
            } else {
                scheduleAutoplay();
            }
        }

        setSlideState(currentIndex);
        syncReducedMotion();

        root.addEventListener('pointerdown', onPointerDown);
        root.addEventListener('pointermove', onPointerMove);
        root.addEventListener('pointerup', finishPointer);
        root.addEventListener('pointercancel', finishPointer);
        root.addEventListener('mouseenter', () => {
            isHovered = true;
            clearAutoplay();
        });
        root.addEventListener('mouseleave', () => {
            isHovered = false;
            scheduleAutoplay();
        });

        document.addEventListener('visibilitychange', () => {
            isVisible = document.visibilityState !== 'hidden';
            scheduleAutoplay();
        });

        if (typeof reducedMotionQuery.addEventListener === 'function') {
            reducedMotionQuery.addEventListener('change', syncReducedMotion);
        } else if (typeof reducedMotionQuery.addListener === 'function') {
            reducedMotionQuery.addListener(syncReducedMotion);
        }
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
