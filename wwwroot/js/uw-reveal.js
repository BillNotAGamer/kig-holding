(function () {
    'use strict';

    const SELECTOR = '[data-uw-reveal]';
    const elements = Array.from(document.querySelectorAll(SELECTOR));

    if (!elements.length) {
        return;
    }

    const supportsReducedMotion = typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    function getMotionOverride() {
        try {
            return window.localStorage.getItem('uwRevealMotion');
        } catch {
            return null;
        }
    }

    const motionOverride = getMotionOverride();
    // "force" lets a developer or user explicitly test reveal animations.
    // "off" disables reveals entirely. Otherwise we respect prefers-reduced-motion.
    const shouldReduceMotion = motionOverride === 'off'
        || (motionOverride !== 'force' && supportsReducedMotion);
    const supportsObserver = 'IntersectionObserver' in window;
    const supportsAnimate = typeof Element !== 'undefined'
        && typeof Element.prototype.animate === 'function';

    const defaultEnterDuration = 850;
    const defaultDistance = 56;
    const enterEasing = 'cubic-bezier(0.22, 1, 0.36, 1)';
    const exitEasing = 'cubic-bezier(0.4, 0, 0.2, 1)';
    const observerOptions = {
        root: null,
        threshold: [0, 0.15, 0.35],
        rootMargin: '0px 0px -10% 0px'
    };

    const stateMap = new WeakMap();

    function parseNumber(value, fallback) {
        const parsed = Number.parseInt(value, 10);
        return Number.isFinite(parsed) && parsed >= 0 ? parsed : fallback;
    }

    function getConfig(element) {
        return {
            type: element.getAttribute('data-uw-reveal') || 'fade-up',
            delay: parseNumber(element.getAttribute('data-uw-delay'), 0),
            duration: parseNumber(element.getAttribute('data-uw-duration'), defaultEnterDuration),
            distance: parseNumber(element.getAttribute('data-uw-distance'), defaultDistance),
            once: element.getAttribute('data-uw-once') === 'true'
        };
    }

    function getState(element) {
        let state = stateMap.get(element);

        if (!state) {
            state = {
                animation: null,
                visible: false,
                phase: 'hidden',
                token: 0,
                config: getConfig(element)
            };
            stateMap.set(element, state);
        }

        return state;
    }

    function clearInlineStyles(element) {
        element.style.removeProperty('opacity');
        element.style.removeProperty('transform');
        element.style.removeProperty('will-change');
    }

    function applyHiddenStyles(element, config) {
        const distance = `${config.distance}px`;
        let transform = 'none';

        switch (config.type) {
            case 'fade-down':
                transform = `translate3d(0, -${distance}, 0)`;
                break;
            case 'fade-left':
                transform = `translate3d(${distance}, 0, 0)`;
                break;
            case 'fade-right':
                transform = `translate3d(-${distance}, 0, 0)`;
                break;
            case 'zoom-up':
                transform = `translate3d(0, ${distance}, 0) scale(0.94)`;
                break;
            case 'fade-in':
                transform = 'none';
                break;
            case 'fade-up':
            default:
                transform = `translate3d(0, ${distance}, 0)`;
                break;
        }

        element.style.opacity = '0';
        element.style.transform = transform;
        element.style.willChange = config.type === 'fade-in' ? 'opacity' : 'opacity, transform';
    }

    function getVisibleTransform(type) {
        return type === 'fade-in' ? 'none' : 'translate3d(0, 0, 0)';
    }

    function getEnterKeyframes(config) {
        const visibleTransform = getVisibleTransform(config.type);
        const distance = `${config.distance}px`;

        switch (config.type) {
            case 'fade-down':
                return [
                    { opacity: 0, transform: `translate3d(0, -${distance}, 0)` },
                    { opacity: 1, transform: visibleTransform }
                ];
            case 'fade-left':
                return [
                    { opacity: 0, transform: `translate3d(${distance}, 0, 0)` },
                    { opacity: 1, transform: visibleTransform }
                ];
            case 'fade-right':
                return [
                    { opacity: 0, transform: `translate3d(-${distance}, 0, 0)` },
                    { opacity: 1, transform: visibleTransform }
                ];
            case 'fade-in':
                return [
                    { opacity: 0 },
                    { opacity: 1 }
                ];
            case 'zoom-up':
                return [
                    { opacity: 0, transform: `translate3d(0, ${distance}, 0) scale(0.94)` },
                    { opacity: 1, transform: 'translate3d(0, 0, 0) scale(1)' }
                ];
            case 'fade-up':
            default:
                return [
                    { opacity: 0, transform: `translate3d(0, ${distance}, 0)` },
                    { opacity: 1, transform: visibleTransform }
                ];
        }
    }

    function getExitKeyframes(config) {
        const visibleTransform = getVisibleTransform(config.type);
        const distance = `${config.distance}px`;

        switch (config.type) {
            case 'fade-down':
                return [
                    { opacity: 1, transform: visibleTransform },
                    { opacity: 0, transform: `translate3d(0, -${distance}, 0)` }
                ];
            case 'fade-left':
                return [
                    { opacity: 1, transform: visibleTransform },
                    { opacity: 0, transform: `translate3d(${distance}, 0, 0)` }
                ];
            case 'fade-right':
                return [
                    { opacity: 1, transform: visibleTransform },
                    { opacity: 0, transform: `translate3d(-${distance}, 0, 0)` }
                ];
            case 'fade-in':
                return [
                    { opacity: 1 },
                    { opacity: 0 }
                ];
            case 'zoom-up':
                return [
                    { opacity: 1, transform: 'translate3d(0, 0, 0) scale(1)' },
                    { opacity: 0, transform: `translate3d(0, ${distance}, 0) scale(0.94)` }
                ];
            case 'fade-up':
            default:
                return [
                    { opacity: 1, transform: visibleTransform },
                    { opacity: 0, transform: `translate3d(0, ${distance}, 0)` }
                ];
        }
    }

    function cancelAnimation(state) {
        if (state.animation) {
            state.animation.cancel();
            state.animation = null;
        }
    }

    function finishVisible(element, state, token) {
        if (state.token !== token) {
            return;
        }

        state.animation = null;
        clearInlineStyles(element);
        state.visible = true;
        state.phase = 'visible';
    }

    function finishHidden(element, state, config, token) {
        if (state.token !== token) {
            return;
        }

        state.animation = null;
        state.visible = false;
        applyHiddenStyles(element, config);
        state.phase = 'hidden';
    }

    function animateIn(element, state, config, observer) {
        cancelAnimation(state);
        state.token += 1;
        const token = state.token;
        state.visible = true;
        state.phase = 'entering';

        const animation = element.animate(getEnterKeyframes(config), {
            duration: config.duration,
            easing: enterEasing,
            delay: config.delay,
            fill: 'both'
        });

        state.animation = animation;

        animation.onfinish = () => finishVisible(element, state, token);
        animation.oncancel = () => {
            if (state.token === token) {
                state.animation = null;
            }
        };

        if (config.once && observer) {
            observer.unobserve(element);
        }
    }

    function animateOut(element, state, config) {
        cancelAnimation(state);
        state.token += 1;
        const token = state.token;
        state.visible = false;
        state.phase = 'exiting';
        const exitDuration = config.duration === defaultEnterDuration
            ? 650
            : Math.max(250, Math.round(config.duration * 0.76));

        const animation = element.animate(getExitKeyframes(config), {
            duration: exitDuration,
            easing: exitEasing,
            delay: 0,
            fill: 'both'
        });

        state.animation = animation;

        animation.onfinish = () => finishHidden(element, state, config, token);
        animation.oncancel = () => {
            if (state.token === token) {
                state.animation = null;
            }
        };
    }

    function isInViewport(element) {
        const rect = element.getBoundingClientRect();
        const viewportWidth = window.innerWidth || document.documentElement.clientWidth;
        const viewportHeight = window.innerHeight || document.documentElement.clientHeight;

        return rect.bottom > 0
            && rect.right > 0
            && rect.top < viewportHeight
            && rect.left < viewportWidth;
    }

    function revealAllImmediately() {
        elements.forEach((element) => {
            clearInlineStyles(element);
        });
    }

    if (shouldReduceMotion || !supportsObserver || !supportsAnimate) {
        revealAllImmediately();
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            const element = entry.target;
            const state = getState(element);
            const config = state.config;

            if (entry.isIntersecting) {
                if (state.phase === 'visible' || state.phase === 'entering') {
                    return;
                }

                animateIn(element, state, config, observer);
                return;
            }

            if (config.once || state.phase === 'hidden' || state.phase === 'exiting') {
                return;
            }

            animateOut(element, state, config);
        });
    }, observerOptions);

    const boot = () => {
        elements.forEach((element) => {
            const state = getState(element);
            const config = state.config;

            applyHiddenStyles(element, config);
            observer.observe(element);
        });

        requestAnimationFrame(() => {
            elements.forEach((element) => {
                const state = getState(element);
                const config = state.config;

                if (isInViewport(element)) {
                    animateIn(element, state, config, observer);
                }
            });
        });
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => requestAnimationFrame(boot), { once: true });
    } else {
        requestAnimationFrame(boot);
    }
})();
