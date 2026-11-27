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

    var COL = 3;                 // độ rộng 1 cột của đống tuyết (px)
    var flakes = [];
    var ledges = [];             // các "mép" tuyết có thể bám: nút, link trên header
    var clumps = [];             // cục tuyết vừa tách ra, đang rơi
    var puffs = [];              // mảnh vụn khi cục tuyết vỡ
    var width = 0;
    var height = 0;
    var lastTime = 0;
    var measureTimer = 0;

    // Chỗ tuyết có thể bám trên header (selector, lùi vào hai bên, hạ xuống, độ dày tối đa)
    var LEDGE_TARGETS = [
        { sel: '.site-header .nav-link', inset: 3, drop: 0, maxH: 3.2, text: true },
        { sel: '.site-header .header-app__button', inset: 15, drop: 0, maxH: 7 },
        { sel: '.site-header [data-booking-modal-trigger]', inset: 15, drop: 0, maxH: 7 }
    ];

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

    // Cục tuyết hình cầu có đổ bóng (sáng góc trên trái, xanh nhạt phía dưới) cho cảm giác 3D
    var ball = (function () {
        var size = 48;
        var s = document.createElement('canvas');
        s.width = size;
        s.height = size;
        var c = s.getContext('2d');
        var g = c.createRadialGradient(size * 0.38, size * 0.34, 2, size / 2, size / 2, size * 0.42);
        g.addColorStop(0, '#ffffff');
        g.addColorStop(0.5, '#f3f8ff');
        g.addColorStop(1, '#a9c1e2');
        c.shadowColor = 'rgba(30, 50, 90, 0.45)';
        c.shadowBlur = 4;
        c.fillStyle = g;
        c.beginPath();
        c.arc(size / 2, size / 2, size * 0.34, 0, Math.PI * 2);
        c.fill();
        return s;
    })();

    function makeFlake(randomY) {
        var depth = Math.random();
        return {
            x: Math.random() * width,
            y: randomY ? Math.random() * height : -12,
            py: 0,
            r: 1.6 + depth * 3.4,
            vy: 18 + depth * 34,
            sway: 6 + Math.random() * 16,
            phase: Math.random() * Math.PI * 2,
            speed: 0.35 + Math.random() * 0.6,
            alpha: 0.5 + depth * 0.5
        };
    }

    /* ---------- Tuyết bám trên header ---------- */
    function measureLedges() {
        var next = [];
        LEDGE_TARGETS.forEach(function (cfg) {
            document.querySelectorAll(cfg.sel).forEach(function (el) {
                var r = el.getBoundingClientRect();
                if (r.width < 8 || r.height < 8 || r.bottom < 0 || r.top > height) { return; }
                var x0 = r.left + cfg.inset;
                var x1 = r.right - cfg.inset;
                if (x1 - x0 < 10) { return; }
                var n = Math.ceil((x1 - x0) / COL);
                var ledge = null;
                for (var i = 0; i < ledges.length; i++) {
                    if (ledges[i].el === el) { ledge = ledges[i]; break; }
                }
                if (!ledge) {
                    ledge = { el: el, n: n, h: new Float32Array(n), maxH: cfg.maxH };
                    prefill(ledge);
                } else if (ledge.n !== n) {
                    // đổi kích thước chút ít (font tải xong...) thì giữ nguyên lượng tuyết đã có
                    var resized = new Float32Array(n);
                    for (var c = 0; c < n; c++) {
                        resized[c] = ledge.h[Math.min(ledge.n - 1, Math.round(c * ledge.n / n))];
                    }
                    ledge.h = resized;
                    ledge.n = n;
                }
                ledge.rect = { left: r.left, right: r.right, top: r.top, bottom: r.bottom };
                ledge.x0 = x0;
                ledge.x1 = x1;
                ledge.y = cfg.text ? textTopY(el, r) : r.top + cfg.drop;
                ledge.maxH = cfg.maxH;
                next.push(ledge);
            });
        });
        ledges = next;
    }

    // Y của đỉnh chữ hoa trong link (đo bằng vùng chữ thật, không phụ thuộc padding) để tuyết nằm trên chữ
    function textTopY(el, rect) {
        var fs = parseFloat(window.getComputedStyle(el).fontSize) || 15;
        try {
            var range = document.createRange();
            range.selectNodeContents(el);
            var tr = range.getBoundingClientRect();
            if (tr && tr.height > 4) { return tr.top + tr.height / 2 - fs * 0.36; }
        } catch (e) { /* dùng cách dự phòng bên dưới */ }
        return rect.top + rect.height / 2 - fs * 0.36;
    }

    // Mở trang là tuyết đã bám sẵn: lớp nền mỏng cong hai đầu + vài mô tuyết dày hơn
    function prefill(ledge) {
        var n = ledge.n;
        for (var i = 0; i < n; i++) {
            ledge.h[i] = ledge.maxH * 0.42 * Math.pow(Math.sin(Math.PI * (i + 0.5) / n), 0.6);
        }
        var bumps = Math.max(2, Math.round(n / 7));
        for (var b = 0; b < bumps; b++) {
            addSnow(ledge, (Math.random() * n) | 0, ledge.maxH * (0.35 + Math.random() * 0.4));
        }
        for (var j = 0; j < n; j++) { ledge.h[j] = Math.min(ledge.h[j], ledge.maxH * 0.88); }
    }

    var KERNEL = [0.35, 0.75, 1, 0.75, 0.35];

    function addSnow(ledge, col, amount) {
        for (var k = -2; k <= 2; k++) {
            var i = col + k;
            if (i < 0 || i >= ledge.n) { continue; }
            ledge.h[i] = Math.min(ledge.maxH, ledge.h[i] + amount * KERNEL[k + 2]);
        }
    }

    // Một mảng tuyết tách khỏi đống và rơi xuống
    function shed(ledge, col) {
        var lo = Math.max(0, col - 2);
        var hi = Math.min(ledge.n - 1, col + 2);
        var volume = 0;
        for (var i = lo; i <= hi; i++) {
            var take = ledge.h[i] * 0.65;
            volume += take;
            ledge.h[i] -= take;
        }
        if (volume < 0.5) { return; }
        var y = ledge.y - ledge.h[col];
        clumps.push({
            x: ledge.x0 + (col + 0.5) * COL,
            y: y,
            startY: y,
            vx: (Math.random() - 0.5) * 14,
            vy: 6,
            r: Math.min(1.7 + volume * 0.45, 4.2),
            fall: 45 + Math.random() * 40
        });
    }

    // Bông tuyết chạm vào mép: bám lại, hoặc làm đống tuyết đã đầy tách ra
    function landed(ledge, col) {
        var fullness = ledge.h[col] / ledge.maxH;
        if (fullness > 0.55 && Math.random() < 0.55) {
            shed(ledge, col);
            return true;
        }
        if (Math.random() < 0.7) {
            addSnow(ledge, col, 1.4);
            return true;
        }
        return false;
    }

    // Lia chuột qua đống tuyết: tuyết bị gạt đi, văng ra thành cục + bụi tuyết
    var brush = null;

    function applyBrush(b) {
        for (var li = 0; li < ledges.length; li++) {
            var L = ledges[li];
            var r = L.rect;
            if (!r || b.x < r.left - 4 || b.x > r.right + 4 || b.y < r.top - 18 || b.y > r.bottom + 4) { continue; }

            var radius = 14 + Math.min(20, Math.abs(b.dx) * 1.4);
            var lo = Math.max(0, Math.floor((b.x - radius - L.x0) / COL));
            var hi = Math.min(L.n - 1, Math.ceil((b.x + radius - L.x0) / COL));
            var removed = 0;
            for (var i = lo; i <= hi; i++) {
                var near = 1 - Math.abs(L.x0 + (i + 0.5) * COL - b.x) / radius;
                if (near <= 0) { continue; }
                var take = L.h[i] * Math.min(0.85, 0.3 + 0.55 * near);
                L.h[i] -= take;
                removed += take;
            }
            if (removed < 0.35) { continue; }

            var lumps = Math.min(3, 1 + ((removed / 2.5) | 0));
            for (var k = 0; k < lumps; k++) {
                var cy = L.y - L.maxH * 0.6;
                clumps.push({
                    x: b.x + (Math.random() - 0.5) * radius,
                    y: cy,
                    startY: cy,
                    vx: Math.max(-110, Math.min(110, b.dx * 7 * (0.4 + Math.random()))) + (Math.random() - 0.5) * 30,
                    vy: -35 - Math.random() * 45,
                    r: 1.5 + Math.random() * 1.8,
                    fall: 40 + Math.random() * 45
                });
            }
            var dust = Math.min(9, 3 + ((removed * 2) | 0));
            for (var d = 0; d < dust; d++) {
                puffs.push({
                    x: b.x + (Math.random() - 0.5) * radius,
                    y: L.y - 2,
                    vx: b.dx * 5 + (Math.random() - 0.5) * 70,
                    vy: -25 - Math.random() * 45,
                    r: 0.7 + Math.random() * 1.2,
                    a: 0.95
                });
            }
        }
    }

    window.addEventListener('pointermove', function (event) {
        if (event.pointerType === 'touch') { return; }
        var last = brush || { x: event.clientX, y: event.clientY, dx: 0 };
        brush = { x: event.clientX, y: event.clientY, dx: event.clientX - last.x, moved: true };
    }, { passive: true });

    function updateSnowPhysics(dt) {
        if (brush && brush.moved) {
            applyBrush(brush);
            brush.moved = false;
            brush.dx = 0;
        }

        // tự rơi nhẹ khi đống tuyết đã dày
        for (var li = 0; li < ledges.length; li++) {
            var L = ledges[li];
            if (Math.random() < dt * 0.12) {
                var best = 0;
                for (var c = 1; c < L.n; c++) {
                    if (L.h[c] > L.h[best]) { best = c; }
                }
                if (L.h[best] >= L.maxH * 0.9) { shed(L, best); }
            }
        }

        for (var i = clumps.length - 1; i >= 0; i--) {
            var k = clumps[i];
            k.vy += 230 * dt;
            k.y += k.vy * dt;
            k.x += k.vx * dt;
            if (k.y - k.startY > k.fall || k.y > height + 10) {
                // vỡ thành vài mảnh nhỏ rồi tan
                if (k.y <= height) {
                    for (var p = 0; p < 4; p++) {
                        puffs.push({
                            x: k.x, y: k.y,
                            vx: (Math.random() - 0.5) * 46,
                            vy: k.vy * 0.35 + Math.random() * 20,
                            r: k.r * 0.5,
                            a: 0.9
                        });
                    }
                }
                clumps.splice(i, 1);
            }
        }

        for (var j = puffs.length - 1; j >= 0; j--) {
            var q = puffs[j];
            q.vy += 60 * dt;
            q.x += q.vx * dt;
            q.y += q.vy * dt;
            q.a -= dt * 1.2;
            if (q.a <= 0) { puffs.splice(j, 1); }
        }
    }

    function drawLedge(L) {
        var max = 0;
        for (var i = 0; i < L.n; i++) { if (L.h[i] > max) { max = L.h[i]; } }
        if (max < 0.35) { return; }

        var grad = ctx.createLinearGradient(0, L.y - Math.max(max, 1.5), 0, L.y);
        grad.addColorStop(0, '#ffffff');
        grad.addColorStop(0.55, '#e9f1fb');
        grad.addColorStop(1, '#b4c9e4');
        ctx.fillStyle = grad;
        ctx.shadowColor = 'rgba(25, 40, 80, 0.45)';
        ctx.shadowBlur = 2.5;
        ctx.shadowOffsetY = 1.5;

        // chỉ vẽ những đoạn còn tuyết, mỗi đoạn là một mô tuyết thoải hai đầu
        var c = 0;
        while (c < L.n) {
            if (L.h[c] < 0.35) { c++; continue; }
            var start = c;
            while (c < L.n && L.h[c] >= 0.35) { c++; }
            var end = c - 1;
            var x0 = L.x0 + start * COL;
            ctx.beginPath();
            ctx.moveTo(x0, L.y);
            var px = x0;
            var py = L.y - L.h[start] * 0.5;
            ctx.lineTo(px, py);
            for (var k = start; k <= end; k++) {
                var x = L.x0 + (k + 0.5) * COL;
                var y = L.y - L.h[k];
                ctx.quadraticCurveTo(px, py, (px + x) / 2, (py + y) / 2);
                px = x;
                py = y;
            }
            var x1 = L.x0 + (end + 1) * COL;
            ctx.quadraticCurveTo(px, py, x1, L.y - L.h[end] * 0.5);
            ctx.lineTo(x1, L.y);
            ctx.closePath();
            ctx.fill();
        }
        ctx.shadowBlur = 0;
        ctx.shadowOffsetY = 0;
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
        measureLedges();
    }

    function tick(now) {
        window.requestAnimationFrame(tick);
        var dt = Math.min((now - lastTime) / 1000 || 0.016, 0.05);
        lastTime = now;

        if (now - measureTimer > 600) {
            measureTimer = now;
            measureLedges();
        }

        updateSnowPhysics(dt);
        ctx.clearRect(0, 0, width, height);

        for (var li = 0; li < ledges.length; li++) { drawLedge(ledges[li]); }

        for (var i = 0; i < flakes.length; i++) {
            var f = flakes[i];
            f.py = f.y;
            f.y += f.vy * dt;
            f.phase += f.speed * dt;
            var x = f.x + Math.sin(f.phase) * f.sway;

            var consumed = false;
            for (var l = 0; l < ledges.length && !consumed; l++) {
                var L = ledges[l];
                if (x < L.x0 || x >= L.x1) { continue; }
                var col = ((x - L.x0) / COL) | 0;
                var top = L.y - L.h[col];
                if (f.py < top && f.y >= top) { consumed = landed(L, col); }
            }

            if (consumed || f.y > height + 12) {
                flakes[i] = makeFlake(false);
                continue;
            }
            var d = f.r * 2.2;
            ctx.globalAlpha = f.alpha;
            ctx.drawImage(sprite, x - d / 2, f.y - d / 2, d, d);
        }

        for (var a = 0; a < clumps.length; a++) {
            var k = clumps[a];
            var kd = k.r * 3.2 * (1 + Math.max(0, k.y - k.startY) / 140);
            ctx.globalAlpha = 0.16;
            ctx.drawImage(ball, k.x - k.vx * 0.06 - kd / 2, k.y - k.vy * 0.06 - kd / 2, kd, kd);
            ctx.globalAlpha = 0.32;
            ctx.drawImage(ball, k.x - k.vx * 0.03 - kd / 2, k.y - k.vy * 0.03 - kd / 2, kd, kd);
            ctx.globalAlpha = 1;
            ctx.drawImage(ball, k.x - kd / 2, k.y - kd / 2, kd, kd);
        }
        for (var b = 0; b < puffs.length; b++) {
            var q = puffs[b];
            var qd = q.r * 2.4;
            ctx.globalAlpha = Math.max(0, q.a);
            ctx.drawImage(sprite, q.x - qd / 2, q.y - qd / 2, qd, qd);
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
