window.KrsModelColors = {
    map: {},

    init(modelColorMap) {
        this.map = modelColorMap || {};
    },

    listForModel(modelId) {
        if (!modelId) return [];
        return this.map[modelId] || this.map[String(modelId)] || [];
    },

    populateSelect(selectEl, modelId, selectedColorId, placeholder) {
        if (!selectEl) return;
        const colors = this.listForModel(modelId);
        const current = selectedColorId != null ? String(selectedColorId) : selectEl.value;
        selectEl.innerHTML = '';

        const empty = document.createElement('option');
        empty.value = '';
        empty.textContent = placeholder || '-- Select Color --';
        selectEl.appendChild(empty);

        colors.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = c.name;
            if (current && String(c.id) === String(current)) {
                opt.selected = true;
            }
            selectEl.appendChild(opt);
        });

        if (current && !colors.some(c => String(c.id) === String(current))) {
            selectEl.value = '';
        }
    },

    bind(modelSelectEl, colorSelectEl, onModelChange) {
        if (!modelSelectEl || !colorSelectEl) return;

        const refresh = () => {
            const selectedColor = colorSelectEl.value;
            this.populateSelect(colorSelectEl, modelSelectEl.value, selectedColor);
            if (typeof onModelChange === 'function') {
                onModelChange();
            }
        };

        modelSelectEl.addEventListener('change', refresh);
        refresh();
    }
};
