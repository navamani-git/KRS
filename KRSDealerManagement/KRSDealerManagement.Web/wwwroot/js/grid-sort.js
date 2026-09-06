(function () {
    function getHeaderRow(table) {
        return table.querySelector('thead tr:not(.grid-column-filters)') || table.querySelector('thead tr');
    }

    function currentSort() {
        return window.krsGridSort || { column: '', dir: '' };
    }

    function setHidden(form, name, value) {
        var el = form.elements[name];
        if (!el || el.length) {
            el = form.querySelector('input[type="hidden"][name="' + name + '"]');
        }
        if (!el) {
            el = document.createElement('input');
            el.type = 'hidden';
            el.name = name;
            form.appendChild(el);
        }
        el.value = value || '';
    }

    function ensureSortFields() {
        var form = document.getElementById('gridFilterForm');
        if (!form) return null;
        var sort = currentSort();
        setHidden(form, 'sort', sort.column);
        setHidden(form, 'dir', sort.dir);
        return form;
    }

    function isSortableKey(key) {
        if (!key || key.charAt(0) === '_') return false;
        return true;
    }

    function headerLooksLikeActions(th) {
        var text = (th.textContent || '').replace(/\s+/g, ' ').trim().toLowerCase();
        return text === 'action' || text === 'actions';
    }

    function resolveColumnKey(table, index, th) {
        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (filterRow && filterRow.cells[index]) {
            var key = filterRow.cells[index].getAttribute('data-filter-key');
            if (isSortableKey(key)) return key;
        }
        if (th.classList.contains('grid-col-rownum') || th.classList.contains('grid-col-actions'))
            return '';
        if (headerLooksLikeActions(th)) return '';
        return '';
    }

    function iconFor(key) {
        var sort = currentSort();
        if (!key || !sort.column || sort.column.toLowerCase() !== key.toLowerCase())
            return 'bi-arrow-down-up';
        return (sort.dir || '').toLowerCase() === 'desc' ? 'bi-sort-down' : 'bi-sort-up';
    }

    function applySort(key) {
        var sort = currentSort();
        var nextDir = 'asc';
        if (sort.column && sort.column.toLowerCase() === key.toLowerCase() && (sort.dir || 'asc').toLowerCase() !== 'desc')
            nextDir = 'desc';

        var form = ensureSortFields();
        if (form) {
            setHidden(form, 'sort', key);
            setHidden(form, 'dir', nextDir);
        }
        window.krsGridSort = { column: key, dir: nextDir };

        if (typeof window.krsApplyGridFilters === 'function') {
            window.krsApplyGridFilters(null, null);
            return;
        }

        var params = new URLSearchParams();
        if (form) {
            Array.from(form.elements).forEach(function (field) {
                if (!field.name || field.disabled) return;
                if (field.type === 'submit' || field.type === 'button' || field.type === 'file') return;
                var val = (field.value || '').trim();
                if (val) params.set(field.name, val);
            });
        }
        params.set('sort', key);
        params.set('dir', nextDir);
        params.delete('page');
        if (typeof window.krsNavigateGridParams === 'function') {
            window.krsNavigateGridParams(params);
            return;
        }
        window.location.assign(window.location.pathname + '?' + params.toString());
    }

    function initTable(table) {
        if (table.dataset.gridSortInit === '1') return;
        var headerRow = getHeaderRow(table);
        if (!headerRow) return;
        table.dataset.gridSortInit = '1';

        Array.from(headerRow.cells).forEach(function (th, index) {
            var key = resolveColumnKey(table, index, th);
            if (!isSortableKey(key)) return;

            th.classList.add('grid-th-sortable');
            var btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'grid-col-sort';
            btn.setAttribute('title', 'Sort this column');
            btn.setAttribute('aria-label', 'Sort column');
            btn.innerHTML = '<i class="bi ' + iconFor(key) + '"></i>';
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                applySort(key);
            });
            th.appendChild(btn);
        });
    }

    function initAll() {
        ensureSortFields();
        document.querySelectorAll('table.grid-excel-table, table.grid-data-table').forEach(initTable);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
    window.addEventListener('load', initAll);
})();
