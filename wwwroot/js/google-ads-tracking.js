(function () {
    // Google Ads conversions (account TRUYỀN THUYẾT CHAMPONG, tag AW-17917718766).
    const conversionId = 'AW-17917718766';
    const labels = {
        reservation: 'nMG0COGkz_MbEO7h6t9C', // TechSol - Form đặt bàn 22118
        messenger: 'wdgjCOnM3PMbEO7h6t9C',   // TechSol - Messenger 8597
        zalo: 'hCduCK7fubkdEO7h6t9C'         // KIG - Click Zalo (kigholding.vn)
    };
    const trackedOnLoadAttribute = 'data-gads-onload-tracked';

    window.kigGadsConversion = function (key) {
        const label = labels[key];
        if (!label) {
            return;
        }

        try {
            if (typeof window.gtag !== 'function') {
                return;
            }

            window.gtag('event', 'conversion', { send_to: `${conversionId}/${label}` });
        }
        catch {
            // Ignore tracking errors so public interactions remain unaffected.
        }
    };

    // Full-page reservation success (/dat-ban/thanh-cong).
    const trackReservationSuccessPage = () => {
        document.querySelectorAll('[data-reservation-page="success"] [data-tiktok-track-onload][data-tiktok-event="Schedule"]').forEach((element) => {
            if (!(element instanceof HTMLElement) || element.hasAttribute(trackedOnLoadAttribute)) {
                return;
            }

            element.setAttribute(trackedOnLoadAttribute, 'true');
            window.kigGadsConversion('reservation');
        });
    };

    // Messenger and Zalo buttons (floating contact FAB, footer, contact page).
    document.addEventListener('click', (event) => {
        if (!(event.target instanceof Element)) {
            return;
        }

        const messengerLink = event.target.closest('[data-tiktok-label="Messenger"], a[href*="m.me/"], a[href*="messenger.com"]');
        if (messengerLink instanceof HTMLElement) {
            window.kigGadsConversion('messenger');
            return;
        }

        const zaloLink = event.target.closest('[data-tiktok-label*="Zalo"], a[href*="zalo.me"]');
        if (zaloLink instanceof HTMLElement) {
            window.kigGadsConversion('zalo');
        }
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', trackReservationSuccessPage, { once: true });
    }
    else {
        trackReservationSuccessPage();
    }
})();
