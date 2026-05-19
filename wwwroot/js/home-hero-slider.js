(function () {
    const root = document.querySelector('[data-champong-hero-slider]');

    if (!root) {
        return;
    }

    const slides = Array.from(root.querySelectorAll('[data-champong-hero-slide]'));
    const prevButton = root.querySelector('[data-champong-hero-prev]');
    const nextButton = root.querySelector('[data-champong-hero-next]');
    const dots = Array.from(root.querySelectorAll('[data-champong-hero-dot]'));
    const currentLabel = root.querySelector('[data-champong-hero-current]');
    const totalLabel = root.querySelector('[data-champong-hero-total]');
    const reducedMotionQuery = window.matchMedia('(prefers-reduced-motion: reduce)');

    const autoplayDelay = 6500;
    const transitionDuration = 700;

    let currentIndex = 0;
    let autoplayId = null;
    let isHovered = false;
    let isFocused = false;
    let isVisible = document.visibilityState !== 'hidden';

    const hideTimers = new WeakMap();

    function updateCounter(index) {
        if (currentLabel) {
            currentLabel.textContent = String(index + 1).padStart(2, '0');
        }

        if (totalLabel) {
            totalLabel.textContent = String(slides.length).padStart(2, '0');
        }
    }

    function updateDots(index) {
        dots.forEach((dot, dotIndex) => {
            const active = dotIndex === index;

            dot.setAttribute('aria-current', active ? 'true' : 'false');
            dot.classList.toggle('bg-white/35', active);
            dot.classList.toggle('border-white/35', active);
            dot.classList.toggle('bg-transparent', !active);
            dot.classList.toggle('border-white/30', !active);
        });
    }

    function setRevealState(slide, active) {
        const revealItems = slide.querySelectorAll('[data-champong-hero-reveal]');

        revealItems.forEach((item) => {
            const delay = Number(item.dataset.champongHeroDelay || '0') || 0;
            item.style.transitionDelay = active ? `${delay}ms` : '0ms';

            if (active) {
                item.classList.remove('opacity-0', 'translate-y-5');
                item.classList.add('opacity-100', 'translate-y-0');
            } else {
                item.classList.add('opacity-0', 'translate-y-5');
                item.classList.remove('opacity-100', 'translate-y-0');
            }
        });
    }

    function activateSlide(index, immediate = false) {
        if (!slides.length) {
            return;
        }

        const nextIndex = (index + slides.length) % slides.length;
        if (nextIndex === currentIndex && !immediate) {
            return;
        }

        const currentSlide = slides[currentIndex];
        const nextSlide = slides[nextIndex];

        if (currentSlide && currentSlide !== nextSlide) {
            window.clearTimeout(hideTimers.get(currentSlide));
            currentSlide.setAttribute('aria-hidden', 'true');
            currentSlide.classList.remove('opacity-100', 'z-20');
            currentSlide.classList.remove('is-active');
            currentSlide.classList.add('opacity-0', 'z-10', 'pointer-events-none');
            setRevealState(currentSlide, false);

            const hideDelay = immediate || reducedMotionQuery.matches ? 0 : transitionDuration;
            const hideTimer = window.setTimeout(() => {
                currentSlide.hidden = true;
                if ('inert' in currentSlide) {
                    currentSlide.inert = true;
                }
            }, hideDelay);
            hideTimers.set(currentSlide, hideTimer);
        }

        currentIndex = nextIndex;

        window.clearTimeout(hideTimers.get(nextSlide));
        nextSlide.hidden = false;
        if ('inert' in nextSlide) {
            nextSlide.inert = false;
        }
        nextSlide.setAttribute('aria-hidden', 'false');
        nextSlide.classList.add('is-active');
        nextSlide.classList.remove('pointer-events-none', 'z-10', 'opacity-0');
        nextSlide.classList.add('pointer-events-auto', 'z-20', 'opacity-100');

        if (immediate || reducedMotionQuery.matches) {
            setRevealState(nextSlide, true);
        } else {
            setRevealState(nextSlide, false);
            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    setRevealState(nextSlide, true);
                });
            });
        }

        updateCounter(nextIndex);
        updateDots(nextIndex);
    }

    function scheduleAutoplay() {
        window.clearTimeout(autoplayId);

        if (slides.length < 2 || reducedMotionQuery.matches || isHovered || isFocused || !isVisible) {
            return;
        }

        autoplayId = window.setTimeout(() => {
            activateSlide(currentIndex + 1);
            scheduleAutoplay();
        }, autoplayDelay);
    }

    function updatePauseState() {
        window.clearTimeout(autoplayId);
        scheduleAutoplay();
    }

    function setHoverState(value) {
        isHovered = value;
        updatePauseState();
    }

    function setFocusState(value) {
        isFocused = value;
        updatePauseState();
    }

    slides.forEach((slide, index) => {
        slide.hidden = index !== 0;
        if ('inert' in slide) {
            slide.inert = index !== 0;
        }
        slide.setAttribute('aria-hidden', index === 0 ? 'false' : 'true');
        slide.classList.toggle('is-active', index === 0);
        slide.classList.toggle('opacity-100', index === 0);
        slide.classList.toggle('opacity-0', index !== 0);
        slide.classList.toggle('pointer-events-auto', index === 0);
        slide.classList.toggle('pointer-events-none', index !== 0);
        slide.classList.toggle('z-20', index === 0);
        slide.classList.toggle('z-10', index !== 0);
        setRevealState(slide, index === 0);
    });

    updateCounter(0);
    updateDots(0);

    if (prevButton) {
        prevButton.addEventListener('click', () => {
            activateSlide(currentIndex - 1, reducedMotionQuery.matches);
            updatePauseState();
        });
    }

    if (nextButton) {
        nextButton.addEventListener('click', () => {
            activateSlide(currentIndex + 1, reducedMotionQuery.matches);
            updatePauseState();
        });
    }

    dots.forEach((dot) => {
        dot.addEventListener('click', () => {
            const target = Number(dot.dataset.champongHeroDot || '0');
            activateSlide(target, reducedMotionQuery.matches);
            updatePauseState();
        });
    });

    root.addEventListener('mouseenter', () => setHoverState(true));
    root.addEventListener('mouseleave', () => setHoverState(false));
    root.addEventListener('focusin', () => setFocusState(true));
    root.addEventListener('focusout', () => {
        window.setTimeout(() => {
            setFocusState(root.contains(document.activeElement));
        }, 0);
    });

    root.addEventListener('keydown', (event) => {
        if (event.key === 'ArrowLeft') {
            event.preventDefault();
            activateSlide(currentIndex - 1, reducedMotionQuery.matches);
            updatePauseState();
        }

        if (event.key === 'ArrowRight') {
            event.preventDefault();
            activateSlide(currentIndex + 1, reducedMotionQuery.matches);
            updatePauseState();
        }
    });

    document.addEventListener('visibilitychange', () => {
        isVisible = document.visibilityState !== 'hidden';
        updatePauseState();
    });

    if (reducedMotionQuery.addEventListener) {
        reducedMotionQuery.addEventListener('change', () => {
            activateSlide(currentIndex, true);
            updatePauseState();
        });
    } else if (reducedMotionQuery.addListener) {
        reducedMotionQuery.addListener(() => {
            activateSlide(currentIndex, true);
            updatePauseState();
        });
    }

    scheduleAutoplay();
})();
