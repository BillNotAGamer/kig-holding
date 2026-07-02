(function () {
    const trackedOnLoadAttribute = 'data-tiktok-onload-tracked';

    const normalizeParams = (params) => {
        const normalized = {};

        Object.entries(params || {}).forEach(([key, value]) => {
            if (typeof value !== 'string') {
                return;
            }

            const trimmedValue = value.trim();
            if (!trimmedValue) {
                return;
            }

            normalized[key] = trimmedValue;
        });

        return normalized;
    };

    window.kigTikTokTrack = function (eventName, params) {
        if (typeof eventName !== 'string' || !eventName.trim()) {
            return;
        }

        try {
            if (!window.ttq || typeof window.ttq.track !== 'function') {
                return;
            }

            const normalizedParams = normalizeParams(params);
            if (Object.keys(normalizedParams).length > 0) {
                window.ttq.track(eventName, normalizedParams);
                return;
            }

            window.ttq.track(eventName);
        }
        catch {
            // Ignore TikTok errors so public interactions remain unaffected.
        }
    };

    const buildParamsFromElement = (element) => {
        if (!(element instanceof HTMLElement)) {
            return {};
        }

        return normalizeParams({
            button_name: element.dataset.tiktokLabel,
            content_type: element.dataset.tiktokContentType,
            content_name: element.dataset.tiktokContentName,
            content_category: element.dataset.tiktokContentCategory,
            location: element.dataset.tiktokLocation,
            source: element.dataset.tiktokSource
        });
    };

    const trackElement = (element) => {
        if (!(element instanceof HTMLElement)) {
            return;
        }

        const eventName = element.dataset.tiktokEvent;
        if (!eventName) {
            return;
        }

        window.kigTikTokTrack(eventName, buildParamsFromElement(element));
    };

    const trackOnLoadElements = () => {
        document.querySelectorAll('[data-tiktok-track-onload][data-tiktok-event]').forEach((element) => {
            if (!(element instanceof HTMLElement) || element.hasAttribute(trackedOnLoadAttribute)) {
                return;
            }

            element.setAttribute(trackedOnLoadAttribute, 'true');
            trackElement(element);
        });
    };

    document.addEventListener('click', (event) => {
        if (!(event.target instanceof Element)) {
            return;
        }

        const trackableElement = event.target.closest('[data-tiktok-event]:not([data-tiktok-track-onload])');
        if (!(trackableElement instanceof HTMLElement)) {
            return;
        }

        trackElement(trackableElement);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', trackOnLoadElements, { once: true });
    }
    else {
        trackOnLoadElements();
    }
})();
