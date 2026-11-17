// Booking conditions: small note under "Đặt bàn ngay" buttons + guest-count hints inside booking forms.
(function () {
    'use strict';

    var NOTE_TEXT = 'T2 – T6 tối thiểu 6 khách · T7 – CN tối thiểu 10 khách · Từ 20 khách đặt cọc giữ bàn';
    var DEPOSIT_FROM = 20;
    var DAY_NAMES = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];

    function normalize(text) {
        return (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
    }

    function addButtonNotes() {
        var links = document.querySelectorAll('a[href="/dat-ban"], a[href^="/dat-ban?"], button[data-booking-modal-open]');
        links.forEach(function (link) {
            if (normalize(link.textContent) !== 'đặt bàn ngay') return;
            if (link.closest('header, nav, [data-site-header], [data-mobile-menu], [role="dialog"], .booking-conditions-host')) return;

            var parent = link.parentElement;
            if (!parent) return;
            if (parent.querySelector(':scope > .booking-conditions-note') ||
                (parent.nextElementSibling && parent.nextElementSibling.classList.contains('booking-conditions-note')) ||
                (link.nextElementSibling && link.nextElementSibling.classList.contains('booking-conditions-note'))) return;

            var style = window.getComputedStyle(parent);
            var isFlexRow = style.display.indexOf('flex') !== -1 && style.flexDirection.indexOf('row') === 0;
            var isRow = isFlexRow || style.display.indexOf('grid') !== -1;
            var justify = style.justifyContent || '';

            var note = document.createElement('p');
            note.className = 'booking-conditions-note';
            note.textContent = NOTE_TEXT;

            function alignFromJustify() {
                if (justify.indexOf('center') !== -1) return 'center';
                if (justify.indexOf('end') !== -1 || justify.indexOf('right') !== -1) return 'right';
                return '';
            }

            if (isFlexRow && style.flexWrap === 'wrap') {
                // Wrapping button row: drop the note on its own line inside the row (keeps reveal animations).
                note.style.flexBasis = '100%';
                note.style.textAlign = alignFromJustify();
                parent.appendChild(note);
                return;
            }

            if (isRow) {
                var box = (parent.parentElement || parent).getBoundingClientRect();
                var linkRect = link.getBoundingClientRect();
                if (box.width > 0 && linkRect.width > 0) {
                    var center = (linkRect.left + linkRect.width / 2 - box.left) / box.width;
                    note.style.textAlign = center < 0.38 ? 'left' : (center > 0.62 ? 'right' : 'center');
                } else {
                    note.style.textAlign = alignFromJustify();
                }
                parent.insertAdjacentElement('afterend', note);
                return;
            }

            link.insertAdjacentElement('afterend', note);
        });
    }

    function parseDay(value) {
        var m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
        if (!m) return null;
        return new Date(Date.UTC(+m[1], +m[2] - 1, +m[3])).getUTCDay();
    }

    function wireForm(form) {
        var guest = form.querySelector('[name="GuestCount"]');
        if (!guest || guest.dataset.bookingConditionsWired) return;
        guest.dataset.bookingConditionsWired = 'true';
        var dateInput = form.querySelector('[name="ReservationDate"]');

        var hint = document.createElement('p');
        hint.className = 'booking-conditions-hint';
        hint.setAttribute('aria-live', 'polite');
        var label = guest.closest('label');
        if (label) {
            label.appendChild(hint);
        } else {
            guest.insertAdjacentElement('afterend', hint);
        }

        function update() {
            var count = parseInt(guest.value, 10) || 0;
            var day = dateInput ? parseDay(dateInput.value) : null;
            var parts = [];
            if (day !== null) {
                var weekend = day === 0 || day === 6;
                parts.push(DAY_NAMES[day] + ': tối thiểu ' + (weekend ? 10 : 6) + ' khách.');
            } else {
                parts.push('T2 – T6 tối thiểu 6 khách, T7 – CN tối thiểu 10 khách.');
            }
            if (count >= DEPOSIT_FROM) {
                parts.push('Nhóm từ 20 khách cần đặt cọc giữ bàn, nhà hàng sẽ liên hệ hướng dẫn.');
                hint.classList.add('is-deposit');
            } else {
                hint.classList.remove('is-deposit');
            }
            hint.textContent = parts.join(' ');
        }

        guest.addEventListener('input', update);
        guest.addEventListener('change', update);
        if (dateInput) {
            dateInput.addEventListener('input', update);
            dateInput.addEventListener('change', update);
        }
        update();
    }

    function init() {
        addButtonNotes();
        document.querySelectorAll('form').forEach(function (form) {
            if (form.querySelector('[name="GuestCount"]') && form.querySelector('[name="ReservationDate"]')) {
                wireForm(form);
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
