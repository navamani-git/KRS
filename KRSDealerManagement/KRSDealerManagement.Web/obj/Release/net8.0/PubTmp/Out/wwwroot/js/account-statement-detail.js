(function () {
    function decodeTxnDetail(btn) {
        var encoded = btn.getAttribute('data-txn-detail');
        if (!encoded) return null;

        try {
            var json = atob(encoded);
            return JSON.parse(json);
        } catch (e) {
            return null;
        }
    }

    function setText(id, value) {
        var el = document.getElementById(id);
        if (!el) return;
        el.textContent = value && String(value).trim() ? value : '—';
    }

    function setHtml(id, value) {
        var el = document.getElementById(id);
        if (!el) return;
        if (!value || !String(value).trim()) {
            el.textContent = '—';
            return;
        }
        el.textContent = value;
    }

    function populateStatementDetailModal(btn) {
        var detail = decodeTxnDetail(btn);
        var subtitle = document.getElementById('statementDetailSubtitle');
        var datesBody = document.getElementById('statementDetailDates');

        if (!detail) {
            if (subtitle) subtitle.textContent = 'Could not load transaction details.';
            if (datesBody) {
                datesBody.innerHTML = '<tr><td class="text-muted" colspan="2">Details are unavailable for this row.</td></tr>';
            }
            setText('statementDetailRemarks', '—');
            return;
        }

        if (subtitle) {
            subtitle.textContent = detail.subtitle || [detail.statementDate, detail.categoryLabel].filter(Boolean).join(' · ');
        }

        setText('statementDetailDescription', detail.description);
        setText('statementDetailCustomer', detail.customer);
        setText('statementDetailPayType', detail.paymentType);
        setText('statementDetailFinance', detail.finance);
        var vinEl = document.getElementById('statementDetailVin');
        if (vinEl) {
            vinEl.textContent = '';
            if (detail.vin && String(detail.vin).trim()) {
                var code = document.createElement('code');
                code.textContent = detail.vin;
                vinEl.appendChild(code);
            } else {
                vinEl.textContent = '—';
            }
        }
        setText('statementDetailRequested', detail.requestedAmount);
        setText('statementDetailApproved', detail.approvedAmount);
        setText('statementDetailDebit', detail.debit);
        setText('statementDetailCredit', detail.credit);
        setText('statementDetailBalance', detail.balanceAfter);
        setText('statementDetailReference', detail.reference);
        setHtml('statementDetailRemarks', detail.remarks);

        if (!datesBody) return;
        datesBody.innerHTML = '';

        var dates = Array.isArray(detail.dates) ? detail.dates : [];
        if (!dates.length) {
            datesBody.innerHTML = '<tr><td class="text-muted" colspan="2">No milestone dates recorded.</td></tr>';
            return;
        }

        dates.forEach(function (d) {
            var tr = document.createElement('tr');
            var label = document.createElement('td');
            label.className = 'text-muted';
            label.style.width = '42%';
            label.textContent = d.label || '—';
            var value = document.createElement('td');
            value.textContent = d.value || '—';
            tr.appendChild(label);
            tr.appendChild(value);
            datesBody.appendChild(tr);
        });
    }

    function initStatementDetailModal() {
        var modalEl = document.getElementById('statementDetailModal');
        if (!modalEl || modalEl.dataset.krsStatementDetailInit === '1') return;
        modalEl.dataset.krsStatementDetailInit = '1';

        if (modalEl.parentElement !== document.body) {
            document.body.appendChild(modalEl);
        }

        modalEl.addEventListener('show.bs.modal', function (e) {
            var btn = e.relatedTarget;
            if (!btn || !btn.classList.contains('statement-detail-btn')) return;
            populateStatementDetailModal(btn);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initStatementDetailModal);
    } else {
        initStatementDetailModal();
    }
})();
