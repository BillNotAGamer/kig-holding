(function () {
    'use strict';

    var rootSelector = '[data-member-tier-tabs]';
    var tabSelector = '[data-member-tier-tab]';
    var panelSelector = '[data-member-tier-panel]';
    var visualSelector = '[data-member-tier-visual]';
    var initializedAttribute = 'data-member-tier-tabs-bound';
    var reduceMotionQuery = '(prefers-reduced-motion: reduce)';

    function getTabs(root) {
        return Array.prototype.slice.call(root.querySelectorAll(tabSelector));
    }

    function getPanels(root) {
        return Array.prototype.slice.call(root.querySelectorAll(panelSelector));
    }

    function getVisualCards(root) {
        return Array.prototype.slice.call(root.querySelectorAll(visualSelector));
    }

    function getTierKey(element, attributeName) {
        return element ? element.getAttribute(attributeName) : null;
    }

    function shouldReduceMotion() {
        return window.matchMedia && window.matchMedia(reduceMotionQuery).matches;
    }

    function scrollTabIntoView(tab) {
        if (!tab || typeof tab.scrollIntoView !== 'function') {
            return;
        }

        tab.scrollIntoView({
            behavior: shouldReduceMotion() ? 'auto' : 'smooth',
            block: 'nearest',
            inline: 'center'
        });
    }

    function activateTier(root, tierKey, options) {
        var settings = options || {};
        var tabs = getTabs(root);
        var panels = getPanels(root);
        var visuals = getVisualCards(root);
        var activeTab = null;

        tabs.forEach(function (tab) {
            var isActive = getTierKey(tab, 'data-member-tier-tab') === tierKey;
            tab.setAttribute('aria-selected', isActive ? 'true' : 'false');
            tab.setAttribute('tabindex', isActive ? '0' : '-1');

            if (isActive) {
                activeTab = tab;
            }
        });

        panels.forEach(function (panel) {
            var isActive = getTierKey(panel, 'data-member-tier-panel') === tierKey;
            panel.hidden = !isActive;
        });

        visuals.forEach(function (card) {
            var isActive = getTierKey(card, 'data-member-tier-visual') === tierKey;
            card.hidden = !isActive;
        });

        if (activeTab && settings.focus) {
            activeTab.focus({ preventScroll: true });
        }

        if (activeTab && settings.scroll) {
            scrollTabIntoView(activeTab);
        }
    }

    function getActiveIndex(tabs) {
        var index = tabs.findIndex(function (tab) {
            return tab.getAttribute('aria-selected') === 'true';
        });

        return index >= 0 ? index : 0;
    }

    function moveToIndex(root, index) {
        var tabs = getTabs(root);

        if (!tabs.length) {
            return;
        }

        var nextIndex = (index + tabs.length) % tabs.length;
        var nextTab = tabs[nextIndex];
        var tierKey = getTierKey(nextTab, 'data-member-tier-tab');

        activateTier(root, tierKey, {
            focus: true,
            scroll: true
        });
    }

    function onTabClick(root, event) {
        var tab = event.currentTarget;
        var tierKey = getTierKey(tab, 'data-member-tier-tab');

        activateTier(root, tierKey, {
            focus: false,
            scroll: true
        });
    }

    function onTabKeydown(root, event) {
        var tabs = getTabs(root);
        var activeIndex = getActiveIndex(tabs);

        switch (event.key) {
            case 'ArrowRight':
            case 'Right':
                event.preventDefault();
                moveToIndex(root, activeIndex + 1);
                break;
            case 'ArrowLeft':
            case 'Left':
                event.preventDefault();
                moveToIndex(root, activeIndex - 1);
                break;
            case 'Home':
                event.preventDefault();
                moveToIndex(root, 0);
                break;
            case 'End':
                event.preventDefault();
                moveToIndex(root, tabs.length - 1);
                break;
            case 'Enter':
            case ' ':
            case 'Spacebar':
                event.preventDefault();
                activateTier(root, getTierKey(event.currentTarget, 'data-member-tier-tab'), {
                    focus: false,
                    scroll: true
                });
                break;
            default:
                break;
        }
    }

    function initialize(root) {
        if (!root || root.getAttribute(initializedAttribute) === 'true') {
            return;
        }

        var tabs = getTabs(root);

        if (!tabs.length) {
            return;
        }

        root.setAttribute(initializedAttribute, 'true');
        root.classList.add('member-tier--enhanced');

        tabs.forEach(function (tab) {
            tab.addEventListener('click', onTabClick.bind(null, root));
            tab.addEventListener('keydown', onTabKeydown.bind(null, root));
        });

        var initiallySelected = tabs.find(function (tab) {
            return tab.getAttribute('aria-selected') === 'true';
        }) || tabs[0];

        activateTier(root, getTierKey(initiallySelected, 'data-member-tier-tab'), {
            focus: false,
            scroll: false
        });
    }

    function initializeAll() {
        Array.prototype.slice.call(document.querySelectorAll(rootSelector)).forEach(initialize);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeAll, { once: true });
    } else {
        initializeAll();
    }
}());
