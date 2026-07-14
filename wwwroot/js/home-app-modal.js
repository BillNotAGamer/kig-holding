(function () {
    const init = () => {
        const root = document.querySelector('[data-home-app-modal]');
        const triggers = Array.from(document.querySelectorAll('[data-home-app-modal-trigger]'));

        if (!(root instanceof HTMLElement) || root.getAttribute('data-home-app-modal-bound') === 'true' || !triggers.length) {
            return;
        }

        const dialog = root.querySelector('[data-home-app-modal-dialog]');
        const backdrop = root.querySelector('[data-home-app-modal-backdrop]');
        const closeButtons = Array.from(root.querySelectorAll('[data-home-app-modal-close]'));

        if (!(dialog instanceof HTMLElement) || !(backdrop instanceof HTMLElement) || !closeButtons.length) {
            return;
        }

        root.setAttribute('data-home-app-modal-bound', 'true');

        const body = document.body;
        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const focusableSelector = [
            'a[href]',
            'button:not([disabled])',
            'input:not([disabled])',
            'select:not([disabled])',
            'textarea:not([disabled])',
            '[tabindex]:not([tabindex="-1"])'
        ].join(',');

        let activeTrigger = null;

        const isOpen = () => root.classList.contains('is-open');

        const isBookingModalOpen = () => {
            const bookingModal = document.querySelector('[data-booking-modal]');
            return bookingModal instanceof HTMLElement && bookingModal.classList.contains('is-open');
        };

        const getFocusableElements = () =>
            Array.from(root.querySelectorAll(focusableSelector))
                .filter((element) =>
                    element instanceof HTMLElement &&
                    !element.hasAttribute('hidden') &&
                    element.getAttribute('aria-hidden') !== 'true' &&
                    element.getClientRects().length > 0);

        const focusElement = (element) => {
            if (!(element instanceof HTMLElement)) {
                return;
            }

            try {
                element.focus({ preventScroll: true });
            }
            catch {
                element.focus();
            }
        };

        const closeMobileMenu = () => {
            const drawer = document.querySelector('[data-mobile-menu]');
            if (drawer instanceof HTMLElement && drawer.classList.contains('is-open')) {
                const closeButton = document.querySelector('[data-mobile-menu-close]');
                if (closeButton instanceof HTMLElement) {
                    closeButton.click();
                }
            }
        };

        const closeFloatingContacts = () => {
            document.querySelectorAll('[data-floating-contact].is-open').forEach((floatingRoot) => {
                if (!(floatingRoot instanceof HTMLElement)) {
                    return;
                }

                const toggle = floatingRoot.querySelector('[data-floating-contact-toggle]');
                const actionsContainer = floatingRoot.querySelector('[data-floating-contact-actions]');
                const actions = Array.from(floatingRoot.querySelectorAll('[data-floating-contact-action]'));
                const openLabel = floatingRoot.dataset.floatingContactOpenLabel || 'Mở liên hệ nhanh';

                floatingRoot.classList.remove('is-open');

                if (toggle instanceof HTMLElement) {
                    toggle.setAttribute('aria-expanded', 'false');
                    toggle.setAttribute('aria-label', openLabel);
                }

                if (actionsContainer instanceof HTMLElement) {
                    actionsContainer.setAttribute('aria-hidden', 'true');
                }

                actions.forEach((action) => {
                    if (action instanceof HTMLElement) {
                        action.setAttribute('aria-hidden', 'true');
                        action.setAttribute('tabindex', '-1');
                    }
                });
            });
        };

        const suppressBookingAutoOpen = () => {
            document.dispatchEvent(
                new CustomEvent('kig:booking-modal:suppress-auto-open', {
                    detail: { source: 'home-app-modal' }
                })
            );
        };

        const openModal = (trigger) => {
            if (isOpen() || isBookingModalOpen()) {
                focusElement(trigger);
                return;
            }

            activeTrigger = trigger instanceof HTMLElement ? trigger : document.activeElement;

            suppressBookingAutoOpen();
            closeMobileMenu();
            closeFloatingContacts();

            root.hidden = false;
            root.setAttribute('aria-hidden', 'false');
            body.classList.add('home-app-modal-open');

            window.requestAnimationFrame(() => {
                root.classList.add('is-open');
                focusElement(closeButtons[0] instanceof HTMLElement ? closeButtons[0] : dialog);
            });
        };

        const closeModal = ({ restoreFocus = true } = {}) => {
            if (!isOpen()) {
                return;
            }

            root.classList.remove('is-open');
            root.setAttribute('aria-hidden', 'true');
            body.classList.remove('home-app-modal-open');
            root.hidden = true;

            const focusTarget =
                restoreFocus &&
                activeTrigger instanceof HTMLElement &&
                document.contains(activeTrigger)
                    ? activeTrigger
                    : null;

            activeTrigger = null;

            if (focusTarget) {
                window.requestAnimationFrame(() => focusElement(focusTarget));
            }
        };

        const handleKeydown = (event) => {
            if (!isOpen()) {
                return;
            }

            if (event.key === 'Escape') {
                event.preventDefault();
                event.stopPropagation();
                closeModal();
                return;
            }

            if (event.key !== 'Tab') {
                return;
            }

            const focusableElements = getFocusableElements();
            if (!focusableElements.length) {
                event.preventDefault();
                focusElement(dialog);
                return;
            }

            const first = focusableElements[0];
            const last = focusableElements[focusableElements.length - 1];

            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                focusElement(last);
            }
            else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                focusElement(first);
            }
        };

        triggers.forEach((trigger) => {
            trigger.addEventListener('click', (event) => {
                event.preventDefault();
                openModal(trigger);
            });
        });

        closeButtons.forEach((button) => {
            button.addEventListener('click', () => closeModal());
        });

        backdrop.addEventListener('click', () => closeModal());
        document.addEventListener('keydown', handleKeydown);

        if (prefersReducedMotion) {
            root.dataset.homeAppModalReducedMotion = 'true';
        }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init, { once: true });
    }
    else {
        init();
    }
})();
