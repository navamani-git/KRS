window.KrsStaffRoleMenu = {
    applyDefaults(defaults) {
        document.querySelectorAll('.menu-access-select').forEach(sel => {
            const key = sel.dataset.menuKey;
            sel.value = defaults && defaults[key] ? String(defaults[key]) : '0';
        });
        this.refreshSectionBadges();
    },

    refreshSectionBadges() {
        document.querySelectorAll('[data-role-menu-section]').forEach(section => {
            const selects = section.querySelectorAll('.menu-access-select');
            let enabled = 0;
            selects.forEach(sel => {
                if (parseInt(sel.value, 10) > 0) enabled++;
            });
            const badge = section.querySelector('[data-section-badge]');
            if (badge) badge.textContent = `${enabled} / ${selects.length} enabled`;
        });
    },

    expandAll() {
        document.querySelectorAll('#roleMenuAccordion .accordion-collapse').forEach(el => {
            bootstrap.Collapse.getOrCreateInstance(el, { toggle: false }).show();
        });
    },

    collapseAll() {
        document.querySelectorAll('#roleMenuAccordion .accordion-collapse.show').forEach(el => {
            bootstrap.Collapse.getOrCreateInstance(el, { toggle: false }).hide();
        });
    },

    bind() {
        document.querySelectorAll('.menu-access-select').forEach(sel => {
            sel.addEventListener('change', () => this.refreshSectionBadges());
        });
        document.getElementById('applyTemplateDefaults')?.addEventListener('click', () => {
            const raw = document.getElementById('roleMenuDefaults')?.textContent;
            if (!raw) return;
            try {
                this.applyDefaults(JSON.parse(raw));
            } catch { /* ignore */ }
        });
        document.getElementById('roleMenuExpandAll')?.addEventListener('click', e => {
            e.preventDefault();
            this.expandAll();
        });
        document.getElementById('roleMenuCollapseAll')?.addEventListener('click', e => {
            e.preventDefault();
            this.collapseAll();
        });
        this.refreshSectionBadges();
    }
};
