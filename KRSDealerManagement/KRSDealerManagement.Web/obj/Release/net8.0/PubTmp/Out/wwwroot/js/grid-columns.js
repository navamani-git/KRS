(function () {
    var STORAGE_PREFIX = 'krs-grid-cols:';

    function isPaginatedGridCard(cardBody) {
        if (cardBody.querySelector('[aria-label="Pagination"]')) return true;
        if (cardBody.querySelector('.pagination')) return true;
        var bar = cardBody.querySelector('.d-flex.flex-wrap.justify-content-between.align-items-center');
        return bar && /Showing\s+\d+/i.test(bar.textContent);
    }

    function getHeaderRow(table) {
        return table.querySelector('thead tr:not(.grid-column-filters)') || table.querySelector('thead tr');
    }

    function getColumnLabel(th, index) {
        var text = (th.textContent || '').replace(/\s+/g, ' ').trim();
        return text || ('Column ' + (index + 1));
    }

    function resolveGridId(table) {
        if (table.dataset.gridId) return table.dataset.gridId;
        var el = table.querySelector('.grid-combobox[data-grid-id]');
        if (el && el.dataset.gridId) {
            table.dataset.gridId = el.dataset.gridId;
            return el.dataset.gridId;
        }
        var headerRow = getHeaderRow(table);
        return 'cols-' + (headerRow ? headerRow.cells.length : 0);
    }

    function getStorageKey(table) {
        return STORAGE_PREFIX + window.location.pathname + ':' + resolveGridId(table);
    }

    function loadColumnState(table, defaultState) {
        try {
            var raw = sessionStorage.getItem(getStorageKey(table));
            if (!raw) return defaultState.slice();
            var saved = JSON.parse(raw);
            if (!Array.isArray(saved) || saved.length !== defaultState.length) {
                return defaultState.slice();
            }
            return saved.map(function (v) { return !!v; });
        } catch (e) {
            return defaultState.slice();
        }
    }

    function saveColumnState(table, state) {
        try {
            sessionStorage.setItem(getStorageKey(table), JSON.stringify(state));
        } catch (e) {
            // ignore quota / private mode
        }
    }

    function setColumnVisible(table, colIndex, visible) {
        var headerRow = getHeaderRow(table);
        if (headerRow && headerRow.cells[colIndex]) {
            var headerCell = headerRow.cells[colIndex];
            headerCell.style.display = visible ? '' : 'none';
            headerCell.setAttribute('aria-hidden', visible ? 'false' : 'true');
        }

        table.querySelectorAll('tbody tr').forEach(function (row) {
            var cell = row.cells[colIndex];
            if (!cell) return;
            cell.style.display = visible ? '' : 'none';
            cell.setAttribute('aria-hidden', visible ? 'false' : 'true');
        });

        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (filterRow && filterRow.cells[colIndex]) {
            var filterCell = filterRow.cells[colIndex];
            filterCell.style.display = visible ? '' : 'none';
            filterCell.setAttribute('aria-hidden', visible ? 'false' : 'true');
        }
    }

    function applyColumnState(table, state, menu) {
        state.forEach(function (visible, index) {
            setColumnVisible(table, index, visible);
        });

        if (!menu) return;

        menu.querySelectorAll('input[type="checkbox"][data-col-index]').forEach(function (cb) {
            var index = parseInt(cb.getAttribute('data-col-index'), 10);
            if (!isNaN(index) && index < state.length) {
                cb.checked = state[index];
            }
        });
        syncCheckboxDisabledState(menu, state);
    }

    function countVisible(state) {
        return state.reduce(function (n, v) { return n + (v ? 1 : 0); }, 0);
    }

    function findGridTable(cardBody) {
        var wrapped = cardBody.querySelector('.table-responsive > table');
        if (wrapped && wrapped.tHead) return wrapped;

        for (var i = 0; i < cardBody.children.length; i++) {
            var child = cardBody.children[i];
            if (child.tagName === 'TABLE' && child.tHead) return child;
        }

        var fallback = cardBody.querySelector('table');
        return fallback && fallback.tHead ? fallback : null;
    }

    function placeToolbar(toolbar, table, cardBody) {
        var panel = table.closest('.table-responsive');
        if (panel) {
            panel.parentElement.insertBefore(toolbar, panel);
            return;
        }
        cardBody.insertBefore(toolbar, table);
    }

    function notifyLayoutChanged() {
        if (typeof window.krsFitGridScroll === 'function') {
            window.krsFitGridScroll();
        }
        document.dispatchEvent(new CustomEvent('grid-layout-changed'));
    }

    function syncCheckboxDisabledState(menu, state) {
        var visible = countVisible(state);
        menu.querySelectorAll('input[type="checkbox"]').forEach(function (cb) {
            if (cb.checked && visible <= 1) {
                cb.disabled = true;
            } else {
                cb.disabled = false;
            }
        });
    }

    function initTable(table) {
        if (table.dataset.gridColumnsInit === '1') return;

        var cardBody = table.closest('.card-body');
        if (!cardBody || !isPaginatedGridCard(cardBody)) return;

        var headerRow = getHeaderRow(table);
        if (!headerRow || headerRow.cells.length === 0) return;

        table.dataset.gridColumnsInit = '1';
        table.classList.add('grid-data-table');

        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (filterRow) {
            Array.from(filterRow.cells).forEach(function (cell, index) {
                cell.setAttribute('data-grid-col-index', String(index));
            });
        }

        var headers = Array.from(headerRow.cells);
        var defaultState = headers.map(function () { return true; });
        var state = loadColumnState(table, defaultState);

        var toolbar = document.createElement('div');
        toolbar.className = 'grid-column-toolbar d-flex justify-content-end align-items-center gap-2 px-3 py-2 border-bottom bg-light';

        var hint = document.createElement('small');
        hint.className = 'grid-toolbar-hint me-auto d-none d-md-inline';
        hint.textContent = 'Column layout is remembered on this page; filtering will not reset it.';

        var dropdown = document.createElement('div');
        dropdown.className = 'dropdown';

        var toggleBtn = document.createElement('button');
        toggleBtn.type = 'button';
        toggleBtn.className = 'btn btn-sm btn-outline-secondary dropdown-toggle';
        toggleBtn.setAttribute('data-bs-toggle', 'dropdown');
        toggleBtn.setAttribute('data-bs-auto-close', 'outside');
        toggleBtn.setAttribute('aria-expanded', 'false');
        toggleBtn.innerHTML = '<i class="bi bi-layout-three-columns"></i> Columns';

        var menu = document.createElement('div');
        menu.className = 'dropdown-menu dropdown-menu-end p-2 grid-column-menu';

        var showAllBtn = document.createElement('button');
        showAllBtn.type = 'button';
        showAllBtn.className = 'btn btn-sm btn-link w-100 text-start mb-1';
        showAllBtn.textContent = 'Show all columns';
        showAllBtn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            state.forEach(function (_, i) {
                state[i] = true;
            });
            applyColumnState(table, state, menu);
            saveColumnState(table, state);
            notifyLayoutChanged();
        });
        menu.appendChild(showAllBtn);
        menu.appendChild(document.createElement('hr')).className = 'dropdown-divider my-1';

        headers.forEach(function (th, index) {
            th.setAttribute('data-grid-col-index', String(index));

            var item = document.createElement('label');
            item.className = 'dropdown-item d-flex align-items-center gap-2 py-1 grid-column-menu-item';
            item.addEventListener('click', function (e) {
                e.stopPropagation();
            });

            var cb = document.createElement('input');
            cb.type = 'checkbox';
            cb.className = 'form-check-input mt-0';
            cb.checked = state[index];
            cb.setAttribute('data-col-index', String(index));

            var span = document.createElement('span');
            span.className = 'small';
            span.textContent = getColumnLabel(th, index);

            cb.addEventListener('change', function () {
                if (!cb.checked && countVisible(state) <= 1) {
                    cb.checked = true;
                    return;
                }
                state[index] = cb.checked;
                setColumnVisible(table, index, cb.checked);
                syncCheckboxDisabledState(menu, state);
                saveColumnState(table, state);
                notifyLayoutChanged();
            });

            item.appendChild(cb);
            item.appendChild(span);
            menu.appendChild(item);
        });

        dropdown.appendChild(toggleBtn);
        dropdown.appendChild(menu);
        toolbar.appendChild(hint);

        if (table.querySelector('thead tr.grid-column-filters')) {
            var clearFiltersBtn = document.createElement('button');
            clearFiltersBtn.type = 'button';
            clearFiltersBtn.className = 'btn btn-sm btn-outline-secondary grid-clear-column-filters';
            clearFiltersBtn.setAttribute('data-filter-form', 'gridFilterForm');
            clearFiltersBtn.title = 'Clear all column filters and reload grid';
            clearFiltersBtn.innerHTML = '<i class="bi bi-x-circle"></i> Clear filters';
            toolbar.appendChild(clearFiltersBtn);
        }

        toolbar.appendChild(dropdown);

        placeToolbar(toolbar, table, cardBody);

        applyColumnState(table, state, menu);
        notifyLayoutChanged();
    }

    function initAll() {
        var seen = new Set();
        document.querySelectorAll('.content .card .card-body').forEach(function (cardBody) {
            if (!isPaginatedGridCard(cardBody)) return;
            var table = findGridTable(cardBody);
            if (!table || seen.has(table)) return;
            seen.add(table);
            initTable(table);
        });

        if (typeof window.krsFitGridScroll === 'function') {
            window.krsFitGridScroll();
        }
        if (typeof window.krsInitGridResize === 'function') {
            window.krsInitGridResize();
            setTimeout(window.krsInitGridResize, 120);
        }
        if (typeof window.krsSyncClearFilterButtons === 'function') {
            window.krsSyncClearFilterButtons();
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    window.addEventListener('load', initAll);
})();
