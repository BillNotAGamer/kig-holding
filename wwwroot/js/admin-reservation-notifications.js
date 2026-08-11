(function () {
    if (window.KIGAdminReservationNotificationsBound) {
        return;
    }

    window.KIGAdminReservationNotificationsBound = true;

    var host = document.querySelector("[data-admin-reservation-toast-host]");
    if (!host || typeof window.signalR === "undefined") {
        return;
    }

    var hubUrl = host.getAttribute("data-admin-reservation-hub-url");
    var detailsUrlTemplate = host.getAttribute("data-admin-reservation-details-url-template");
    var audioUrl = host.getAttribute("data-admin-reservation-audio-url");
    var soundToggle = document.querySelector("[data-admin-reservation-sound-toggle]");
    var soundLabel = document.querySelector("[data-admin-reservation-sound-label]");
    var soundActivation = document.querySelector("[data-admin-reservation-sound-activation]");

    if (!hubUrl || !detailsUrlTemplate || !audioUrl) {
        return;
    }

    var eventName = "AdminReservationCreated";
    var soundStorageKey = "kig.admin.reservationNotifications.soundEnabled";
    var recentRetentionMs = 5 * 60 * 1000;
    var audioLockRetentionMs = 1500;
    var reconnectDelays = [0, 2000, 5000, 10000, 30000];
    var recentReservationIds = new Map();
    var stopped = false;
    var startPromise = null;
    var retryTimerId = null;
    var retryAttempt = 0;
    var audio = null;
    var soundPreferenceEnabled = false;
    var soundUnlockedForPage = false;
    var unlockPromise = null;
    var unlockInteractionEvents = ["pointerdown", "keydown", "touchstart"];
    var unlockInteractionListenersRegistered = false;

    function readStorage() {
        try {
            return window.localStorage.getItem(soundStorageKey) === "true";
        } catch {
            return false;
        }
    }

    function writeStorage(enabled) {
        try {
            window.localStorage.setItem(soundStorageKey, enabled ? "true" : "false");
        } catch {
            return;
        }
    }

    function updateSoundControls(reason) {
        var showActivation = soundPreferenceEnabled && !soundUnlockedForPage;

        if (soundToggle) {
            soundToggle.setAttribute("aria-pressed", soundPreferenceEnabled ? "true" : "false");
            if (reason) {
                soundToggle.setAttribute("title", reason);
            } else {
                soundToggle.removeAttribute("title");
            }
        }

        if (soundLabel) {
            soundLabel.textContent = soundPreferenceEnabled
                ? "Tắt âm thanh thông báo"
                : "Bật âm thanh thông báo";
        }

        if (soundActivation) {
            soundActivation.hidden = !showActivation;
            soundActivation.classList.toggle("hidden", !showActivation);
        }
    }

    function getAudio() {
        if (!audio) {
            audio = new Audio(audioUrl);
            audio.preload = "auto";
            try {
                audio.load();
            } catch {
                return audio;
            }
        }

        return audio;
    }

    function resetAudio(instance) {
        try {
            instance.pause();
            instance.currentTime = 0;
        } catch {
            return;
        }
    }

    function removeAutomaticUnlockListeners() {
        if (!unlockInteractionListenersRegistered) {
            return;
        }

        unlockInteractionEvents.forEach(function (eventName) {
            document.removeEventListener(eventName, handleFirstInteraction, true);
        });
        unlockInteractionListenersRegistered = false;
    }

    function registerAutomaticUnlockListeners() {
        if (!soundPreferenceEnabled || soundUnlockedForPage || unlockInteractionListenersRegistered) {
            return;
        }

        unlockInteractionEvents.forEach(function (eventName) {
            document.addEventListener(eventName, handleFirstInteraction, true);
        });
        unlockInteractionListenersRegistered = true;
    }

    function markSoundUnlockedForPage() {
        soundUnlockedForPage = true;
        removeAutomaticUnlockListeners();
        updateSoundControls();
    }

    function markSoundLockedForPage(reason) {
        soundUnlockedForPage = false;
        registerAutomaticUnlockListeners();
        updateSoundControls(reason);
    }

    async function unlockNotificationAudioFromGesture() {
        if (!soundPreferenceEnabled) {
            return false;
        }

        if (soundUnlockedForPage) {
            return true;
        }

        if (unlockPromise) {
            return unlockPromise;
        }

        var instance = getAudio();
        unlockPromise = (async function () {
            var previousMuted = instance.muted;
            var previousVolume = instance.volume;

            try {
                instance.muted = true;
                instance.volume = 0;
                resetAudio(instance);
                await instance.play();
                resetAudio(instance);
                if (!soundPreferenceEnabled) {
                    return false;
                }

                markSoundUnlockedForPage();
                return true;
            } catch (error) {
                markSoundLockedForPage("Trình duyệt cần một thao tác để cho phép phát âm thanh.");
                console.warn("Admin reservation notification sound is waiting for browser activation.", error);
                return false;
            } finally {
                instance.muted = previousMuted;
                instance.volume = previousVolume;
                unlockPromise = null;
            }
        })();

        return unlockPromise;
    }

    function handleFirstInteraction(event) {
        if (event.type === "keydown" && (event.isComposing || event.key === "Process")) {
            return;
        }

        unlockNotificationAudioFromGesture();
    }

    function enableSoundPreferenceFromGesture() {
        soundPreferenceEnabled = true;
        soundUnlockedForPage = false;
        writeStorage(true);
        getAudio();
        registerAutomaticUnlockListeners();
        updateSoundControls();
        unlockNotificationAudioFromGesture();
    }

    function disableSoundPreference() {
        soundPreferenceEnabled = false;
        soundUnlockedForPage = false;
        removeAutomaticUnlockListeners();

        if (audio) {
            resetAudio(audio);
        }

        writeStorage(false);
        updateSoundControls();
    }

    function initSoundControls() {
        soundPreferenceEnabled = readStorage();
        soundUnlockedForPage = false;
        getAudio();

        if (soundPreferenceEnabled) {
            registerAutomaticUnlockListeners();
        }

        updateSoundControls(soundPreferenceEnabled ? "Trình duyệt có thể cần một thao tác để cho phép phát âm thanh." : null);

        if (soundToggle) {
            soundToggle.addEventListener("click", function () {
                if (soundPreferenceEnabled) {
                    disableSoundPreference();
                    return;
                }

                enableSoundPreferenceFromGesture();
            });
        }

        if (soundActivation) {
            soundActivation.addEventListener("click", function () {
                unlockNotificationAudioFromGesture();
            });
        }
    }

    async function playNotificationSound() {
        if (!soundPreferenceEnabled) {
            return false;
        }

        var instance = getAudio();
        try {
            resetAudio(instance);
            await instance.play();
            if (!soundPreferenceEnabled) {
                return false;
            }

            markSoundUnlockedForPage();
            return true;
        } catch (error) {
            markSoundLockedForPage("Trình duyệt cần một thao tác để cho phép phát âm thanh.");
            console.warn("Admin reservation notification sound playback was blocked.", error);
            return false;
        }
    }

    function attemptReservationAudio(reservationId) {
        if (!soundPreferenceEnabled) {
            return;
        }

        if (document.visibilityState !== "visible") {
            return;
        }

        if (!navigator.locks || typeof navigator.locks.request !== "function") {
            playNotificationSound();
            return;
        }

        navigator.locks.request(
            "kig-admin-reservation-audio:" + reservationId,
            { ifAvailable: true, mode: "exclusive" },
            async function (lock) {
                if (!lock || stopped || !soundPreferenceEnabled || document.visibilityState !== "visible") {
                    return;
                }

                await playNotificationSound();
                await wait(audioLockRetentionMs);
            }).catch(function (error) {
                console.warn("Admin reservation notification audio lock failed.", error);
            });
    }

    function teardownSoundForPage() {
        removeAutomaticUnlockListeners();
        soundUnlockedForPage = false;

        if (audio) {
            resetAudio(audio);
        }
    }

    function getPayloadValue(payload, camelName, pascalName) {
        if (Object.prototype.hasOwnProperty.call(payload, camelName)) {
            return payload[camelName];
        }

        return payload[pascalName];
    }

    function warnInvalidPayload(reason) {
        console.warn("Ignored invalid Admin reservation notification payload: " + reason + ".");
    }

    function isLeapYear(year) {
        return (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
    }

    function daysInMonth(year, month) {
        if (month === 2) {
            return isLeapYear(year) ? 29 : 28;
        }

        if (month === 4 || month === 6 || month === 9 || month === 11) {
            return 30;
        }

        return 31;
    }

    function validateReservationDate(dateValue) {
        if (typeof dateValue !== "string") {
            return null;
        }

        var match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateValue);
        if (!match) {
            return null;
        }

        var year = Number(match[1]);
        var month = Number(match[2]);
        var day = Number(match[3]);
        if (month < 1 || month > 12 || day < 1 || day > daysInMonth(year, month)) {
            return null;
        }

        return dateValue;
    }

    function validateReservationTime(timeValue) {
        if (typeof timeValue !== "string") {
            return null;
        }

        var match = /^(\d{2}):(\d{2})(?::(\d{2})(?:\.\d+)?)?$/.exec(timeValue);
        if (!match) {
            return null;
        }

        var hour = Number(match[1]);
        var minute = Number(match[2]);
        var second = typeof match[3] === "string" ? Number(match[3]) : 0;
        if (hour > 23 || minute > 59 || second > 59) {
            return null;
        }

        return timeValue.slice(0, 5);
    }

    function normalizeNotification(payload) {
        if (!payload || typeof payload !== "object") {
            warnInvalidPayload("payload is not an object");
            return null;
        }

        var reservationId = getPayloadValue(payload, "reservationId", "ReservationId");
        var customerName = getPayloadValue(payload, "customerName", "CustomerName");
        var branchName = getPayloadValue(payload, "branchName", "BranchName");
        var reservationDate = getPayloadValue(payload, "reservationDate", "ReservationDate");
        var reservationTime = getPayloadValue(payload, "reservationTime", "ReservationTime");
        var guestCount = getPayloadValue(payload, "guestCount", "GuestCount");

        if (typeof reservationId !== "string" || !reservationId.trim()) {
            warnInvalidPayload("reservationId is missing");
            return null;
        }

        if (typeof customerName !== "string" || !customerName.trim()) {
            warnInvalidPayload("customerName is missing");
            return null;
        }

        if (typeof branchName !== "string" || !branchName.trim()) {
            warnInvalidPayload("branchName is missing");
            return null;
        }

        var validDate = validateReservationDate(reservationDate);
        if (!validDate) {
            warnInvalidPayload("reservationDate is invalid");
            return null;
        }

        var validTime = validateReservationTime(reservationTime);
        if (!validTime) {
            warnInvalidPayload("reservationTime is invalid");
            return null;
        }

        var normalizedGuestCount = Number(guestCount);
        if (!Number.isInteger(normalizedGuestCount) || normalizedGuestCount <= 0) {
            warnInvalidPayload("guestCount is invalid");
            return null;
        }

        return {
            reservationId: reservationId.trim(),
            customerName: customerName.trim(),
            branchName: branchName.trim(),
            reservationDate: validDate,
            reservationTime: validTime,
            guestCount: normalizedGuestCount
        };
    }

    function hasRecentlyHandled(reservationId) {
        var now = Date.now();
        recentReservationIds.forEach(function (expiresAt, key) {
            if (expiresAt <= now) {
                recentReservationIds.delete(key);
            }
        });

        if (recentReservationIds.has(reservationId)) {
            return true;
        }

        recentReservationIds.set(reservationId, now + recentRetentionMs);
        return false;
    }

    function formatDate(dateValue) {
        var match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateValue);
        return match[3] + "/" + match[2] + "/" + match[1];
    }

    function formatTime(timeValue) {
        return timeValue.slice(0, 5);
    }

    function buildDetailsUrl(reservationId) {
        return detailsUrlTemplate.replace("__RESERVATION_ID__", encodeURIComponent(reservationId));
    }

    function scheduleToastDismiss(toast) {
        window.setTimeout(function () {
            toast.classList.add("opacity-0", "translate-y-2");
            window.setTimeout(function () {
                toast.remove();
            }, 300);
        }, 4000);
    }

    function createTextElement(tagName, className, text) {
        var element = document.createElement(tagName);
        if (className) {
            element.className = className;
        }

        element.textContent = text;
        return element;
    }

    function showReservationToast(notification) {
        var toast = document.createElement("div");
        toast.className = "admin-toast pointer-events-auto rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900 shadow-soft transition-all duration-300";
        toast.setAttribute("data-toast", "");
        toast.setAttribute("data-admin-reservation-toast", "");

        var row = document.createElement("div");
        row.className = "flex items-start gap-3";

        var icon = createTextElement("span", "mt-0.5 grid size-5 shrink-0 place-items-center rounded-full bg-amber-100 text-xs font-black text-amber-700", "!");
        var body = document.createElement("div");
        body.className = "min-w-0";

        body.appendChild(createTextElement("p", "font-semibold text-amber-950", "Đặt bàn mới"));
        body.appendChild(createTextElement("p", "mt-1 break-words leading-6", notification.customerName));
        body.appendChild(createTextElement(
            "p",
            "mt-1 leading-6 text-amber-900",
            notification.branchName + " • " + formatDate(notification.reservationDate) + " " + formatTime(notification.reservationTime) + " • " + notification.guestCount + " khách"));

        var link = createTextElement("a", "mt-2 inline-flex text-sm font-semibold text-amber-950 underline underline-offset-4", "Mở chi tiết");
        link.href = buildDetailsUrl(notification.reservationId);
        body.appendChild(link);

        row.appendChild(icon);
        row.appendChild(body);
        toast.appendChild(row);
        host.appendChild(toast);
        scheduleToastDismiss(toast);
    }

    function handleReservationCreated(payload) {
        var notification = normalizeNotification(payload);
        if (!notification || hasRecentlyHandled(notification.reservationId)) {
            return;
        }

        showReservationToast(notification);
        attemptReservationAudio(notification.reservationId);
    }

    function wait(delay) {
        return new Promise(function (resolve) {
            window.setTimeout(resolve, delay);
        });
    }

    function clearRetryTimer() {
        if (retryTimerId !== null) {
            window.clearTimeout(retryTimerId);
            retryTimerId = null;
        }
    }

    function isConnectionDisconnected(connection) {
        if (signalR.HubConnectionState && signalR.HubConnectionState.Disconnected) {
            return connection.state === signalR.HubConnectionState.Disconnected;
        }

        return connection.state === "Disconnected";
    }

    function scheduleConnectionRetry(connection) {
        if (stopped || retryTimerId !== null) {
            return;
        }

        var delay = reconnectDelays[Math.min(retryAttempt, reconnectDelays.length - 1)];
        retryAttempt++;
        retryTimerId = window.setTimeout(function () {
            retryTimerId = null;
            startDisconnectedConnection(connection);
        }, delay);
    }

    function startDisconnectedConnection(connection) {
        if (stopped) {
            return Promise.resolve();
        }

        if (!isConnectionDisconnected(connection)) {
            return Promise.resolve();
        }

        if (startPromise) {
            return startPromise;
        }

        clearRetryTimer();
        startPromise = connection.start()
            .then(function () {
                retryAttempt = 0;
            })
            .catch(function (error) {
                console.warn("Admin reservation notification connection failed; retrying.", error);
                scheduleConnectionRetry(connection);
            })
            .finally(function () {
                startPromise = null;
            });

        return startPromise;
    }

    function initConnection() {
        var connection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect(reconnectDelays)
            .build();

        connection.on(eventName, handleReservationCreated);
        connection.onreconnecting(function (error) {
            if (error) {
                console.warn("Admin reservation notification connection reconnecting.", error);
            }
        });
        connection.onreconnected(function () {
            retryAttempt = 0;
            clearRetryTimer();
        });
        connection.onclose(function (error) {
            if (!stopped && error) {
                console.warn("Admin reservation notification connection closed.", error);
            }
            scheduleConnectionRetry(connection);
        });

        startDisconnectedConnection(connection);

        window.addEventListener("beforeunload", function () {
            stopped = true;
            clearRetryTimer();
            teardownSoundForPage();
            connection.stop();
        });
    }

    initSoundControls();
    initConnection();
})();
