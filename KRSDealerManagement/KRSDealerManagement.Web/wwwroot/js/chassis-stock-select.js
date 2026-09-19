/**
 * Load dealer-stock chassis options for order allocate / create-for-subdealer screens.
 */
window.KrsChassisStock = {
    optionId(m) {
        return String(m.vehicleMasterId ?? m.VehicleMasterId ?? '');
    },

    optionLabel(m) {
        return m.chassisNumber ?? m.ChassisNumber ?? '';
    },

    getSelectValue(select) {
        if (!select) return '';
        if (select.tomselect) {
            const value = select.tomselect.getValue();
            return Array.isArray(value) ? (value[0] || '') : (value || '');
        }
        return select.value || '';
    },

    collectTakenIds(allSelects, exceptSelect) {
        const taken = new Set();
        (allSelects || []).forEach(sel => {
            if (!sel || sel === exceptSelect || sel.disabled) return;
            const value = this.getSelectValue(sel);
            if (value) taken.add(String(value));
        });
        return taken;
    },

    applyOptionData(opt, m) {
        opt.dataset.motor = m.motorNo ?? m.MotorNo ?? '';
        opt.dataset.battery = m.batteryNo ?? m.BatteryNo ?? '';
        opt.dataset.charger = m.chargerNo ?? m.ChargerNo ?? '';
        opt.dataset.controller = m.controllerNo ?? m.ControllerNo ?? '';
        opt.dataset.converter = m.converterNo ?? m.ConverterNo ?? '';
        opt.dataset.chassis = this.optionLabel(m);
    },

    fillSelect(select, options, config) {
        if (!select) return 0;

        const takenIds = config?.takenIds;
        const currentValue = config?.currentValue != null ? String(config.currentValue) : String(select.value || '');
        const emptyMessage = config?.emptyMessage || 'No chassis in dealer stock for this model/color';

        if (window.KrsSearchableSelect) {
            window.KrsSearchableSelect.destroy(select);
        }

        select.innerHTML = '<option value="">Select chassis</option>';
        let shown = 0;

        (options || []).forEach(m => {
            const vid = this.optionId(m);
            if (!vid) return;
            if (takenIds && takenIds.has(vid) && vid !== currentValue) return;

            const opt = document.createElement('option');
            opt.value = vid;
            opt.textContent = this.optionLabel(m);
            this.applyOptionData(opt, m);
            select.appendChild(opt);
            shown += 1;
        });

        if (shown === 0 && emptyMessage) {
            const empty = document.createElement('option');
            empty.value = '';
            empty.textContent = emptyMessage;
            select.appendChild(empty);
        }

        if (currentValue) {
            select.value = currentValue;
        }

        if (window.KrsSearchableSelect) {
            window.KrsSearchableSelect.init(select);
        }

        return shown;
    },

    setEnabled(select, enabled) {
        if (!select) return;
        select.disabled = !enabled;
        select.required = !!enabled;
        if (select.tomselect) {
            if (enabled) {
                select.tomselect.enable();
            } else {
                select.tomselect.disable();
            }
        }
    },

    async fetchOptions(dealershipId, modelId, colorId) {
        if (!dealershipId || !modelId || !colorId) {
            return [];
        }

        const res = await KrsQueryString.fetchGet('/VehicleMasters/Available', {
            dealershipId: String(dealershipId),
            modelId: String(modelId),
            colorId: String(colorId)
        });

        if (!res.ok) {
            throw new Error('Failed to load chassis from dealer stock.');
        }

        const data = await res.json();
        return Array.isArray(data) ? data : [];
    }
};
