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
    var audioContext = null;
    var notificationAudioData = null;
    var notificationAudioDataPromise = null;
    var notificationAudioBuffer = null;
    var notificationBufferPromise = null;
    var activeNotificationSource = null;
    var webAudioFailed = false;
    var audioContextReady = false;
    var soundPreferenceEnabled = false;
    var soundUnlockedForPage = false;
    var soundPlaybackBlockedByBrowser = false;
    var unlockAttemptInFlight = null;
    var unlockInteractionEvents = ["pointerdown", "touchstart", "keydown", "click"];
    var unlockInteractionListenersRegistered = false;

    function readStorage() {
        try {
            var storedValue = window.localStorage.getItem(soundStorageKey);
            if (storedValue === "false") {
                return false;
            }

            if (storedValue === "true") {
                return true;
            }

            writeStorage(true);
            return true;
        } catch {
            return true;
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
        var showActivation = soundPreferenceEnabled && !soundUnlockedForPage && soundPlaybackBlockedByBrowser;

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
        soundPlaybackBlockedByBrowser = false;
        removeAutomaticUnlockListeners();
        updateSoundControls();
    }

    function markSoundLockedForPage(reason, options) {
        var settings = options || {};

        soundUnlockedForPage = false;
        if (settings.showActivation === true) {
            soundPlaybackBlockedByBrowser = true;
        }

        registerAutomaticUnlockListeners();
        updateSoundControls(reason);
    }

    function isTrustedUserEvent(event) {
        return !!event && (!("isTrusted" in event) || event.isTrusted === true);
    }

    function isAudioDebugEnabled() {
        try {
            return window.localStorage.getItem("kig.admin.reservationNotifications.debugAudio") === "true";
        } catch {
            return false;
        }
    }

    function logAudioDebug(message, details) {
        if (!isAudioDebugEnabled()) {
            return;
        }

        if (details) {
            console.debug("[AdminReservationAudio] " + message, details);
            return;
        }

        console.debug("[AdminReservationAudio] " + message);
    }

    function isAutoplayBlocked(error) {
        return error && (error.name === "NotAllowedError" || error.name === "SecurityError");
    }

    function handleAudioFailure(error, options) {
        var settings = options || {};

        if (isAutoplayBlocked(error)) {
            markSoundLockedForPage("Browser interaction is required before notification sound can play.", {
                showActivation: settings.showActivation === true
            });

            if (settings.logBlocked === true) {
                console.warn("Admin reservation notification sound is waiting for browser activation.", error);
            }

            return;
        }

        soundUnlockedForPage = false;
        audioContextReady = false;
        soundPlaybackBlockedByBrowser = false;
        updateSoundControls("Notification sound file could not be played.");
        console.error("Admin reservation notification sound media error.", error);
    }

    function getAudioContextConstructor() {
        return window.AudioContext || window.webkitAudioContext || null;
    }

    function getAudioContext() {
        if (audioContext) {
            return audioContext;
        }

        var AudioContextConstructor = getAudioContextConstructor();
        if (!AudioContextConstructor) {
            return null;
        }

        try {
            audioContext = new AudioContextConstructor();
            logAudioDebug("audio-context-created", { state: audioContext.state });
        } catch (error) {
            webAudioFailed = true;
            console.warn("Admin reservation notification Web Audio initialization failed.", error);
            return null;
        }

        return audioContext;
    }

    function primeWebAudioSilently(context) {
        try {
            var sampleRate = context.sampleRate || 44100;
            var silentBuffer = context.createBuffer(1, 1, sampleRate);
            var silentSource = context.createBufferSource();
            silentSource.buffer = silentBuffer;
            silentSource.connect(context.destination);
            silentSource.start(0);
        } catch (error) {
            logAudioDebug("silent-prime-failed", { name: error.name, message: error.message });
        }
    }

    function decodeAudioData(context, audioData) {
        return new Promise(function (resolve, reject) {
            var settled = false;

            try {
                var decodeResult = context.decodeAudioData(
                    audioData,
                    function (decodedBuffer) {
                        settled = true;
                        resolve(decodedBuffer);
                    },
                    function (error) {
                        settled = true;
                        reject(error);
                    });

                if (decodeResult && typeof decodeResult.then === "function") {
                    decodeResult.then(function (decodedBuffer) {
                        if (!settled) {
                            resolve(decodedBuffer);
                        }
                    }).catch(function (error) {
                        if (!settled) {
                            reject(error);
                        }
                    });
                }
            } catch (error) {
                reject(error);
            }
        });
    }

    function ensureNotificationAudioData() {
        if (notificationAudioData) {
            return Promise.resolve(notificationAudioData);
        }

        if (notificationAudioDataPromise) {
            return notificationAudioDataPromise;
        }

        if (typeof window.fetch !== "function") {
            return Promise.reject(new Error("Audio fetch is unavailable."));
        }

        notificationAudioDataPromise = window.fetch(audioUrl, { cache: "force-cache" })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("Notification audio request failed with HTTP " + response.status + ".");
                }

                return response.arrayBuffer();
            })
            .then(function (audioData) {
                notificationAudioData = audioData;
                logAudioDebug("audio-data-ready", { byteLength: audioData.byteLength });
                return audioData;
            })
            .catch(function (error) {
                notificationAudioDataPromise = null;
                console.warn("Admin reservation notification audio loading failed.", error);
                throw error;
            });

        return notificationAudioDataPromise;
    }

    function ensureNotificationAudioBuffer() {
        if (notificationAudioBuffer) {
            return Promise.resolve(notificationAudioBuffer);
        }

        if (notificationBufferPromise) {
            return notificationBufferPromise;
        }

        var context = getAudioContext();
        if (!context || typeof window.fetch !== "function") {
            return Promise.reject(new Error("Web Audio is unavailable."));
        }

        notificationBufferPromise = ensureNotificationAudioData()
            .then(function (audioData) {
                return decodeAudioData(context, audioData.slice(0));
            })
            .then(function (decodedBuffer) {
                notificationAudioBuffer = decodedBuffer;
                logAudioDebug("audio-buffer-ready", {
                    duration: decodedBuffer.duration,
                    sampleRate: decodedBuffer.sampleRate
                });
                return decodedBuffer;
            })
            .catch(function (error) {
                webAudioFailed = true;
                notificationBufferPromise = null;
                console.warn("Admin reservation notification Web Audio buffer loading failed.", error);
                throw error;
            });

        return notificationBufferPromise;
    }

    function updateWebAudioReadyState() {
        audioContextReady = !!audioContext && audioContext.state === "running" && !!notificationAudioBuffer && !webAudioFailed;
        if (audioContextReady) {
            markSoundUnlockedForPage();
        }

        return audioContextReady;
    }

    async function prepareWebAudioFromGesture() {
        var context = getAudioContext();
        if (!context || webAudioFailed) {
            return false;
        }

        var resumePromise = Promise.resolve();
        try {
            if (context.state === "suspended" && typeof context.resume === "function") {
                resumePromise = context.resume();
            }

            primeWebAudioSilently(context);
            await resumePromise;
            await ensureNotificationAudioBuffer();
            var ready = updateWebAudioReadyState();
            logAudioDebug("gesture-unlock-result", {
                contextState: context.state,
                bufferReady: !!notificationAudioBuffer,
                ready: ready
            });

            if (!ready) {
                markSoundLockedForPage("Browser interaction is required before notification sound can play.", {
                    showActivation: true
                });
            }

            return ready;
        } catch (error) {
            handleAudioFailure(error, {
                showActivation: true,
                logBlocked: true
            });
            return false;
        }
    }

    async function unlockNotificationAudioFromGesture() {
        if (!soundPreferenceEnabled) {
            return false;
        }

        if (unlockAttemptInFlight) {
            return unlockAttemptInFlight;
        }

        unlockAttemptInFlight = prepareWebAudioFromGesture()
            .finally(function () {
                unlockAttemptInFlight = null;
            });

        return unlockAttemptInFlight;
    }

    function attemptPageLoadAudioReadiness() {
        if (!soundPreferenceEnabled) {
            return;
        }

        getAudio();
        ensureNotificationAudioData()
            .catch(function (error) {
                logAudioDebug("page-load-audio-prepare-failed", { name: error.name, message: error.message });
            });
    }

    function handleFirstInteraction(event) {
        if (!isTrustedUserEvent(event)) {
            return;
        }

        if (event.type === "keydown" && (event.isComposing || event.key === "Process")) {
            return;
        }

        unlockNotificationAudioFromGesture();
    }

    function enableSoundPreferenceFromGesture(event) {
        soundPreferenceEnabled = true;
        soundUnlockedForPage = false;
        soundPlaybackBlockedByBrowser = false;
        writeStorage(true);
        getAudio();
        registerAutomaticUnlockListeners();
        updateSoundControls();

        if (isTrustedUserEvent(event)) {
            unlockNotificationAudioFromGesture();
        }
    }

    function disableSoundPreference() {
        soundPreferenceEnabled = false;
        soundUnlockedForPage = false;
        soundPlaybackBlockedByBrowser = false;
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
        soundPlaybackBlockedByBrowser = false;
        getAudio();

        if (soundPreferenceEnabled) {
            registerAutomaticUnlockListeners();
            attemptPageLoadAudioReadiness();
        }

        updateSoundControls(soundPreferenceEnabled ? "Notification sound is enabled." : null);

        if (soundToggle) {
            soundToggle.addEventListener("click", function (event) {
                if (soundPreferenceEnabled) {
                    disableSoundPreference();
                    return;
                }

                enableSoundPreferenceFromGesture(event);
            });
        }

        if (soundActivation) {
            soundActivation.addEventListener("click", function (event) {
                if (!isTrustedUserEvent(event)) {
                    return;
                }

                unlockNotificationAudioFromGesture();
            });
        }

        window.addEventListener("pageshow", attemptPageLoadAudioReadiness);
        window.addEventListener("focus", attemptPageLoadAudioReadiness);
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "visible") {
                attemptPageLoadAudioReadiness();
            }
        });
    }

    function stopActiveNotificationSource() {
        if (!activeNotificationSource) {
            return;
        }

        try {
            activeNotificationSource.stop(0);
        } catch {
            return;
        } finally {
            activeNotificationSource = null;
        }
    }

    function playNotificationSoundWithWebAudio() {
        if (!audioContextReady || !audioContext || audioContext.state !== "running" || !notificationAudioBuffer) {
            return false;
        }

        try {
            stopActiveNotificationSource();
            var source = audioContext.createBufferSource();
            source.buffer = notificationAudioBuffer;
            source.connect(audioContext.destination);
            source.onended = function () {
                if (activeNotificationSource === source) {
                    activeNotificationSource = null;
                }
            };

            activeNotificationSource = source;
            source.start(0);
            markSoundUnlockedForPage();
            logAudioDebug("web-audio-playback-started", {
                contextState: audioContext.state,
                bufferDuration: notificationAudioBuffer.duration
            });
            return true;
        } catch (error) {
            activeNotificationSource = null;
            handleAudioFailure(error, {
                showActivation: true,
                logBlocked: true
            });
            return false;
        }
    }

    async function playNotificationSoundWithHtmlAudioFallback() {
        var instance = getAudio();
        try {
            resetAudio(instance);
            instance.muted = false;
            if (instance.volume === 0) {
                instance.volume = 1;
            }

            await instance.play();
            if (!soundPreferenceEnabled) {
                return false;
            }

            markSoundUnlockedForPage();
            logAudioDebug("html-audio-fallback-playback-started", {
                muted: instance.muted,
                volume: instance.volume,
                readyState: instance.readyState,
                networkState: instance.networkState
            });
            return true;
        } catch (error) {
            handleAudioFailure(error, {
                showActivation: true,
                logBlocked: true
            });
            return false;
        }
    }

    async function playNotificationSound() {
        if (!soundPreferenceEnabled) {
            return false;
        }

        logAudioDebug("playback-requested", {
            visibilityState: document.visibilityState,
            hasFocus: document.hasFocus ? document.hasFocus() : null,
            audioContextState: audioContext ? audioContext.state : null,
            audioContextReady: audioContextReady,
            bufferReady: !!notificationAudioBuffer,
            webAudioFailed: webAudioFailed
        });

        if (unlockAttemptInFlight) {
            await unlockAttemptInFlight;
        }

        updateWebAudioReadyState();
        if (playNotificationSoundWithWebAudio()) {
            return true;
        }

        if (!webAudioFailed && getAudioContextConstructor()) {
            try {
                await ensureNotificationAudioBuffer();
                updateWebAudioReadyState();
                if (playNotificationSoundWithWebAudio()) {
                    return true;
                }
            } catch (error) {
                logAudioDebug("web-audio-playback-prepare-failed", { name: error.name, message: error.message });
            }
        }

        return playNotificationSoundWithHtmlAudioFallback();
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
        soundPlaybackBlockedByBrowser = false;
        audioContextReady = false;
        stopActiveNotificationSource();

        if (audio) {
            resetAudio(audio);
        }
    }

    function exposeAudioDebugApi() {
        if (!isAudioDebugEnabled()) {
            return;
        }

        window.KIGAdminReservationNotificationAudioDebug = {
            state: function () {
                return {
                    soundPreferenceEnabled: soundPreferenceEnabled,
                    soundUnlockedForPage: soundUnlockedForPage,
                    soundPlaybackBlockedByBrowser: soundPlaybackBlockedByBrowser,
                    audioContextState: audioContext ? audioContext.state : null,
                    audioContextReady: audioContextReady,
                    notificationDataReady: !!notificationAudioData,
                    notificationBufferReady: !!notificationAudioBuffer,
                    webAudioFailed: webAudioFailed,
                    htmlAudio: audio
                        ? {
                            paused: audio.paused,
                            muted: audio.muted,
                            volume: audio.volume,
                            currentTime: audio.currentTime,
                            readyState: audio.readyState,
                            networkState: audio.networkState,
                            error: audio.error ? audio.error.code : null
                        }
                        : null
                };
            },
            playTestSound: function () {
                return playNotificationSound();
            },
            unlockFromGesture: function () {
                return unlockNotificationAudioFromGesture();
            }
        };
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
    exposeAudioDebugApi();
    initConnection();
})();
