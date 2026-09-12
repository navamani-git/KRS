(function () {
    var presetPx = {
        sm: 14,
        md: 16,
        lg: 18,
        xl: 20
    };

    function normalizePreset(value) {
        var code = (value || 'md').toString().trim().toLowerCase();
        return Object.prototype.hasOwnProperty.call(presetPx, code) ? code : 'md';
    }

    function applyFontPreset(preset) {
        var code = normalizePreset(preset);
        var px = presetPx[code];
        var root = document.documentElement;
        root.style.fontSize = px + 'px';
        root.dataset.krsFontPreset = code;
        root.classList.remove('krs-fs-sm', 'krs-fs-md', 'krs-fs-lg', 'krs-fs-xl');
        root.classList.add('krs-fs-' + code);
        return code;
    }

    function bindFontForms() {
        document.querySelectorAll('form[data-krs-font-form]').forEach(function (form) {
            if (form.dataset.krsFontBound === '1') return;
            form.dataset.krsFontBound = '1';

            var select = form.querySelector('select[name="fontSizePreset"]');
            if (!select) return;

            select.addEventListener('change', function () {
                applyFontPreset(select.value);
                form.submit();
            });
        });
    }

    window.KrsFontScale = {
        apply: applyFontPreset,
        bind: bindFontForms
    };

    var initial = document.documentElement.dataset.krsFontPreset || 'md';
    applyFontPreset(initial);

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bindFontForms);
    } else {
        bindFontForms();
    }
})();
