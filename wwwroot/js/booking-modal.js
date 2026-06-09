(function () {
    const root = document.querySelector('[data-booking-modal]');
    if (!root) {
        return;
    }

    const panel = root.querySelector('[data-booking-modal-panel]');
    const closeButtons = root.querySelectorAll('[data-booking-modal-close]');
    const triggers = document.querySelectorAll('[data-booking-modal-trigger]');
    const backdrop = root.querySelector('[data-booking-modal-backdrop]');
    const form = root.querySelector('[data-booking-modal-form]');
    const formBody = root.querySelector('[data-booking-modal-form-body]');
    const summary = root.querySelector('[data-booking-modal-summary]');
    const success = root.querySelector('[data-booking-modal-success]');
    const successBody = root.querySelector('[data-booking-modal-success-body]');
    const submitButton = root.querySelector('[data-booking-modal-submit]');
    const firstField = root.querySelector('#booking-modal-customer-name');
    const antiForgeryField = form?.querySelector('input[name="__RequestVerificationToken"]');
    const focusableSelector = [
        'a[href]',
        'button:not([disabled])',
        'input:not([disabled])',
        'select:not([disabled])',
        'textarea:not([disabled])',
        '[tabindex]:not([tabindex="-1"])'
    ].join(',');
    const conditionalFieldConfigs = [
        {
            fieldName: 'DiningOccasionCode',
            fieldKey: 'dining-occasion',
            noteFieldName: 'DiningOccasionOtherNote'
        }
    ];

    if (!form || !panel || !summary || !success || !successBody || !submitButton) {
        return;
    }

    const initialSubmitText = submitButton.textContent?.trim() || 'Gửi yêu cầu đặt bàn';
    let lastTrigger = null;
    let isSubmitting = false;

    const getFocusableElements = () =>
        Array.from(root.querySelectorAll(focusableSelector))
            .filter((element) =>
                !element.hasAttribute('hidden') &&
                element.getAttribute('aria-hidden') !== 'true' &&
                element.getClientRects().length > 0);

    const getFieldWrapper = (fieldName) => root.querySelector(`[data-booking-modal-error-for="${fieldName}"]`)?.closest('.booking-modal__field');
    const getErrorElement = (fieldName) => root.querySelector(`[data-booking-modal-error-for="${fieldName}"]`);

    const clearSummary = () => {
        summary.hidden = true;
        summary.textContent = '';
    };

    const clearFieldErrors = () => {
        root.querySelectorAll('[data-booking-modal-error-for]').forEach((element) => {
            element.textContent = '';
        });

        root.querySelectorAll('.booking-modal__field.is-invalid').forEach((field) => {
            field.classList.remove('is-invalid');
        });
    };

    const syncConditionalField = ({ fieldName, fieldKey, noteFieldName }) => {
        const field = root.querySelector(`[data-booking-modal-other-field="${fieldKey}"]`);
        if (!(field instanceof HTMLElement)) {
            return;
        }

        const shouldShow = Array.from(form.querySelectorAll(`input[name="${fieldName}"]`))
            .some((input) => input instanceof HTMLInputElement && input.checked && input.value === 'other');

        field.hidden = !shouldShow;

        const noteField = field.querySelector(`[name="${noteFieldName}"]`);
        if (!shouldShow && (noteField instanceof HTMLInputElement || noteField instanceof HTMLTextAreaElement)) {
            noteField.value = '';
        }
    };

    const syncConditionalFields = () => {
        conditionalFieldConfigs.forEach(syncConditionalField);
    };

    const clearSuccessState = (resetValues) => {
        success.hidden = true;
        successBody.replaceChildren();
        form.dataset.completed = 'false';

        if (formBody) {
            formBody.hidden = false;
        }

        if (resetValues) {
            form.reset();
            window.requestAnimationFrame(syncConditionalFields);
        }
    };

    const resetFeedback = (resetValues) => {
        clearSummary();
        clearFieldErrors();
        clearSuccessState(resetValues);
    };

    const setSubmitting = (submitting) => {
        isSubmitting = submitting;
        submitButton.disabled = submitting;
        submitButton.classList.toggle('is-loading', submitting);
        submitButton.textContent = submitting ? 'Đang gửi yêu cầu...' : initialSubmitText;
    };

    const setOpen = (open, trigger) => {
        if (open) {
            lastTrigger = trigger ?? document.activeElement;
            resetFeedback(false);
        }

        root.classList.toggle('is-open', open);
        root.setAttribute('aria-hidden', open ? 'false' : 'true');
        document.body.classList.toggle('booking-modal-open', open);

        if (open) {
            window.requestAnimationFrame(() => {
                (firstField || panel).focus();
            });
            return;
        }

        setSubmitting(false);
        resetFeedback(form.dataset.completed === 'true');

        if (lastTrigger instanceof HTMLElement && document.contains(lastTrigger)) {
            lastTrigger.focus();
        }
    };

    const openModal = (trigger) => setOpen(true, trigger);
    const closeModal = () => {
        if (isSubmitting) {
            return;
        }

        setOpen(false);
    };

    const showSummaryErrors = (messages) => {
        if (!messages.length) {
            clearSummary();
            return;
        }

        summary.hidden = false;
        summary.replaceChildren();

        const lead = document.createElement('p');
        lead.textContent = messages[0];
        summary.appendChild(lead);

        if (messages.length > 1) {
            const list = document.createElement('ul');
            messages.slice(1).forEach((message) => {
                const item = document.createElement('li');
                item.textContent = message;
                list.appendChild(item);
            });
            summary.appendChild(list);
        }
    };

    const showFieldErrors = (errors) => {
        const summaryMessages = [];

        Object.entries(errors || {}).forEach(([fieldName, messages]) => {
            const normalizedMessages = Array.isArray(messages)
                ? messages.filter((message) => typeof message === 'string' && message.trim().length > 0)
                : [];

            if (!normalizedMessages.length) {
                return;
            }

            if (fieldName === '_summary' || fieldName === '') {
                summaryMessages.push(...normalizedMessages);
                return;
            }

            const errorElement = getErrorElement(fieldName);
            const fieldWrapper = getFieldWrapper(fieldName);

            if (!errorElement || !fieldWrapper) {
                summaryMessages.push(...normalizedMessages);
                return;
            }

            fieldWrapper.classList.add('is-invalid');
            errorElement.textContent = normalizedMessages[0];
        });

        if (summaryMessages.length) {
            showSummaryErrors(summaryMessages);
        }
        else {
            clearSummary();
        }

        const firstInvalidField = root.querySelector('.booking-modal__field.is-invalid .booking-modal__input, .booking-modal__field.is-invalid .booking-modal__select, .booking-modal__field.is-invalid .booking-modal__textarea, .booking-modal__field.is-invalid .booking-modal__checkbox');
        if (firstInvalidField instanceof HTMLElement) {
            firstInvalidField.focus();
        }
    };

    const renderSuccess = (payload) => {
        const summaryData = payload?.summary || {};
        const summaryItems = [
            ['Họ tên', summaryData.customerName],
            ['Số điện thoại', summaryData.phoneNumber],
            ['Số khách', summaryData.guestCount],
            ['Ngày đến', summaryData.reservationDate],
            ['Giờ đến', summaryData.reservationTime],
            ['Chi nhánh', summaryData.branchName],
            ['Ghi chú', summaryData.note]
        ].filter(([, value]) => value !== null && value !== undefined && `${value}`.trim().length > 0);

        successBody.replaceChildren();

        const title = document.createElement('p');
        title.className = 'booking-modal__success-title';
        title.textContent = payload?.message || 'Yêu cầu đặt bàn đã được ghi nhận.';
        successBody.appendChild(title);

        if (summaryItems.length) {
            const list = document.createElement('div');
            list.className = 'booking-modal__success-list';

            summaryItems.forEach(([label, value]) => {
                const item = document.createElement('div');
                item.className = 'booking-modal__success-item';

                const key = document.createElement('span');
                key.textContent = label;

                const strong = document.createElement('strong');
                strong.textContent = `${value}`;

                item.append(key, strong);
                list.appendChild(item);
            });

            successBody.appendChild(list);
        }

        clearSummary();
        clearFieldErrors();
        form.dataset.completed = 'true';

        if (formBody) {
            formBody.hidden = true;
        }

        success.hidden = false;
        form.reset();
        syncConditionalFields();

        const successCloseButton = success.querySelector('[data-booking-modal-close]');
        if (successCloseButton instanceof HTMLElement) {
            successCloseButton.focus();
        }
    };

    const parseJsonSafely = async (response) => {
        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            return null;
        }

        try {
            return await response.json();
        }
        catch {
            return null;
        }
    };

    triggers.forEach((trigger) => {
        trigger.addEventListener('click', (event) => {
            event.preventDefault();
            openModal(trigger);
        });
    });

    closeButtons.forEach((button) => {
        button.addEventListener('click', closeModal);
    });

    if (backdrop) {
        backdrop.addEventListener('click', closeModal);
    }

    root.addEventListener('change', (event) => {
        const target = event.target;
        if (!(target instanceof HTMLInputElement)) {
            return;
        }

        conditionalFieldConfigs.forEach((config) => {
            if (target.name === config.fieldName) {
                syncConditionalField(config);
            }
        });
    });

    form.addEventListener('submit', async (event) => {
        event.preventDefault();

        if (isSubmitting) {
            return;
        }

        resetFeedback(false);
        setSubmitting(true);

        const formData = new FormData(form);
        const requestVerificationToken = antiForgeryField?.value || '';
        const action = form.dataset.bookingModalAction || form.action;

        try {
            const response = await fetch(action, {
                method: 'POST',
                body: formData,
                credentials: 'same-origin',
                headers: {
                    Accept: 'application/json',
                    RequestVerificationToken: requestVerificationToken,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const payload = await parseJsonSafely(response);

            if (response.ok && payload?.ok) {
                renderSuccess(payload);
                return;
            }

            if (response.status === 400 && payload?.ok === false) {
                showFieldErrors(payload.errors || {});

                if (!payload.errors || Object.keys(payload.errors).length === 0) {
                    showSummaryErrors([payload.message || 'Vui lòng kiểm tra lại thông tin đặt bàn.']);
                }

                return;
            }

            showSummaryErrors([
                payload?.message || 'Không thể gửi yêu cầu đặt bàn lúc này. Vui lòng thử lại sau hoặc gọi hotline để được hỗ trợ.'
            ]);
        }
        catch {
            showSummaryErrors([
                'Kết nối đang gặp sự cố. Vui lòng thử lại sau hoặc gọi hotline để được hỗ trợ.'
            ]);
        }
        finally {
            setSubmitting(false);
        }
    });

    root.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeModal();
            return;
        }

        if (event.key !== 'Tab' || !root.classList.contains('is-open')) {
            return;
        }

        const focusableElements = getFocusableElements();
        if (!focusableElements.length) {
            event.preventDefault();
            panel.focus();
            return;
        }

        const first = focusableElements[0];
        const last = focusableElements[focusableElements.length - 1];

        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        }
        else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    });

    syncConditionalFields();
})();
