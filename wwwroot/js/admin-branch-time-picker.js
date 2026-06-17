(function () {
    "use strict";

    if (typeof window.flatpickr !== "function") {
        return;
    }

    var validTimePattern = /^(?:[01]\d|2[0-3]):[0-5]\d$/;

    document.querySelectorAll(".js-admin-branch-time-picker").forEach(function (input) {
        if (!(input instanceof HTMLInputElement)) {
            return;
        }

        if (input.dataset.flatpickrInitialized === "true") {
            return;
        }

        var currentValue = input.value.trim();

        // Preserve invalid ModelState values so the user can correct them manually.
        if (currentValue !== "" && !validTimePattern.test(currentValue)) {
            return;
        }

        window.flatpickr(input, {
            enableTime: true,
            noCalendar: true,
            dateFormat: "H:i",
            time_24hr: true,
            minuteIncrement: 5,
            allowInput: true,
            disableMobile: true
        });

        input.dataset.flatpickrInitialized = "true";
    });
})();
