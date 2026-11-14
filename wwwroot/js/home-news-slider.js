// Home news slider: arrow buttons, progress bar and mouse drag on top of native scroll-snap.
(function () {
    'use strict';

    function initSlider(root) {
        var track = root.querySelector('[data-news-slider-track]');
        if (!track) {
            return;
        }

        var section = root.closest('section') || document;
        var prevButton = section.querySelector('[data-news-slider-prev]');
        var nextButton = section.querySelector('[data-news-slider-next]');
        var progressBar = root.querySelector('[data-news-slider-progress]');

        function stepSize() {
            var card = track.querySelector('.home-news-slider__card');
            if (!card) {
                return track.clientWidth;
            }

            var gap = parseFloat(window.getComputedStyle(track).columnGap) || 0;
            var perView = Math.max(1, Math.floor((track.clientWidth + gap) / (card.offsetWidth + gap)));
            return perView * (card.offsetWidth + gap);
        }

        function update() {
            var maxScroll = track.scrollWidth - track.clientWidth;
            var atStart = track.scrollLeft <= 2;
            var atEnd = track.scrollLeft >= maxScroll - 2;

            if (prevButton) {
                prevButton.disabled = atStart;
            }

            if (nextButton) {
                nextButton.disabled = atEnd || maxScroll <= 0;
            }

            if (progressBar) {
                var visibleRatio = track.scrollWidth > 0 ? track.clientWidth / track.scrollWidth : 1;
                var widthPercent = Math.min(100, Math.max(12, visibleRatio * 100));
                var scrolledRatio = maxScroll > 0 ? track.scrollLeft / maxScroll : 0;
                progressBar.style.width = widthPercent + '%';
                progressBar.style.transform = 'translateX(' + (scrolledRatio * (100 / widthPercent) * (100 - widthPercent)) + '%)';
            }
        }

        if (prevButton) {
            prevButton.addEventListener('click', function () {
                track.scrollBy({ left: -stepSize(), behavior: 'smooth' });
            });
        }

        if (nextButton) {
            nextButton.addEventListener('click', function () {
                track.scrollBy({ left: stepSize(), behavior: 'smooth' });
            });
        }

        track.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowRight') {
                event.preventDefault();
                track.scrollBy({ left: stepSize(), behavior: 'smooth' });
            } else if (event.key === 'ArrowLeft') {
                event.preventDefault();
                track.scrollBy({ left: -stepSize(), behavior: 'smooth' });
            }
        });

        // Mouse drag (touch devices already swipe natively).
        var pointerId = null;
        var startX = 0;
        var startScroll = 0;
        var moved = false;

        track.addEventListener('pointerdown', function (event) {
            if (event.pointerType !== 'mouse' || event.button !== 0) {
                return;
            }

            pointerId = event.pointerId;
            startX = event.clientX;
            startScroll = track.scrollLeft;
            moved = false;
        });

        track.addEventListener('pointermove', function (event) {
            if (pointerId !== event.pointerId) {
                return;
            }

            var deltaX = event.clientX - startX;
            if (!moved && Math.abs(deltaX) > 6) {
                moved = true;
                track.classList.add('is-dragging');
                track.setPointerCapture(pointerId);
            }

            if (moved) {
                track.scrollLeft = startScroll - deltaX;
            }
        });

        function endDrag(event) {
            if (pointerId !== event.pointerId) {
                return;
            }

            if (moved && track.hasPointerCapture(pointerId)) {
                track.releasePointerCapture(pointerId);
            }

            pointerId = null;
            track.classList.remove('is-dragging');

            if (moved) {
                // Let scroll-snap settle on the nearest card after the drag.
                var card = track.querySelector('.home-news-slider__card');
                var cardStep = card ? card.offsetWidth + (parseFloat(window.getComputedStyle(track).columnGap) || 0) : track.clientWidth;
                track.scrollTo({ left: Math.round(track.scrollLeft / cardStep) * cardStep, behavior: 'smooth' });
                window.setTimeout(function () { moved = false; }, 0);
            }
        }

        track.addEventListener('pointerup', endDrag);
        track.addEventListener('pointercancel', endDrag);

        // Swallow the click that ends a drag so it does not open a post.
        track.addEventListener('click', function (event) {
            if (moved) {
                event.preventDefault();
                event.stopPropagation();
                moved = false;
            }
        }, true);

        track.addEventListener('scroll', update, { passive: true });
        window.addEventListener('resize', update);
        update();
    }

    function init() {
        document.querySelectorAll('[data-news-slider]').forEach(initSlider);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
