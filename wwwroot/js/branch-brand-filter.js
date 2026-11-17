// /chi-nhanh: filter branch cards by brand (client-side, works together with the city/search filters).
(function () {
    'use strict';

    var filter = document.querySelector('[data-branch-brand-filter]');
    if (!filter) return;

    var chips = Array.prototype.slice.call(filter.querySelectorAll('[data-brand]'));
    var items = Array.prototype.slice.call(document.querySelectorAll('[data-branch-brand]'));
    var groups = Array.prototype.slice.call(document.querySelectorAll('[data-branch-group]'));
    var totalLabel = document.querySelector('[data-branch-visible-count]');

    function apply(brand, updateUrl) {
        var visibleTotal = 0;

        items.forEach(function (item) {
            var show = !brand || item.getAttribute('data-branch-brand') === brand;
            item.hidden = !show;
        });

        groups.forEach(function (group) {
            var visible = group.querySelectorAll('[data-branch-brand]:not([hidden])').length;
            group.hidden = visible === 0;
            var count = group.querySelector('[data-branch-group-count]');
            if (count) count.textContent = String(visible);
            visibleTotal += visible;
        });

        if (totalLabel) totalLabel.textContent = String(visibleTotal);

        chips.forEach(function (chip) {
            chip.setAttribute('aria-pressed', chip.getAttribute('data-brand') === brand ? 'true' : 'false');
        });

        if (updateUrl && window.history && window.URL) {
            var url = new URL(window.location.href);
            if (brand) {
                url.searchParams.set('brand', brand);
            } else {
                url.searchParams.delete('brand');
            }
            window.history.replaceState(null, '', url.toString());
        }

        // Keep the chosen brand when switching city.
        document.querySelectorAll('[data-branch-locator] a[href*="/chi-nhanh"]').forEach(function (link) {
            try {
                var target = new URL(link.href, window.location.origin);
                if (brand) {
                    target.searchParams.set('brand', brand);
                } else {
                    target.searchParams.delete('brand');
                }
                link.href = target.pathname + target.search + target.hash;
            } catch (e) { /* ignore */ }
        });
    }

    chips.forEach(function (chip) {
        chip.addEventListener('click', function () {
            apply(chip.getAttribute('data-brand') || '', true);
        });
    });

    var initial = '';
    try {
        initial = new URL(window.location.href).searchParams.get('brand') || '';
    } catch (e) {
        initial = '';
    }

    var known = chips.some(function (chip) { return chip.getAttribute('data-brand') === initial; });
    apply(known ? initial : '', false);
})();
