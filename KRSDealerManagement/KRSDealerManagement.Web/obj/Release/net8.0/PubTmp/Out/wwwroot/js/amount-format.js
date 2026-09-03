(function () {
    function parseIndianAmount(text) {
        if (!text) return NaN;
        var cleaned = String(text).replace(/,/g, '').replace(/[^\d.]/g, '');
        if (!cleaned) return NaN;
        var parts = cleaned.split('.');
        if (parts.length > 2) cleaned = parts.shift() + '.' + parts.join('');
        return parseFloat(cleaned);
    }

    function formatIndianAmount(value) {
        if (value === '' || value === null || value === undefined) return '';
        var num = typeof value === 'number' ? value : parseIndianAmount(value);
        if (isNaN(num)) return String(value);
        var negative = num < 0;
        num = Math.abs(num);
        var fixed = num.toFixed(2);
        var parts = fixed.split('.');
        var intPart = parts[0];
        var decPart = parts[1];
        if (intPart.length <= 3) {
            return (negative ? '-' : '') + intPart + '.' + decPart;
        }
        var lastThree = intPart.slice(-3);
        var rest = intPart.slice(0, -3);
        var grouped = rest.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
        return (negative ? '-' : '') + grouped + ',' + lastThree + '.' + decPart;
    }

    function isAmountField(el) {
        if (!el || el.tagName !== 'INPUT' || el.readOnly || el.disabled) return false;
        if (el.type === 'number') return true;
        if (el.classList.contains('amount-input')) return true;
        var name = (el.getAttribute('name') || '').toLowerCase();
        return /amount|balance|price|commission|payment/.test(name);
    }

    function attach(el) {
        if (!isAmountField(el) || el.dataset.indianAmountInit === '1') return;
        el.dataset.indianAmountInit = '1';
        if (el.type === 'number') {
            el.type = 'text';
            el.inputMode = 'decimal';
        }
        el.autocomplete = 'off';

        el.addEventListener('focus', function () {
            if (!el.value) return;
            var raw = parseIndianAmount(el.value);
            if (!isNaN(raw)) {
                el.value = raw.toString();
            } else {
                el.value = el.value.replace(/,/g, '');
            }
        });

        el.addEventListener('blur', function () {
            var raw = parseIndianAmount(el.value);
            if (!isNaN(raw)) el.value = formatIndianAmount(raw);
        });

        el.addEventListener('input', function () {
            var v = el.value;
            if (!v) return;
            var cleaned = v.replace(/,/g, '').replace(/[^\d.-]/g, '');
            var parts = cleaned.split('.');
            if (parts.length > 2) {
                cleaned = parts[0] + '.' + parts.slice(1).join('');
            }
            if (cleaned !== v) {
                var pos = el.selectionStart;
                el.value = cleaned;
                if (typeof pos === 'number') {
                    el.setSelectionRange(pos, pos);
                }
            }
        });

        if (el.value) {
            var initial = parseIndianAmount(el.value);
            if (!isNaN(initial)) el.value = formatIndianAmount(initial);
        }
    }

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || !form.querySelectorAll) return;
        form.querySelectorAll('input[data-indian-amount-init="1"]').forEach(function (el) {
            var raw = parseIndianAmount(el.value);
            if (!isNaN(raw)) el.value = raw.toString();
        });
    }, true);

    function scan(root) {
        (root || document).querySelectorAll('input').forEach(attach);
    }

    document.addEventListener('DOMContentLoaded', function () {
        scan(document);
    });

    document.addEventListener('shown.bs.modal', function (e) {
        scan(e.target);
    });

    window.krsFormatIndianAmount = formatIndianAmount;
    window.krsParseIndianAmount = parseIndianAmount;
})();
