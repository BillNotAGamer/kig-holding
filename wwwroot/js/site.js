(function () {
    const body = document.body;
    const header = document.querySelector('[data-site-header]');
    const drawer = document.querySelector('[data-mobile-menu]');
    const overlay = document.querySelector('[data-mobile-menu-overlay]');
    const openButton = document.querySelector('[data-mobile-menu-open]');
    const closeButtons = document.querySelectorAll('[data-mobile-menu-close], [data-mobile-menu-link]');
    const backToTopButton = document.querySelector('[data-back-to-top]');
    const menuFilterButtons = document.querySelectorAll('[data-menu-filter]');
    const menuCards = document.querySelectorAll('[data-menu-card]');

    const openDrawer = () => {
        if (!drawer || !overlay || !openButton) {
            return;
        }

        drawer.classList.add('is-open');
        overlay.classList.add('is-open');
        drawer.setAttribute('aria-hidden', 'false');
        openButton.setAttribute('aria-expanded', 'true');
        body.classList.add('overflow-hidden');
    };

    const closeDrawer = () => {
        if (!drawer || !overlay || !openButton) {
            return;
        }

        drawer.classList.remove('is-open');
        overlay.classList.remove('is-open');
        drawer.setAttribute('aria-hidden', 'true');
        openButton.setAttribute('aria-expanded', 'false');
        body.classList.remove('overflow-hidden');
    };

    const filterMenuCards = (filter) => {
        if (!menuCards.length) {
            return;
        }

        menuCards.forEach((card) => {
            const category = card.getAttribute('data-menu-category');
            const shouldShow = filter === 'all' || category === filter;
            card.classList.toggle('hidden', !shouldShow);
        });

        menuFilterButtons.forEach((button) => {
            button.dataset.active = button.getAttribute('data-menu-filter') === filter ? 'true' : 'false';
        });
    };

    if (openButton) {
        openButton.addEventListener('click', openDrawer);
    }

    if (overlay) {
        overlay.addEventListener('click', closeDrawer);
    }

    closeButtons.forEach((button) => {
        button.addEventListener('click', closeDrawer);
    });

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            closeDrawer();
        }
    });

    if (backToTopButton) {
        backToTopButton.addEventListener('click', () => {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    menuFilterButtons.forEach((button) => {
        button.addEventListener('click', () => {
            filterMenuCards(button.getAttribute('data-menu-filter') || 'all');
        });
    });
})();
