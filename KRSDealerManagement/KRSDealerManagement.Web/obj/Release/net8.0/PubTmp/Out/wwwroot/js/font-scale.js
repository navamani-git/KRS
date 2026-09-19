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

    function syncFontDropdown(form, preset) {
        var code = normalizePreset(preset);
        var hidden = form.querySelector('[data-krs-font-input]');
        if (hidden) hidden.value = code;

        form.querySelectorAll('[data-font-preset]').forEach(function (btn) {
            var active = btn.getAttribute('data-font-preset') === code;
            btn.classList.toggle('active', active);
        });

        var toggle = form.querySelector('.dropdown-toggle');
        if (toggle) {
            var label = form.querySelector('[data-font-preset="' + code + '"]')?.textContent?.trim() || code.toUpperCase();
            toggle.innerHTML = '<span class="d-none d-lg-inline me-1">Font</span>' + label;
        }
    }

    function adjustFontDropdownDirection() {
        document.querySelectorAll('.krs-font-dropdown').forEach(function (form) {
            // Navbar is at top — open menu upward on mobile. Login page keeps normal dropdown.
            var onLoginPage = !!form.closest('.login-font-bar');
            form.classList.toggle('dropup', !onLoginPage && window.innerWidth < 992);
        });
    }

    function bindFontForms() {
        document.querySelectorAll('form[data-krs-font-form]').forEach(function (form) {
            if (form.dataset.krsFontBound === '1') return;
            form.dataset.krsFontBound = '1';

            var hidden = form.querySelector('[data-krs-font-input]');
            var legacySelect = form.querySelector('select[name="fontSizePreset"]');
            if (legacySelect && hidden) {
                legacySelect.addEventListener('change', function () {
                    hidden.value = legacySelect.value;
                    applyFontPreset(legacySelect.value);
                    form.submit();
                });
                return;
            }

            form.querySelectorAll('[data-font-preset]').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    var preset = btn.getAttribute('data-font-preset');
                    if (!preset || !hidden) return;
                    hidden.value = preset;
                    applyFontPreset(preset);
                    syncFontDropdown(form, preset);
                    form.submit();
                });
            });

            if (hidden) syncFontDropdown(form, hidden.value);
        });

        adjustFontDropdownDirection();
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

    window.addEventListener('resize', adjustFontDropdownDirection);
})();
