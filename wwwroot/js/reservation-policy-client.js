(function () {
    const genericFailureMessage = 'Kh\u00f4ng th\u1ec3 ki\u1ec3m tra ch\u00ednh s\u00e1ch \u0111\u1eb7t b\u00e0n l\u00fac n\u00e0y. Vui l\u00f2ng th\u1eed l\u1ea1i.';
    const guidPattern = /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;
    const supportedStatuses = new Set([
        'Allowed',
        'PastDate',
        'GlobalBlockedDate',
        'InvalidBranch',
        'InvalidGuestCount',
        'BranchClosedDate',
        'MinimumGuestsNotMet'
    ]);

    const isValidDateInput = (value) => {
        const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
        if (!match) {
            return false;
        }

        const year = Number(match[1]);
        const month = Number(match[2]);
        const day = Number(match[3]);
        const parsed = new Date(Date.UTC(year, month - 1, day));
        return parsed.getUTCFullYear() === year &&
            parsed.getUTCMonth() === month - 1 &&
            parsed.getUTCDate() === day;
    };

    const createReservationPolicyValidator = (options) => {
        const form = options?.form;
        const branchField = options?.branchField;
        const dateField = options?.dateField;
        const guestCountField = options?.guestCountField;
        const endpoint = options?.endpoint;

        if (!(form instanceof HTMLFormElement) ||
            !(branchField instanceof HTMLSelectElement || branchField instanceof HTMLInputElement) ||
            !(dateField instanceof HTMLInputElement) ||
            !(guestCountField instanceof HTMLInputElement) ||
            typeof endpoint !== 'string' || endpoint.length === 0) {
            return null;
        }

        let requestSequence = 0;
        let activeController = null;
        let debounceTimer = null;

        const getTuple = () => ({
            branchId: branchField.value.trim(),
            reservationDate: dateField.value.trim(),
            guestCount: guestCountField.value.trim()
        });

        const getTupleKey = (tuple) =>
            `${tuple.branchId}|${tuple.reservationDate}|${tuple.guestCount}`;

        const isCurrentTuple = (key) => getTupleKey(getTuple()) === key;

        const clearPolicyFeedback = () => {
            if (typeof options.clearPolicyFeedback === 'function') {
                options.clearPolicyFeedback();
            }
        };

        const showPolicyError = (fieldName, message, status) => {
            if (typeof options.showPolicyError === 'function') {
                options.showPolicyError(fieldName, message, status);
            }
        };

        const abortActiveRequest = () => {
            requestSequence += 1;
            if (activeController) {
                activeController.abort();
                activeController = null;
            }
        };

        const validateLocalInput = (tuple, showIncomplete) => {
            if (!tuple.branchId) {
                if (showIncomplete) {
                    showPolicyError('BranchId', 'Vui l\u00f2ng ch\u1ecdn chi nh\u00e1nh.', 'InvalidBranch');
                }
                return { complete: false, valid: false };
            }

            if (!guidPattern.test(tuple.branchId)) {
                showPolicyError('BranchId', 'Chi nh\u00e1nh kh\u00f4ng h\u1ee3p l\u1ec7.', 'InvalidBranch');
                return { complete: true, valid: false };
            }

            if (!tuple.reservationDate) {
                if (showIncomplete) {
                    showPolicyError('ReservationDate', 'Vui l\u00f2ng ch\u1ecdn ng\u00e0y \u0111\u1ebfn.', 'InvalidRequest');
                }
                return { complete: false, valid: false };
            }

            if (!isValidDateInput(tuple.reservationDate)) {
                showPolicyError('ReservationDate', 'Ng\u00e0y \u0111\u1ebfn kh\u00f4ng h\u1ee3p l\u1ec7.', 'InvalidRequest');
                return { complete: true, valid: false };
            }

            if (typeof window.kigValidateReservationDate === 'function') {
                const dateResult = window.kigValidateReservationDate(tuple.reservationDate);
                if (!dateResult.valid) {
                    showPolicyError(
                        'ReservationDate',
                        dateResult.message || 'Ng\u00e0y \u0111\u1ebfn kh\u00f4ng h\u1ee3p l\u1ec7.',
                        'GlobalBlockedDate');
                }
            }

            if (!tuple.guestCount) {
                if (showIncomplete) {
                    showPolicyError('GuestCount', 'Vui l\u00f2ng nh\u1eadp s\u1ed1 kh\u00e1ch.', 'InvalidGuestCount');
                }
                return { complete: false, valid: false };
            }

            const guestCount = Number(tuple.guestCount);
            if (!/^\d+$/.test(tuple.guestCount) || !Number.isInteger(guestCount) || guestCount < 1 || guestCount > 100) {
                showPolicyError('GuestCount', 'S\u1ed1 kh\u00e1ch ph\u1ea3i t\u1eeb 1 \u0111\u1ebfn 100.', 'InvalidGuestCount');
                return { complete: true, valid: false };
            }

            return { complete: true, valid: true };
        };

        const validate = async ({ showIncomplete = false } = {}) => {
            abortActiveRequest();
            clearPolicyFeedback();

            const tuple = getTuple();
            const tupleKey = getTupleKey(tuple);
            const localResult = validateLocalInput(tuple, showIncomplete);
            if (!localResult.complete || !localResult.valid) {
                return false;
            }

            const sequence = requestSequence;
            const controller = new AbortController();
            activeController = controller;
            let timedOut = false;
            const timeout = window.setTimeout(() => {
                timedOut = true;
                controller.abort();
            }, 10000);

            const requestUrl = new URL(endpoint, window.location.origin);
            requestUrl.searchParams.set('branchId', tuple.branchId);
            requestUrl.searchParams.set('reservationDate', tuple.reservationDate);
            requestUrl.searchParams.set('guestCount', tuple.guestCount);

            try {
                const response = await fetch(requestUrl.toString(), {
                    method: 'GET',
                    credentials: 'same-origin',
                    cache: 'no-store',
                    headers: { Accept: 'application/json' },
                    signal: controller.signal
                });

                const responseText = await response.text();
                let payload = null;
                try {
                    payload = responseText ? JSON.parse(responseText) : null;
                }
                catch {
                    payload = null;
                }

                if (sequence !== requestSequence || !isCurrentTuple(tupleKey)) {
                    return false;
                }

                clearPolicyFeedback();

                if (!response.ok) {
                    showPolicyError(
                        typeof payload?.fieldName === 'string' ? payload.fieldName : null,
                        typeof payload?.message === 'string' && payload.message.trim() ? payload.message : genericFailureMessage,
                        typeof payload?.status === 'string' ? payload.status : 'VerificationFailed');
                    return false;
                }

                if (!payload || typeof payload.status !== 'string' || !supportedStatuses.has(payload.status)) {
                    showPolicyError(null, genericFailureMessage, 'VerificationFailed');
                    return false;
                }

                if (payload.status === 'Allowed') {
                    clearPolicyFeedback();
                    return true;
                }

                if (typeof payload.message !== 'string' || !payload.message.trim()) {
                    showPolicyError(null, genericFailureMessage, 'VerificationFailed');
                    return false;
                }

                showPolicyError(
                    typeof payload.fieldName === 'string' ? payload.fieldName : null,
                    payload.message,
                    payload.status);
                return false;
            }
            catch (error) {
                if (sequence !== requestSequence || !isCurrentTuple(tupleKey)) {
                    return false;
                }

                if (error?.name === 'AbortError' && !timedOut) {
                    return false;
                }

                clearPolicyFeedback();
                showPolicyError(null, genericFailureMessage, 'VerificationFailed');
                return false;
            }
            finally {
                window.clearTimeout(timeout);
                if (activeController === controller) {
                    activeController = null;
                }
            }
        };

        const handleInputChange = () => {
            if (debounceTimer !== null) {
                window.clearTimeout(debounceTimer);
            }

            abortActiveRequest();
            clearPolicyFeedback();

            const tuple = getTuple();
            const localResult = validateLocalInput(tuple, false);
            if (!localResult.complete || !localResult.valid) {
                return;
            }

            debounceTimer = window.setTimeout(() => {
                debounceTimer = null;
                void validate();
            }, 150);
        };

        branchField.addEventListener('change', handleInputChange);
        dateField.addEventListener('change', handleInputChange);
        dateField.addEventListener('input', handleInputChange);
        guestCountField.addEventListener('change', handleInputChange);
        guestCountField.addEventListener('input', handleInputChange);

        return {
            validateForSubmit: () => validate({ showIncomplete: true }),
            refresh: handleInputChange,
            cancel: abortActiveRequest
        };
    };

    window.kigCreateReservationPolicyValidator = createReservationPolicyValidator;
})();
