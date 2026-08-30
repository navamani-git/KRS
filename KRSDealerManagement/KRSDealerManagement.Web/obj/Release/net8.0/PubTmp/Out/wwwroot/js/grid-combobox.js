(function () {
    var FORM_ID = 'gridFilterForm';
    var PAGE_CONTEXT_PARAMS = [
        'status',
        'dealershipId',
        'dealershipLocation',
        'subdealerId',
        'bookingPhaseOnly',
        'fromDate',
        'toDate',
        'accountId',
        'id',
        'isActive',
        'searchTerm'
    ];
    var activeInput = null;
    var suggestEl = null;
    var loadTimer = null;
    var blurTimer = null;

    function getTargetForm(el) {
        var formId = el.getAttribute('form') || el.dataset.filterFormId || FORM_ID;
        return document.getElementById(formId);
    }

    function getFieldName(el) {
        return el.dataset.filterName || el.getAttribute('name') || '';
    }

    function readComboboxValue(el) {
        return (el.value || '').toString();
    }

    function isFilterCellVisible(el) {
        var cell = el.closest('th, td');
        if (!cell) return true;
        if (cell.getAttribute('aria-hidden') === 'true') return false;
        return cell.style.display !== 'none' && !cell.hasAttribute('hidden');
    }

    function appendFormFields(params, form) {
        if (!form) return;

        Array.from(form.elements).forEach(function (field) {
            if (!field.name) return;
            if (field.name.indexOf('cf_') === 0) return;
            if (field.disabled) return;
            if (field.type === 'submit' || field.type === 'button' || field.type === 'file') return;

            if (field.type === 'checkbox') {
                if (field.checked) params.set(field.name, field.value || 'true');
                else params.delete(field.name);
                return;
            }

            if (field.type === 'radio') {
                if (field.checked) params.set(field.name, field.value);
                return;
            }

            var val = (field.value || '').trim();
            if (val) params.set(field.name, val);
            else params.delete(field.name);
        });
    }

    function collectColumnFilters(form, changedEl, clearFieldName) {
        var cf = {};
        var formId = form ? form.id : FORM_ID;
        var current = new URLSearchParams(window.location.search);

        current.forEach(function (value, key) {
            if (key.indexOf('cf_') === 0 && (value || '').trim()) {
                cf[key] = value.trim();
            }
        });

        if (clearFieldName) {
            delete cf[clearFieldName];
        }

        document.querySelectorAll('[form="' + formId + '"]').forEach(function (field) {
            if (!field.name || field.name.indexOf('cf_') !== 0) return;
            if (field.classList.contains('grid-combobox')) return;
            if (!isFilterCellVisible(field)) return;

            var val = (field.value || '').trim();
            if (val) cf[field.name] = val;
            else delete cf[field.name];
        });

        document.querySelectorAll('.grid-combobox').forEach(function (el) {
            var name = getFieldName(el);
            if (!name) return;
            if (form && getTargetForm(el) !== form) return;

            var val = readComboboxValue(el).trim();
            if (val) {
                cf[name] = val;
            } else if (changedEl === el || clearFieldName === name) {
                delete cf[name];
            }
        });

        return cf;
    }

    function applyGridFilters(changedEl, clearFieldName) {
        var form = document.getElementById(FORM_ID);
        var params = new URLSearchParams();
        var current = new URLSearchParams(window.location.search);

        if (current.has('pageSize')) {
            params.set('pageSize', current.get('pageSize'));
        }

        appendFormFields(params, form);

        PAGE_CONTEXT_PARAMS.forEach(function (name) {
            if (!params.has(name)) {
                var val = readPageContextParam(name);
                if (val) params.set(name, val);
            }
        });

        var cf = collectColumnFilters(form, changedEl, clearFieldName);
        Object.keys(cf).forEach(function (key) {
            params.set(key, cf[key]);
        });

        var qs = params.toString();
        window.location.assign(window.location.pathname + (qs ? '?' + qs : ''));
    }

    function clearGridColumnFilters(formId) {
        formId = formId || FORM_ID;
        var form = document.getElementById(formId);
        var params = new URLSearchParams();
        var current = new URLSearchParams(window.location.search);

        if (current.has('pageSize')) {
            params.set('pageSize', current.get('pageSize'));
        }

        appendFormFields(params, form);

        var qs = params.toString();
        window.location.assign(window.location.pathname + (qs ? '?' + qs : ''));
    }

    function hasActiveColumnFilters() {
        var params = new URLSearchParams(window.location.search);
        var active = false;
        params.forEach(function (value, key) {
            if (key.indexOf('cf_') === 0 && (value || '').trim()) active = true;
        });
        if (active) return true;

        document.querySelectorAll('[form="' + FORM_ID + '"]').forEach(function (field) {
            if (!field.name || field.name.indexOf('cf_') !== 0) return;
            if (field.classList.contains('grid-combobox')) return;
            if ((field.value || '').trim()) active = true;
        });

        document.querySelectorAll('.grid-combobox').forEach(function (el) {
            if (readComboboxValue(el).trim()) active = true;
        });

        return active;
    }

    function syncClearFilterButtons() {
        var active = hasActiveColumnFilters();
        document.querySelectorAll('.grid-clear-column-filters').forEach(function (btn) {
            btn.disabled = false;
            btn.classList.toggle('opacity-75', !active);
            btn.title = active
                ? 'Clear all column filters and reload grid'
                : 'Clear column filter inputs and reload grid';
        });
    }

    function readPageContextParam(name) {
        var form = document.getElementById(FORM_ID);
        var val = '';

        if (form && form.elements[name]) {
            val = (form.elements[name].value || '').trim();
        }

        if (!val) {
            var urlParams = new URLSearchParams(window.location.search);
            if (urlParams.has(name)) {
                val = (urlParams.get(name) || '').trim();
            }
        }

        return val;
    }

    function buildSuggestContextUrl() {
        var params = new URLSearchParams();
        PAGE_CONTEXT_PARAMS.forEach(function (name) {
            var val = readPageContextParam(name);
            if (val) params.set(name, val);
        });
        return params.toString();
    }

    function resolveGridId(el) {
        if (el.dataset.gridId) return el.dataset.gridId;
        var table = el.closest('table[data-grid-id]');
        return table ? table.dataset.gridId : '';
    }

    function getFilterCell(input) {
        return input.closest('th[data-grid-col-index], th[data-filter-key]') || input.closest('th');
    }

    function matchesQuery(value, query) {
        if (!query) return true;
        return value.toLowerCase().indexOf(query.toLowerCase()) >= 0;
    }

    function getDistinctValuesUrl() {
        return window.krsGridDistinctValuesUrl || '/Grids/DistinctValues';
    }

    function fetchDistinctValues(el, query) {
        var gridId = resolveGridId(el);
        var column = el.dataset.column;
        if (!gridId || !column) return Promise.resolve([]);

        var url = getDistinctValuesUrl()
            + '?grid=' + encodeURIComponent(gridId)
            + '&column=' + encodeURIComponent(column)
            + '&search=' + encodeURIComponent(query || '');
        var ctx = buildSuggestContextUrl();
        if (ctx) url += '&' + ctx;

        return fetch(url, {
            headers: { 'Accept': 'application/json' },
            credentials: 'same-origin',
            krsNoLoader: true
        })
            .then(function (r) {
                if (!r.ok) return [];
                return r.json();
            })
            .then(function (items) {
                return Array.isArray(items) ? items : [];
            })
            .catch(function () { return []; });
    }

    function ensureInputWrap(input) {
        var wrap = input.parentElement;
        if (wrap && wrap.classList.contains('grid-filter-input-wrap')) return wrap;

        wrap = document.createElement('div');
        wrap.className = 'grid-filter-input-wrap';
        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(input);
        return wrap;
    }

    function bindSuggestScrollWheel(box) {
        if (box.dataset.scrollWheelBound === '1') return;
        box.dataset.scrollWheelBound = '1';

        box.addEventListener('wheel', function (e) {
            var scrollEl = box.querySelector('.grid-filter-suggest-scroll') || box;
            if (scrollEl.scrollHeight <= scrollEl.clientHeight + 1) return;

            var delta = e.deltaY;
            var atTop = scrollEl.scrollTop <= 0;
            var atBottom = scrollEl.scrollTop + scrollEl.clientHeight >= scrollEl.scrollHeight - 1;

            if ((delta < 0 && !atTop) || (delta > 0 && !atBottom)) {
                e.stopPropagation();
            }
        }, { passive: true });
    }

    function ensureSuggestElement() {
        if (suggestEl) return suggestEl;

        suggestEl = document.createElement('div');
        suggestEl.className = 'grid-filter-suggest';
        suggestEl.setAttribute('role', 'listbox');
        suggestEl.hidden = true;
        document.body.appendChild(suggestEl);
        bindSuggestScrollWheel(suggestEl);
        return suggestEl;
    }

    function createSuggestScrollContainer() {
        var scroll = document.createElement('div');
        scroll.className = 'grid-filter-suggest-scroll';
        scroll.setAttribute('role', 'presentation');
        return scroll;
    }

    function appendSuggestItem(scroll, input, value, extraClass) {
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'grid-filter-suggest-item' + (extraClass ? ' ' + extraClass : '');
        btn.textContent = value;
        btn.addEventListener('mousedown', function (e) {
            e.preventDefault();
            input.value = value;
            hideSuggest();
            applyGridFilters(input, null);
            syncClearFilterButtons();
        });
        scroll.appendChild(btn);
    }

    function getScrollPanel(input) {
        return input ? input.closest('.grid-scroll-panel, .table-responsive') : null;
    }

    function ensureGridHeaderLayout(input) {
        var panel = getScrollPanel(input);
        if (!panel) return;

        var headerRow = panel.querySelector('thead tr:not(.grid-column-filters)');
        var filterRow = panel.querySelector('thead tr.grid-column-filters');
        if (headerRow) {
            panel.style.setProperty('--grid-header-row-height', headerRow.getBoundingClientRect().height + 'px');
        }
        if (filterRow) {
            panel.style.setProperty('--grid-filter-row-height', filterRow.getBoundingClientRect().height + 'px');
        }
    }

    function positionSuggestFixed(input) {
        if (!suggestEl || suggestEl.hidden || !input) return;

        if (suggestEl.parentElement !== document.body) {
            document.body.appendChild(suggestEl);
        }

        ensureGridHeaderLayout(input);

        var inputRect = input.getBoundingClientRect();
        var cell = getFilterCell(input);
        var cellRect = cell ? cell.getBoundingClientRect() : null;
        var width = Math.max(inputRect.width, cellRect ? cellRect.width : 0, 168);
        var left = Math.min(Math.max(8, inputRect.left), window.innerWidth - width - 8);
        var maxHeight = 220;
        var top = inputRect.bottom + 2;

        if (top + maxHeight > window.innerHeight - 8 && inputRect.top > maxHeight + 12) {
            top = Math.max(8, inputRect.top - maxHeight - 2);
        }

        suggestEl.style.position = 'fixed';
        suggestEl.style.left = left + 'px';
        suggestEl.style.top = top + 'px';
        suggestEl.style.width = width + 'px';
        suggestEl.style.minWidth = '168px';
        suggestEl.style.maxHeight = maxHeight + 'px';
        suggestEl.style.zIndex = '10050';
        suggestEl.style.display = 'block';
    }

    function syncOpenSuggest(input) {
        if (!input || !suggestEl || suggestEl.hidden) return;
        positionSuggestFixed(input);
    }

    function scheduleSyncOpenSuggest(input) {
        ensureGridHeaderLayout(input);
        positionSuggestFixed(input);

        requestAnimationFrame(function () {
            positionSuggestFixed(input);
            requestAnimationFrame(function () {
                positionSuggestFixed(input);
            });
        });
    }

    function showSuggestBox(input) {
        if (suggestEl.parentElement !== document.body) {
            document.body.appendChild(suggestEl);
        }
        suggestEl.hidden = false;
        suggestEl.removeAttribute('hidden');
        scheduleSyncOpenSuggest(input);
    }

    function hideSuggest() {
        if (suggestEl) {
            suggestEl.hidden = true;
            suggestEl.style.display = 'none';
            suggestEl.innerHTML = '';
        }
        activeInput = null;
    }

    function renderSuggest(input, values, query, loading) {
        var box = ensureSuggestElement();
        if (!box) return;

        activeInput = input;
        box.innerHTML = '';

        if (loading) {
            var loadingEl = document.createElement('div');
            loadingEl.className = 'grid-filter-suggest-empty';
            loadingEl.textContent = 'Loading suggestions...';
            box.appendChild(loadingEl);
        } else if (!values.length) {
            var empty = document.createElement('div');
            empty.className = 'grid-filter-suggest-empty';
            empty.textContent = query ? 'No matches found' : 'Type to filter';
            box.appendChild(empty);
        } else {
            var scroll = createSuggestScrollContainer();
            values.slice(0, 100).forEach(function (value) {
                appendSuggestItem(scroll, input, value, '');
            });
            box.appendChild(scroll);
        }

        if (!loading && query && !values.some(function (v) { return v.toLowerCase() === query.toLowerCase(); })) {
            var createScroll = box.querySelector('.grid-filter-suggest-scroll') || createSuggestScrollContainer();
            if (!createScroll.parentElement) {
                box.appendChild(createScroll);
            }

            var create = document.createElement('button');
            create.type = 'button';
            create.className = 'grid-filter-suggest-item grid-filter-suggest-create';
            create.innerHTML = 'Use: <strong></strong>';
            create.querySelector('strong').textContent = query;
            create.addEventListener('mousedown', function (e) {
                e.preventDefault();
                input.value = query;
                hideSuggest();
                applyGridFilters(input, null);
                syncClearFilterButtons();
            });
            createScroll.appendChild(create);
        }

        showSuggestBox(input);
    }

    function updateSuggest(input) {
        var query = readComboboxValue(input).trim();
        var requestId = (input.dataset.suggestRequestId = String((parseInt(input.dataset.suggestRequestId, 10) || 0) + 1));

        renderSuggest(input, [], query, true);

        clearTimeout(loadTimer);
        loadTimer = setTimeout(function () {
            fetchDistinctValues(input, query).then(function (items) {
                if (input.dataset.suggestRequestId !== requestId) return;
                renderSuggest(input, items, query, false);
            });
        }, 150);
    }

    function initCombobox(el) {
        if (el.dataset.comboboxInit === '1') return;
        if (!el.dataset.column) return;
        if (!resolveGridId(el)) return;

        el.dataset.comboboxInit = '1';
        el.setAttribute('autocomplete', 'off');
        el.setAttribute('spellcheck', 'false');

        var fieldName = el.getAttribute('name');
        if (fieldName) {
            el.dataset.filterName = fieldName;
            el.removeAttribute('name');
        }

        var form = getTargetForm(el);
        if (form && form.id) {
            el.dataset.filterFormId = form.id;
        }

        var cell = getFilterCell(el);
        if (cell) {
            cell.classList.add('grid-filter-cell');
        }

        ensureInputWrap(el);

        el.addEventListener('focus', function () {
            hideSuggest();
            ensureGridHeaderLayout(el);
            updateSuggest(el);
        });

        el.addEventListener('click', function () {
            updateSuggest(el);
        });

        el.addEventListener('input', function () {
            updateSuggest(el);
            syncClearFilterButtons();
        });

        el.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                hideSuggest();
                return;
            }
            if (e.key === 'Enter') {
                e.preventDefault();
                hideSuggest();
                applyGridFilters(el, readComboboxValue(el).trim() ? null : getFieldName(el));
                syncClearFilterButtons();
            }
        });

        el.addEventListener('blur', function () {
            clearTimeout(blurTimer);
            blurTimer = setTimeout(function () {
                if (suggestEl && suggestEl.contains(document.activeElement)) return;
                hideSuggest();
            }, 200);
        });

        var panel = getScrollPanel(el);
        if (panel) {
            panel.addEventListener('scroll', function () {
                if (activeInput === el) positionSuggestFixed(el);
            }, { passive: true });
        }
    }

    function cleanupLegacySuggestElements() {
        document.querySelectorAll('th > .grid-filter-suggest').forEach(function (node) {
            node.remove();
        });
    }

    function initAll() {
        cleanupLegacySuggestElements();
        document.querySelectorAll('.grid-combobox').forEach(initCombobox);
        syncClearFilterButtons();
    }

    window.krsApplyGridFilters = function () {
        applyGridFilters(null, null);
    };

    window.krsClearGridColumnFilters = clearGridColumnFilters;
    window.krsSyncClearFilterButtons = syncClearFilterButtons;

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.grid-clear-column-filters');
        if (!btn) return;
        e.preventDefault();
        clearGridColumnFilters(btn.dataset.filterForm || FORM_ID);
    });

    document.addEventListener('mousedown', function (e) {
        if (e.target.closest('.grid-filter-suggest') || e.target.closest('.grid-combobox')) return;
        hideSuggest();
    });

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.id !== FORM_ID) return;
        e.preventDefault();
        applyGridFilters(null, null);
    }, true);

    document.addEventListener('change', function (e) {
        var t = e.target;
        if (!t || !t.name || t.name.indexOf('cf_') !== 0) return;
        if (t.classList.contains('grid-fixed-select') || t.type === 'date') {
            applyGridFilters(t, (t.value || '').trim() ? null : t.name);
            syncClearFilterButtons();
        }
    });

    document.addEventListener('grid-layout-changed', function () {
        if (activeInput) scheduleSyncOpenSuggest(activeInput);
    });

    document.addEventListener('grid-scroll-ready', function () {
        if (activeInput) scheduleSyncOpenSuggest(activeInput);
    });

    window.addEventListener('scroll', function () {
        if (activeInput) positionSuggestFixed(activeInput);
    }, true);

    window.addEventListener('resize', function () {
        if (activeInput) positionSuggestFixed(activeInput);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    window.addEventListener('load', initAll);
})();
