/* Hiệu ứng Giáng Sinh nhẹ: tuyết rơi. Chỉ chạy khi server render [data-noel-root].
   Tự gỡ toàn bộ (kể cả logo) nếu đã quá hạn (data-noel-end). */
(function () {
    'use strict';

    var root = document.querySelector('[data-noel-root]');
    if (!root) {
        return;
    }

    var body = document.body;

    function teardown() {
        document.querySelectorAll('img[data-noel-logo]').forEach(function (img) {
            var original = img.getAttribute('data-normal-src');
            var originalClass = img.getAttribute('data-normal-class');
            if (original) { img.setAttribute('src', original); }
            if (originalClass !== null) { img.setAttribute('class', originalClass); }
            img.removeAttribute('data-noel-logo');
        });
        body.classList.remove('theme-noel');
        root.parentNode && root.parentNode.removeChild(root);
        var greeting = document.querySelector('.noel-footer-greeting');
        greeting && greeting.parentNode.removeChild(greeting);
    }

    var endAt = Date.parse(root.getAttribute('data-noel-end') || '');
    if (!isNaN(endAt) && Date.now() >= endAt) {
        teardown();
        return;
    }

    var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var canvas = root.querySelector('[data-noel-snow]');
    var ctx = canvas && canvas.getContext ? canvas.getContext('2d') : null;
    if (!ctx || reduceMotion) {
        return;
    }

    var flakes = [];
    var width = 0;
    var height = 0;
    var lastTime = 0;

    // Sprite dựng sẵn: lõi trắng + viền xanh nhạt để thấy được cả trên nền sáng
    var sprite = (function () {
        var size = 32;
        var s = document.createElement('canvas');
        s.width = size;
        s.height = size;
        var c = s.getContext('2d');
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
    })();

    function makeFlake(randomY) {
        var depth = Math.random();
        return {
            x: Math.random() * width,
            y: randomY ? Math.random() * height : -12,
            r: 1.6 + depth * 3.4,
            vy: 18 + depth * 34,
            sway: 6 + Math.random() * 16,
            phase: Math.random() * Math.PI * 2,
            speed: 0.35 + Math.random() * 0.6,
            alpha: 0.5 + depth * 0.5
        };
    }

    function resize() {
        var dpr = Math.min(window.devicePixelRatio || 1, 2);
        width = window.innerWidth;
        height = window.innerHeight;
        canvas.width = Math.round(width * dpr);
        canvas.height = Math.round(height * dpr);
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        var target = Math.max(28, Math.min(Math.round(width / 24), 64));
        while (flakes.length < target) { flakes.push(makeFlake(true)); }
        if (flakes.length > target) { flakes.length = target; }
    }

    function tick(now) {
        window.requestAnimationFrame(tick);
        var dt = Math.min((now - lastTime) / 1000 || 0.016, 0.05);
        lastTime = now;
        ctx.clearRect(0, 0, width, height);

        for (var i = 0; i < flakes.length; i++) {
            var f = flakes[i];
            f.y += f.vy * dt;
            f.phase += f.speed * dt;
            if (f.y > height + 12) {
                flakes[i] = makeFlake(false);
                continue;
            }
            var d = f.r * 2.2;
            ctx.globalAlpha = f.alpha;
            ctx.drawImage(sprite, f.x + Math.sin(f.phase) * f.sway - d / 2, f.y - d / 2, d, d);
        }
        ctx.globalAlpha = 1;
    }

    var resizeTimer = 0;
    window.addEventListener('resize', function () {
        window.clearTimeout(resizeTimer);
        resizeTimer = window.setTimeout(resize, 150);
    });

    resize();
    lastTime = performance.now();
    window.requestAnimationFrame(tick);
})();
