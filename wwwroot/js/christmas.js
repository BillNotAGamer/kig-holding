/* Hiệu ứng Giáng Sinh: tuyết rơi nhẹ, xe tuyết ông già Noel, lời chúc, nút bật/tắt.
   Chỉ chạy khi server render [data-noel-root]. Tự gỡ nếu đã quá hạn (data-noel-end). */
(function () {
    'use strict';

    var root = document.querySelector('[data-noel-root]');
    if (!root) {
        return;
    }

    var body = document.body;
    var FX_KEY = 'kig-noel-fx';
    var TOAST_KEY = 'kig-noel-toast';

    function safeGet(storage, key) {
        try { return storage.getItem(key); } catch (e) { return null; }
    }
    function safeSet(storage, key, value) {
        try { storage.setItem(key, value); } catch (e) { /* bỏ qua */ }
    }

    /* ---- Hết mùa: gỡ toàn bộ, trả logo + giao diện về như cũ (phòng khi HTML bị cache) ---- */
    function teardown() {
        document.querySelectorAll('img[data-noel-logo]').forEach(function (img) {
            var original = img.getAttribute('data-normal-src');
            var originalClass = img.getAttribute('data-normal-class');
            if (original) { img.setAttribute('src', original); }
            if (originalClass !== null) { img.setAttribute('class', originalClass); }
            img.removeAttribute('data-noel-logo');
        });
        body.classList.remove('theme-noel', 'noel-fx-off');
        root.parentNode && root.parentNode.removeChild(root);
        var footerGreeting = document.querySelector('.noel-footer-greeting');
        footerGreeting && footerGreeting.parentNode.removeChild(footerGreeting);
    }

    var endAt = Date.parse(root.getAttribute('data-noel-end') || '');
    if (!isNaN(endAt) && Date.now() >= endAt) {
        teardown();
        return;
    }

    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    /* ---- Bật / tắt hiệu ứng ---- */
    var toggle = root.querySelector('[data-noel-toggle]');
    var fxOn = safeGet(window.localStorage, FX_KEY) !== 'off';

    function applyFx() {
        body.classList.toggle('noel-fx-off', !fxOn);
        if (toggle) {
            toggle.setAttribute('aria-pressed', fxOn ? 'true' : 'false');
            toggle.setAttribute('aria-label', fxOn ? 'Tắt hiệu ứng Giáng Sinh' : 'Bật hiệu ứng Giáng Sinh');
        }
        if (fxOn) { startSnow(); } else { stopSnow(); }
    }

    if (toggle) {
        toggle.addEventListener('click', function () {
            fxOn = !fxOn;
            safeSet(window.localStorage, FX_KEY, fxOn ? 'on' : 'off');
            applyFx();
        });
    }

    /* ---- Tuyết rơi nhẹ (canvas, sprite dựng sẵn cho nhẹ máy) ---- */
    var canvas = root.querySelector('[data-noel-snow]');
    var ctx = canvas && canvas.getContext ? canvas.getContext('2d') : null;
    var flakes = [];
    var rafId = 0;
    var width = 0;
    var height = 0;
    var dpr = 1;
    var sprite = null;
    var lastTime = 0;

    function buildSprite() {
        var size = 32;
        var s = document.createElement('canvas');
        s.width = size;
        s.height = size;
        var c = s.getContext('2d');
        // viền xanh nhạt để thấy được trên nền trắng/kem, lõi trắng sáng trên nền tối
        c.shadowColor = 'rgba(80, 130, 200, 0.85)';
        c.shadowBlur = 6;
        c.fillStyle = '#ffffff';
        c.beginPath();
        c.arc(size / 2, size / 2, 8, 0, Math.PI * 2);
        c.fill();
        c.shadowBlur = 0;
        c.lineWidth = 1.4;
        c.strokeStyle = 'rgba(120, 160, 215, 0.8)';
        c.stroke();
        return s;
    }

    function flakeCount() {
        var n = Math.round(width / 20);
        return Math.max(32, Math.min(n, 80));
    }

    function makeFlake(randomY) {
        var depth = Math.random();               // 0 = xa, 1 = gần
        return {
            x: Math.random() * width,
            y: randomY ? Math.random() * height : -12,
            r: 1.6 + depth * 3.4,                // bán kính hiển thị
            vy: 18 + depth * 34,                 // px / giây - rơi chậm
            sway: 6 + Math.random() * 16,        // biên độ lắc ngang
            phase: Math.random() * Math.PI * 2,
            speed: 0.35 + Math.random() * 0.6,   // tốc độ lắc
            alpha: 0.5 + depth * 0.5
        };
    }

    function resize() {
        if (!canvas) { return; }
        dpr = Math.min(window.devicePixelRatio || 1, 2);
        width = window.innerWidth;
        height = window.innerHeight;
        canvas.width = Math.round(width * dpr);
        canvas.height = Math.round(height * dpr);
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        var target = flakeCount();
        while (flakes.length < target) { flakes.push(makeFlake(true)); }
        if (flakes.length > target) { flakes.length = target; }
    }

    function tick(now) {
        rafId = window.requestAnimationFrame(tick);
        var dt = Math.min((now - lastTime) / 1000 || 0.016, 0.05);
        lastTime = now;
        ctx.clearRect(0, 0, width, height);

        for (var i = 0; i < flakes.length; i++) {
            var f = flakes[i];
            f.y += f.vy * dt;
            f.phase += f.speed * dt;
            var x = f.x + Math.sin(f.phase) * f.sway;
            if (f.y > height + 12) {
                flakes[i] = makeFlake(false);
                continue;
            }
            ctx.globalAlpha = f.alpha;
            var d = f.r * 2.2;
            ctx.drawImage(sprite, x - d / 2, f.y - d / 2, d, d);
        }
        ctx.globalAlpha = 1;
    }

    function startSnow() {
        if (!ctx || reduceMotion || rafId) { return; }
        if (!sprite) { sprite = buildSprite(); }
        resize();
        lastTime = performance.now();
        rafId = window.requestAnimationFrame(tick);
    }

    function stopSnow() {
        if (rafId) {
            window.cancelAnimationFrame(rafId);
            rafId = 0;
        }
        if (ctx) { ctx.clearRect(0, 0, width, height); }
    }

    var resizeTimer = 0;
    window.addEventListener('resize', function () {
        window.clearTimeout(resizeTimer);
        resizeTimer = window.setTimeout(function () { if (rafId) { resize(); } }, 150);
    });

    /* ---- Xe tuyết ông già Noel bay ngang màn hình định kỳ ---- */
    var santa = root.querySelector('[data-noel-santa]');

    function flySanta() {
        if (!santa || !fxOn || reduceMotion || document.hidden) { return; }
        santa.classList.remove('is-flying');
        void santa.offsetWidth; // restart animation
        santa.classList.add('is-flying');
    }

    if (santa) {
        santa.addEventListener('animationend', function (event) {
            if (event.target === santa) { santa.classList.remove('is-flying'); }
        });
        window.setTimeout(flySanta, 7000);
        window.setInterval(flySanta, 60000);
    }

    /* ---- Lời chúc (1 lần / phiên) ---- */
    var toast = root.querySelector('[data-noel-toast]');
    var toastClose = root.querySelector('[data-noel-toast-close]');

    function hideToast() {
        toast && toast.classList.remove('is-visible');
    }

    if (toast && safeGet(window.sessionStorage, TOAST_KEY) !== '1') {
        window.setTimeout(function () {
            if (!fxOn) { return; }
            toast.classList.add('is-visible');
            safeSet(window.sessionStorage, TOAST_KEY, '1');
            window.setTimeout(hideToast, 9000);
        }, 2200);
    }
    toastClose && toastClose.addEventListener('click', hideToast);

    applyFx();
})();
