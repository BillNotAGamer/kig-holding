(function () {
    // "Tải app" buttons: on phones, jump straight to the right store.
    // Desktop keeps the normal link (member page with QR code).
    const appStoreUrl = 'https://apps.apple.com/vn/app/truy%E1%BB%81n-thuy%E1%BA%BFt-champong-%EC%A0%88%EC%84%A4%EC%9D%98%EC%A7%AC%EB%BD%BB/id6753157313?l=vi';
    const googlePlayUrl = 'https://play.google.com/store/apps/details?id=com.eggstech.champong.app&pcampaignid=web_share';

    const detectStoreUrl = () => {
        const ua = navigator.userAgent || '';
        const isIos = /iPhone|iPad|iPod/i.test(ua) ||
            (/Macintosh/i.test(ua) && navigator.maxTouchPoints > 1);

        if (isIos) {
            return appStoreUrl;
        }

        if (/Android/i.test(ua)) {
            return googlePlayUrl;
        }

        return null;
    };

    document.addEventListener('click', (event) => {
        if (!(event.target instanceof Element)) {
            return;
        }

        const link = event.target.closest('[data-app-smart-link]');
        if (!(link instanceof HTMLElement)) {
            return;
        }

        const storeUrl = detectStoreUrl();
        if (!storeUrl) {
            return;
        }

        event.preventDefault();
        window.location.href = storeUrl;
    });
})();
