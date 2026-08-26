window.KrsPriceColors = {
    container: null,
    emptyMessage: null,
    selectAllBtn: null,
    clearAllBtn: null,
    modelSelect: null,
    monthSelect: null,
    yearSelect: null,
    effectiveFromInput: null,
    effectiveToInput: null,
    availableUrl: '',

    init(options) {
        this.container = document.getElementById(options.containerId || 'colorCheckboxList');
        this.emptyMessage = document.getElementById(options.emptyMessageId || 'colorEmptyMessage');
        this.selectAllBtn = document.getElementById(options.selectAllId || 'colorSelectAll');
        this.clearAllBtn = document.getElementById(options.clearAllId || 'colorClearAll');
        this.modelSelect = document.getElementById(options.modelSelectId || 'modelId');
        this.monthSelect = document.getElementById(options.monthSelectId || 'month');
        this.yearSelect = document.getElementById(options.yearSelectId || 'year');
        this.effectiveFromInput = document.getElementById(options.effectiveFromId || 'effectiveFrom');
        this.effectiveToInput = document.getElementById(options.effectiveToId || 'effectiveTo');
        this.availableUrl = options.availableUrl || '';

        const refresh = () => this.refresh();
        this.modelSelect?.addEventListener('change', refresh);
        this.monthSelect?.addEventListener('change', refresh);
        this.yearSelect?.addEventListener('change', refresh);
        this.effectiveFromInput?.addEventListener('change', refresh);
        this.effectiveToInput?.addEventListener('change', refresh);

        this.selectAllBtn?.addEventListener('click', (e) => {
            e.preventDefault();
            this.container?.querySelectorAll('input[type="checkbox"]').forEach(cb => { cb.checked = true; });
        });
        this.clearAllBtn?.addEventListener('click', (e) => {
            e.preventDefault();
            this.container?.querySelectorAll('input[type="checkbox"]').forEach(cb => { cb.checked = false; });
        });

        refresh();
    },

    async refresh() {
        if (!this.container) return;

        const modelId = this.modelSelect?.value;
        if (!modelId) {
            this.render([], 'Select a model to see available colors.');
            return;
        }

        const month = this.monthSelect?.value;
        const year = this.yearSelect?.value;
        const effectiveFrom = this.effectiveFromInput?.value || '';
        const effectiveTo = this.effectiveToInput?.value || '';

        const params = new URLSearchParams({
            modelId,
            month: month || '1',
            year: year || String(new Date().getFullYear())
        });
        if (effectiveFrom) params.set('effectiveFrom', effectiveFrom);
        if (effectiveTo) params.set('effectiveTo', effectiveTo);

        this.container.innerHTML = '<div class="text-muted small py-2">Loading colors…</div>';
        if (this.emptyMessage) this.emptyMessage.classList.add('d-none');

        try {
            const res = await fetch(`${this.availableUrl}?${params.toString()}`);
            const colors = await res.json();
            if (!Array.isArray(colors) || colors.length === 0) {
                this.render([], 'All mapped colors already have a price for this period, or none are mapped to this model.');
                return;
            }
            this.render(colors, null);
        } catch {
            this.render([], 'Could not load colors. Please try again.');
        }
    },

    render(colors, message) {
        if (!this.container) return;
        this.container.innerHTML = '';

        if (!colors.length) {
            if (this.emptyMessage) {
                this.emptyMessage.textContent = message || 'No colors available.';
                this.emptyMessage.classList.remove('d-none');
            }
            return;
        }

        if (this.emptyMessage) this.emptyMessage.classList.add('d-none');

        colors.forEach(c => {
            const wrap = document.createElement('div');
            wrap.className = 'form-check mb-2';

            const input = document.createElement('input');
            input.type = 'checkbox';
            input.className = 'form-check-input price-color-checkbox';
            input.name = 'colorIds';
            input.value = String(c.id);
            input.id = `price_color_${c.id}`;

            const label = document.createElement('label');
            label.className = 'form-check-label d-flex align-items-center gap-2';
            label.htmlFor = input.id;

            if (c.hex) {
                const swatch = document.createElement('span');
                swatch.style.cssText = 'display:inline-block;width:18px;height:18px;border-radius:50%;border:1px solid #ccc;';
                swatch.style.backgroundColor = c.hex;
                label.appendChild(swatch);
            }

            const text = document.createElement('span');
            text.textContent = c.name;
            label.appendChild(text);

            wrap.appendChild(input);
            wrap.appendChild(label);
            this.container.appendChild(wrap);
        });
    },

    validateSelection() {
        const checked = this.container?.querySelectorAll('input.price-color-checkbox:checked').length || 0;
        return checked > 0;
    }
};
