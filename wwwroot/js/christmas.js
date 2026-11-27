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
        document.querySelectorAll('.noel-mascot-overlay').forEach(function (o) { o.parentNode && o.parentNode.removeChild(o); });
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

    /* ---------- Linh vật hổ: đội mũ Noel + chớp mắt nhẹ ---------- */
    var HAT_SVG =
        '<svg viewBox="0 0 1000 1000" width="100%" height="100%" style="overflow:visible" xmlns="http://www.w3.org/2000/svg">' +
        '<defs>' +
        '<linearGradient id="noelHatRed" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#e5222f"/><stop offset="1" stop-color="#a10c16"/></linearGradient>' +
        '<linearGradient id="noelFur" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffffff"/><stop offset="1" stop-color="#dfe8f3"/></linearGradient>' +
        '<radialGradient id="noelLid" cx="50%" cy="40%" r="65%"><stop offset="0" stop-color="#fbe0cf"/><stop offset="1" stop-color="#f0c4ac"/></radialGradient>' +
        '</defs>' +
        '<g class="noel-lid noel-lid--l"><ellipse cx="414" cy="500" rx="62" ry="72" transform="rotate(-10 414 500)" fill="url(#noelLid)"/>' +
        '<path d="M356 506 Q414 548 474 496" fill="none" stroke="#2a1510" stroke-width="9" stroke-linecap="round" transform="rotate(-10 414 500)"/></g>' +
        '<g class="noel-lid noel-lid--r"><ellipse cx="636" cy="454" rx="64" ry="66" transform="rotate(-10 636 454)" fill="url(#noelLid)"/>' +
        '<path d="M574 458 Q636 498 698 450" fill="none" stroke="#2a1510" stroke-width="9" stroke-linecap="round" transform="rotate(-10 636 454)"/></g>' +
        '<g class="noel-hat" transform="rotate(-7 520 160)">' +
        '<path d="M350 170 C352 86 430 24 530 22 C612 20 676 62 704 132 L700 176 Z" fill="url(#noelHatRed)"/>' +
        '<path d="M520 30 C600 26 664 70 692 140" fill="none" stroke="#ff6b73" stroke-opacity=".35" stroke-width="10" stroke-linecap="round"/>' +
        '<rect x="326" y="140" width="392" height="66" rx="33" fill="url(#noelFur)"/>' +
        '<rect x="326" y="190" width="392" height="16" rx="8" fill="#b9c8dc" opacity=".35"/>' +
        '<g class="noel-pom"><circle cx="716" cy="150" r="36" fill="url(#noelFur)"/><circle cx="706" cy="140" r="12" fill="#fff" opacity=".7"/></g>' +
        '</g></svg>';

    function dressMascot() {
        var img = document.querySelector('img.champong-takehome__mascot');
        if (!img || !img.parentNode) { return; }
        var host = img.parentNode;
        var overlay = document.createElement('div');
        overlay.className = 'noel-mascot-overlay';
        overlay.setAttribute('aria-hidden', 'true');
        overlay.innerHTML = HAT_SVG;
        host.appendChild(overlay);

        function place() {
            // ảnh dùng object-fit: contain → hình thực là hình vuông căn giữa trong khung ảnh
            var w = img.clientWidth;
            var h = img.clientHeight;
            var side = Math.min(w, h);
            overlay.style.left = (img.offsetLeft + (w - side) / 2) + 'px';
            overlay.style.top = (img.offsetTop + (h - side) / 2) + 'px';
            overlay.style.width = side + 'px';
            overlay.style.height = side + 'px';
            overlay.style.display = side > 40 ? '' : 'none';
        }
        place();
        img.addEventListener('load', place);
        window.addEventListener('resize', place);
        if (typeof ResizeObserver === 'function') { new ResizeObserver(place).observe(img); }
    }
    dressMascot();

    /* ---------- Ông già Noel đu dây leo lên (trang chủ) ---------- */
    var SANTA_SVG =
        '<svg viewBox="0 0 68 100" width="68" height="100" xmlns="http://www.w3.org/2000/svg">' +
        '<g class="noel-santa-sack"><circle cx="52" cy="66" r="11" fill="#8a5a33"/><path d="M46 57 Q52 52 58 57" fill="none" stroke="#5a3519" stroke-width="2.4" stroke-linecap="round"/></g>' +
        '<g data-leg="l"><rect x="24" y="62" width="9" height="19" rx="4" fill="#a50d17"/><rect x="22.5" y="77" width="12" height="10" rx="3.5" fill="#1b1b1b"/></g>' +
        '<g data-leg="r"><rect x="35" y="62" width="9" height="19" rx="4" fill="#a50d17"/><rect x="33.5" y="77" width="12" height="10" rx="3.5" fill="#1b1b1b"/></g>' +
        '<rect x="18" y="34" width="32" height="33" rx="11" fill="#d4121f"/>' +
        '<rect x="18" y="55" width="32" height="6" fill="#1b1b1b"/><rect x="30.5" y="54.5" width="7" height="7" rx="1.5" fill="#f2c230"/>' +
        '<line data-arm="l" x1="23" y1="40" x2="31" y2="24" stroke="#d4121f" stroke-width="7" stroke-linecap="round"/>' +
        '<line data-arm="r" x1="45" y1="40" x2="37" y2="24" stroke="#d4121f" stroke-width="7" stroke-linecap="round"/>' +
        '<circle data-hand="l" cx="31" cy="24" r="4.2" fill="#fff"/><circle data-hand="r" cx="37" cy="24" r="4.2" fill="#fff"/>' +
        '<circle cx="34" cy="27" r="11" fill="#f6d2b8"/>' +
        '<ellipse cx="34" cy="35" rx="11.5" ry="8.5" fill="#fff"/>' +
        '<ellipse cx="29.5" cy="31" rx="4.6" ry="2.8" fill="#fff"/><ellipse cx="38.5" cy="31" rx="4.6" ry="2.8" fill="#fff"/>' +
        '<circle cx="34" cy="29" r="2.3" fill="#e8998a"/>' +
        '<circle cx="29.5" cy="25" r="1.4" fill="#222" class="noel-santa-eye"/><circle cx="38.5" cy="25" r="1.4" fill="#222" class="noel-santa-eye"/>' +
        '<circle cx="26" cy="29" r="2.2" fill="#f2a99a" opacity=".7"/><circle cx="42" cy="29" r="2.2" fill="#f2a99a" opacity=".7"/>' +
        '<path d="M22 21 Q25 4 40 3 Q53 3 57 17 Q49 13 46 21 Z" fill="#d4121f"/>' +
        '<rect x="20.5" y="18" width="27" height="7.5" rx="3.75" fill="#fff"/><circle cx="57" cy="17" r="4.8" fill="#fff"/>' +
        '</svg>';

    // Xe trượt: dùng đúng hình gốc. Ông già Noel được tách riêng khỏi hình để tuột dây xuống.
    var SW = 190;
    var SH = Math.round(190 * 249 / 273);
    var BUST_LEFT = 35.5;        // vị trí ông trên xe (px, theo hình đã co)
    var BUST_TOP = 0.7;
    var HAND_X = 74;             // bàn tay nắm dây (trong ảnh ông)
    var HAND_Y = 65;

    var LEGS_SVG =
        '<svg class="noel-santa-legs" viewBox="0 0 78 46" width="78" height="46" xmlns="http://www.w3.org/2000/svg">' +
        '<g data-leg="l"><rect x="22" y="0" width="14" height="30" rx="6" fill="#c41f1f"/><rect x="19.5" y="26" width="19" height="12" rx="5" fill="#1b1b1b"/></g>' +
        '<g data-leg="r"><rect x="40" y="0" width="14" height="30" rx="6" fill="#b01818"/><rect x="37.5" y="26" width="19" height="12" rx="5" fill="#1b1b1b"/></g>' +
        '</svg>';

    function startSanta() {
        if (reduceMotion || !/^\/?(home(\/index)?)?$/i.test(location.pathname.replace(/\/+$/, '') || '/')) { return; }
        var clip = document.createElement('div');
        clip.className = 'noel-climber';
        clip.setAttribute('aria-hidden', 'true');
        clip.innerHTML =
            '<div class="noel-deer">' +
            '<img class="noel-sleigh noel-sleigh--full" src="/images/seasonal/santa-sleigh.webp" alt="" width="' + SW + '" height="' + SH + '" decoding="async">' +
            '<img class="noel-sleigh noel-sleigh--empty" src="/images/seasonal/santa-sleigh-empty.webp" alt="" width="' + SW + '" height="' + SH + '" decoding="async">' +
            '</div>' +
            '<div class="noel-climber__pivot"><div class="noel-climber__rope"></div>' +
            '<div class="noel-climber__santa">' + LEGS_SVG + '<img class="noel-santa-bust" src="/images/seasonal/santa-bust.webp" alt="" width="78" height="86" decoding="async"></div></div>';
        root.appendChild(clip);

        var deer = clip.firstChild;
        var full = deer.firstChild;
        var empty = deer.lastChild;
        var pivot = clip.lastChild;
        var rope = pivot.firstChild;
        var santa = pivot.lastChild;
        var legL = santa.querySelector('[data-leg="l"]');
        var legR = santa.querySelector('[data-leg="r"]');

        var SX = BUST_LEFT + HAND_X;     // điểm buộc dây = bàn tay lúc ngồi trên xe
        var SY = BUST_TOP + HAND_Y;
        var DEER_TOP = 4;                // xe chạy sát mép dưới header
        var K_HANG = 1.35;
        var Y_RIDE = BUST_TOP - SY;      // đỉnh người so với gốc dây khi ngồi trên xe
        var HALF = 0.5;                  // giây / nhịp leo
        var STEP = 22;
        var phase = 'wait';
        var phaseStart = performance.now();
        var waitFor = 1500;
        var deerX = -SW - 40;
        var Y = Y_RIDE;
        var hidden = false;

        function ease(t) { return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2; }

        function legs(flip, kick) {
            legL.setAttribute('transform', 'rotate(' + (flip * kick) + ' 29 2)');
            legR.setAttribute('transform', 'rotate(' + (-flip * kick) + ' 47 2)');
        }

        function frame(now) {
            if (!root.isConnected) { return; }
            window.requestAnimationFrame(frame);

            var header = document.querySelector('.site-header');
            var top = header ? Math.max(0, header.getBoundingClientRect().bottom) : 0;
            var vw = window.innerWidth;
            var show = vw >= 760 && (window.pageYOffset || 0) < 260;
            if (show === hidden) {
                hidden = !show;
                clip.style.opacity = show ? '1' : '0';
            }
            if (!show) { return; }

            var ropeLen = Math.max(190, Math.min(250, window.innerHeight * 0.36));
            var yHang = ropeLen - 62 - HAND_Y;            // đỉnh người khi treo lơ lửng
            var xDrop = Math.min(vw - 150, vw * 0.88);
            var doRope = vw >= 1100;
            var xStand = xDrop - SX;
            var pivotTop = DEER_TOP + SY;
            clip.style.top = top + 'px';
            clip.style.height = (pivotTop + ropeLen + 120) + 'px';

            var t = now - phaseStart;
            var ang = 0;
            var flip = 1;
            var kick = 0;
            var riding = true;
            var running = false;
            var tilt = 0;

            if (phase === 'wait') {
                deerX = -SW - 40;
                Y = Y_RIDE;
                if (t > waitFor) { phase = 'enter'; phaseStart = now; }
            } else if (phase === 'enter') {
                var d1 = Math.min(1, t / 11000);
                deerX = -SW - 40 + (xStand + SW + 40) * (1 - Math.pow(1 - d1, 2));
                running = true;
                if (d1 >= 1) { phase = doRope ? 'lower' : 'leave'; phaseStart = now; }
            } else if (phase === 'lower') {
                var d2 = Math.min(1, t / 1700);
                Y = Y_RIDE + (yHang - Y_RIDE) * (1 - Math.pow(1 - d2, 3));
                ang = 7 * d2 * Math.sin(d2 * 7);
                kick = 16 * Math.sin(t / 120) * (1 - d2);
                riding = false;
                if (d2 >= 1) { phase = 'swing'; phaseStart = now; }
            } else if (phase === 'swing') {
                var s3 = Math.min(1, t / 2600);
                Y = yHang;
                ang = 9 * (1 - 0.5 * s3) * Math.sin(t / 1000 * 2.2 + 0.8);
                kick = 22 * Math.sin(t / 1000 * 4.4);
                riding = false;
                if (s3 >= 1) { phase = 'climb'; phaseStart = now; }
            } else if (phase === 'climb') {
                var k = t / 1000 / HALF;
                var n = Math.floor(k);
                var f = k - n;
                var e = ease(f);
                Y = yHang - (n + e) * STEP;
                flip = n % 2 === 0 ? 1 : -1;
                kick = 26 * Math.sin(Math.PI * f);
                tilt = 3.5 * Math.sin(Math.PI * (n + e));
                ang = 2.5 * Math.sin(t / 1000 * 2.4) * Math.max(0.2, 1 - t / 4000);
                riding = false;
                if (Y <= Y_RIDE) { Y = Y_RIDE; phase = 'leave'; phaseStart = now; riding = true; }
            } else if (phase === 'leave') {
                var d4 = Math.min(1, t / 9000);
                deerX = xStand + (vw + 60 - xStand) * d4 * d4;
                Y = Y_RIDE;
                running = true;
                if (d4 >= 1) {
                    phase = 'wait';
                    phaseStart = now;
                    waitFor = 2500;
                }
            }

            // xe trượt nhấp nhô nhẹ khi chạy
            var bob = running ? 2.5 * Math.sin(now / 420) : 0;
            deer.style.transform = 'translate(' + deerX.toFixed(1) + 'px,' + (DEER_TOP + bob).toFixed(1) + 'px)';
            full.style.visibility = riding ? 'visible' : 'hidden';
            empty.style.visibility = riding ? 'hidden' : 'visible';

            // ông già Noel (hình gốc): to dần khi tuột xuống, nhỏ lại khi leo về xe
            var ratio = Math.max(0, Math.min(1, (Y - Y_RIDE) / (yHang - Y_RIDE)));
            var kNow = 1 + (K_HANG - 1) * ratio;
            legs(flip, kick);
            pivot.style.left = (deerX + SX).toFixed(1) + 'px';
            pivot.style.top = (pivotTop + (riding ? bob : 0)).toFixed(1) + 'px';
            pivot.style.transform = 'rotate(' + ang.toFixed(2) + 'deg)';
            var ropeH = Y + HAND_Y + 20;
            rope.style.height = Math.max(0, Math.min(ropeLen + 24, ropeH)).toFixed(1) + 'px';
            rope.style.opacity = riding ? '0' : '1';
            santa.style.visibility = riding ? 'hidden' : 'visible';
            santa.style.transform = 'translate(' + (-HAND_X) + 'px,' + Y.toFixed(1) + 'px) rotate(' + tilt.toFixed(2) + 'deg) scale(' + kNow.toFixed(3) + ')';
        }
        window.requestAnimationFrame(frame);
    }
    startSanta();

    /* ---------- Ông già Noel leo lên chữ "TINH HOA ẨM THỰC" rồi vẫy tay chào ---------- */
    function startHeadingSanta() {
        if (reduceMotion) { return; }
        var heading = null;
        document.querySelectorAll('h1, h2').forEach(function (h) {
            if (!heading && /tinh hoa/i.test(h.textContent || '')) { heading = h; }
        });
        if (!heading) { return; }

        var box = document.createElement('div');
        box.className = 'noel-heading-santa';
        box.setAttribute('aria-hidden', 'true');
        box.innerHTML = SANTA_SVG;
        root.appendChild(box);

        var armL = box.querySelector('[data-arm="l"]');
        var armR = box.querySelector('[data-arm="r"]');
        var handL = box.querySelector('[data-hand="l"]');
        var handR = box.querySelector('[data-hand="r"]');
        var legL = box.querySelector('[data-leg="l"]');
        var legR = box.querySelector('[data-leg="r"]');

        var K = 1.25;
        var STEP = 18;
        var HALF = 0.36;
        var phase = 'wait';
        var phaseStart = performance.now();
        var waitFor = 2500;
        var wasVisible = false;

        function ease(t) { return t < 0.5 ? 2 * t * t : 1 - Math.pow(-2 * t + 2, 2) / 2; }
        function setArm(arm, hand, x, y) {
            arm.setAttribute('x2', x); arm.setAttribute('y2', y);
            hand.setAttribute('cx', x); hand.setAttribute('cy', y);
        }

        function frame(now) {
            if (!root.isConnected) { return; }
            window.requestAnimationFrame(frame);

            var r = heading.getBoundingClientRect();
            var vis = window.innerWidth >= 760 && r.bottom > 120 && r.top < window.innerHeight - 60;
            if (!vis) {
                box.style.opacity = '0';
                wasVisible = false;
                phase = 'wait';
                phaseStart = now;
                waitFor = 1200;
                return;
            }
            if (!wasVisible) { wasVisible = true; phaseStart = now; }

            // mép trái chữ & đỉnh dòng 1
            var textLeft = r.left;
            var capTop = r.top;
            try {
                var range = document.createRange();
                range.selectNodeContents(heading);
                var rects = range.getClientRects();
                if (rects.length) {
                    var fs = parseFloat(window.getComputedStyle(heading).fontSize) || 60;
                    var first = rects[0];
                    textLeft = first.left;
                    capTop = first.top + first.height / 2 - fs * 0.36;
                    var lastBottom = 0;
                    for (var i = 0; i < rects.length; i++) { lastBottom = Math.max(lastBottom, rects[i].bottom); }
                    r = { top: capTop, bottom: lastBottom, left: textLeft };
                }
            } catch (e) { /* dùng khung của tiêu đề */ }

            var gripX = textLeft - 6;
            var yTop = capTop - 6 * K;                  // tay bám ở đỉnh chữ
            var yBottom = r.bottom - 6 * K;             // bắt đầu leo từ chân dòng cuối
            var yStand = capTop - 86 * K;               // đứng trên đỉnh chữ
            var t = now - phaseStart;
            var Y = yBottom + 60;
            var op = 0;
            var kickL = 0;
            var grip = true, hy = 6, flip = 1, kick = 0;
            var wave = false, standing = false;
            var xf = 0;

            if (phase === 'wait') {
                if (t > waitFor) { phase = 'climb'; phaseStart = now; }
            } else if (phase === 'climb') {
                var k = t / 1000 / HALF;
                var n = Math.floor(k), f = k - n, e = ease(f);
                grip = n % 2 === 0;
                hy = 6 + STEP * e;
                Y = yBottom - (n + e) * STEP * K;
                flip = grip ? 1 : -1;
                kick = 24 * Math.sin(Math.PI * f);
                op = Math.min(1, t / 300);
                if (Y <= yTop) { Y = yTop; phase = 'mount'; phaseStart = now; }
            } else if (phase === 'mount') {
                var m = Math.min(1, t / 700);
                Y = yTop + (yStand - yTop) * ease(m);
                op = 1;
                kick = 18 * Math.sin(m * Math.PI);
                standing = m > 0.7;
                xf = ease(m);
                if (m >= 1) { phase = 'wave'; phaseStart = now; }
            } else if (phase === 'wave') {
                Y = yStand;
                op = 1;
                standing = true;
                wave = true;
                xf = 1;
                if (t > 4800) { phase = 'leave'; phaseStart = now; }
            } else if (phase === 'leave') {
                var l = Math.min(1, t / 700);
                Y = yStand + (yTop - yStand) * ease(l);
                op = 1 - Math.max(0, (l - 0.6) / 0.4);
                standing = false;
                xf = 1 - ease(l);
                if (l >= 1) { phase = 'wait'; phaseStart = now; waitFor = 14000; op = 0; }
            }

            // tư thế
            if (phase === 'climb') {
                var hl = grip ? hy : 30 - hy, hr = grip ? 30 - hy : hy;
                setArm(armL, handL, 31, hl); setArm(armR, handR, 37, hr);
                legL.setAttribute('transform', 'rotate(' + flip * kick + ' 28.5 62)');
                legR.setAttribute('transform', 'rotate(' + (-flip * kick) + ' 39.5 62)');
            } else if (wave) {
                var w = Math.sin(t / 1000 * 7);
                setArm(armL, handL, 17, 54);                                  // tay trái buông
                setArm(armR, handR, 56 + 3 * w, 12 + 2 * Math.cos(t / 1000 * 7)); // tay phải vẫy
                legL.setAttribute('transform', 'rotate(0 28.5 62)');
                legR.setAttribute('transform', 'rotate(0 39.5 62)');
            } else if (standing) {
                setArm(armL, handL, 17, 54); setArm(armR, handR, 51, 52);
            } else {
                setArm(armL, handL, 31, 6); setArm(armR, handR, 37, 20);
                legL.setAttribute('transform', 'rotate(' + kick + ' 28.5 62)');
                legR.setAttribute('transform', 'rotate(' + (-kick) + ' 39.5 62)');
            }

            var sway = wave ? 2.5 * Math.sin(t / 1000 * 3.5) : 0;
            box.style.opacity = op.toFixed(2);
            box.style.transform = 'translate(' + (gripX + 90 * xf - 34 * K).toFixed(1) + 'px,' + Y.toFixed(1) + 'px) rotate(' + sway.toFixed(2) + 'deg) scale(' + K + ')';
        }
        window.requestAnimationFrame(frame);
    }
    startHeadingSanta();
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

    // Những chỗ tuyết có thể bám. text = bám lên đỉnh chữ; còn lại bám lên mép trên của khối.
    var TARGETS = [
        { sel: '.site-header .header-app__button, .site-header [data-booking-modal-trigger]', inset: 15, maxH: 7 },
        { sel: 'button, a[class*="btn"], a[class*="button"], [role="button"], .rounded-button, input[type="submit"]', inset: 0.14, maxH: 6, page: true },
        { sel: 'footer.relative', inset: 0, maxH: 24, page: true, thick: true }
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

    /* ---------- Tuyết bám lên nút, thẻ, tiêu đề, menu ---------- */
    var store = typeof WeakMap === 'function' ? new WeakMap() : null;   // el -> tuyết đang bám (giữ khi cuộn đi rồi quay lại)
    var candidates = [];
    var geometryDirty = true;
    var lastRefreshY = 0;
    var needRefresh = false;

    function isUsable(el, cfg) {
        if (el.closest('[data-noel-root]')) { return false; }
        if (cfg.page && el.closest('.site-header, .mobile-drawer, [role="dialog"], [aria-hidden="true"], [role="group"], [role="tablist"], [class*="filter"], [class*="chip"]')) { return false; }
        if (cfg.page && /chip|filter|\bh-8\b|\bh-9\b|tab/i.test(el.className && el.className.baseVal === undefined ? el.className : '')) { return false; }
        if (typeof el.checkVisibility === 'function') {
            try {
                if (!el.checkVisibility({ opacityProperty: true, visibilityProperty: true })) { return false; }
            } catch (e) { /* trình duyệt cũ: bỏ qua */ }
        }
        return true;
    }

    function refreshCandidates() {
        var seen = [];
        var list = [];
        TARGETS.forEach(function (cfg) {
            document.querySelectorAll(cfg.sel).forEach(function (el) {
                if (seen.indexOf(el) !== -1) { return; }
                var near = el.getBoundingClientRect();   // chỉ xét phần tử gần màn hình hiện tại
                if (near.bottom < -250 || near.top > height + 250) { return; }
                if (!isUsable(el, cfg)) { return; }
                seen.push(el);
                list.push({ el: el, cfg: cfg });
            });
        });
        candidates = list.slice(0, 260);
        lastRefreshY = window.pageYOffset || 0;
    }

    function measureLedges(full) {
        if (full) { refreshCandidates(); }
        var header = document.querySelector('.site-header');
        var headerBottom = header ? header.getBoundingClientRect().bottom : 0;
        var next = [];

        for (var ci = 0; ci < candidates.length; ci++) {
            var cfg = candidates[ci].cfg;
            var el = candidates[ci].el;
            var r = el.getBoundingClientRect();
            if (cfg.page && !cfg.thick && (r.width > 340 || r.height > 80)) { continue; }   // chỉ nút nhỏ, không phải hàng/tab/thẻ
            if (r.width < 40 || r.height < 12 || r.bottom < 0 || r.top > height || r.right < 0 || r.left > width) { continue; }

            var inset = cfg.inset < 1 ? Math.min(18, r.width * cfg.inset) : cfg.inset;
            var x0 = Math.max(r.left + inset, 0);
            var x1 = Math.min(r.right - inset, width);
            if (x1 - x0 < 24) { continue; }

            var y = cfg.text ? textTopY(el, r) : r.top;
            if (cfg.page && y < headerBottom + 2) { continue; }   // đang nằm dưới header thì không vẽ

            var n = Math.ceil((x1 - x0) / COL);
            var ledge = store ? store.get(el) : null;
            if (!ledge) {
                ledge = { el: el, n: n, h: new Float32Array(n), maxH: cfg.maxH };
                prefill(ledge, cfg.thick ? 1.1 : cfg.page ? 0.9 : 1);
                if (store) { store.set(el, ledge); }
            } else if (ledge.n !== n) {
                // đổi kích thước thì giữ nguyên lượng tuyết đã có
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
            ledge.y = y;
            next.push(ledge);
        }
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
    function prefill(ledge, scale) {
        scale = scale || 1;
        var n = ledge.n;
        for (var i = 0; i < n; i++) {
            ledge.h[i] = scale * ledge.maxH * 0.42 * Math.pow(Math.sin(Math.PI * (i + 0.5) / n), 0.6);
        }
        var bumps = Math.max(2, Math.round(n / 7));
        for (var b = 0; b < bumps; b++) {
            addSnow(ledge, (Math.random() * n) | 0, scale * ledge.maxH * (0.35 + Math.random() * 0.4));
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
        measureLedges(true);
    }

    function tick(now) {
        window.requestAnimationFrame(tick);
        var dt = Math.min((now - lastTime) / 1000 || 0.016, 0.05);
        lastTime = now;

        if (now - measureTimer > 600 || needRefresh) {
            needRefresh = false;
            measureTimer = now;
            measureLedges(true);
            geometryDirty = false;
        } else if (geometryDirty) {
            measureLedges(false);
            geometryDirty = false;
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

    window.addEventListener('scroll', function () {
        geometryDirty = true;
        // cuộn xa so với lần quét trước thì quét lại danh sách phần tử để mục mới hiện ra có tuyết bám sẵn
        if (Math.abs((window.pageYOffset || 0) - lastRefreshY) > 120) { needRefresh = true; }
    }, { passive: true });

    var resizeTimer = 0;
    window.addEventListener('resize', function () {
        window.clearTimeout(resizeTimer);
        resizeTimer = window.setTimeout(resize, 150);
    });

    resize();
    lastTime = performance.now();
    window.requestAnimationFrame(tick);
})();
