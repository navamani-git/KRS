window.KrsModelColors = {
    map: {},

    init(modelColorMap) {
        this.map = modelColorMap || {};
    },

    getSelectValue(selectEl) {
        if (!selectEl) return '';
        if (selectEl.tomselect) {
            const value = selectEl.tomselect.getValue();
            return Array.isArray(value) ? (value[0] || '') : (value || '');
        }
        return selectEl.value || '';
    },

    listForModel(modelId) {
        if (!modelId) return [];
        return this.map[modelId] || this.map[String(modelId)] || [];
    },

    populateSelect(selectEl, modelId, selectedColorId, placeholder) {
        if (!selectEl) return;

        if (window.KrsSearchableSelect) {
            window.KrsSearchableSelect.destroy(selectEl);
        }

        const colors = this.listForModel(modelId);
        const current = selectedColorId != null ? String(selectedColorId) : '';
        selectEl.innerHTML = '';

        const empty = document.createElement('option');
        empty.value = '';
        empty.textContent = placeholder || (modelId ? '-- Select Color --' : '-- Select Model First --');
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

        if (window.KrsSearchableSelect) {
            window.KrsSearchableSelect.init(selectEl);
        }
    },

    bind(modelSelectEl, colorSelectEl, onModelChange) {
        if (!modelSelectEl || !colorSelectEl) return;

        const refresh = () => {
            const modelId = this.getSelectValue(modelSelectEl);
            const selectedColor = this.getSelectValue(colorSelectEl);
            this.populateSelect(colorSelectEl, modelId, selectedColor);
            if (typeof onModelChange === 'function') {
                onModelChange();
            }
        };

        modelSelectEl.addEventListener('change', refresh);
        refresh();
    },

    bindEdit(modelSelectEl, colorSelectEl, initialColorId) {
        if (!modelSelectEl || !colorSelectEl) return;

        this.populateSelect(colorSelectEl, this.getSelectValue(modelSelectEl), initialColorId);
        modelSelectEl.addEventListener('change', () => {
            this.populateSelect(colorSelectEl, this.getSelectValue(modelSelectEl), null);
        });
    }
};
