(function () {
    var FORM_ID = 'gridFilterForm';
    var PAGE_CONTEXT_PARAMS = [
        'status',
        'dealershipId',
        'dealershipLocation',
        'subdealerId',
        'bookingPhaseOnly',
        'subsidyIdPendingOnly',
        'subsidyDocsPendingOnly',
        'registeredAwaitingPlateOnly',
        'bookedToCustomerView',
        'fromDate',
        'toDate',
        'accountId',
        'id',
        'isActive',
        'searchTerm',
        'sort',
        'dir'
    ];
    var activeInput = null;
    var suggestEl = null;
    var cachedSuggestWidth = null;
    var lastSuggestValues = [];
    var loadTimer = null;
    var blurTimer = null;
    var suggestGeneration = 0;

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

        params.delete('page');
        navigateGridParams(params);
    }

    function navigateGridParams(params) {
        var path = window.location.pathname;
        var qs = params.toString();
        if (window.KrsQueryString && typeof window.KrsQueryString.pack === 'function' && typeof window.krsNavigate === 'function') {
            var values = {};
            params.forEach(function (value, key) {
                values[key] = value;
            });
            window.KrsQueryString.pack(values).then(function (q) {
                window.krsNavigate(path + '?q=' + encodeURIComponent(q), 'Loading...');
            }).catch(function () {
                window.location.assign(path + (qs ? '?' + qs : ''));
            });
            return;
        }
        window.location.assign(path + (qs ? '?' + qs : ''));
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

        PAGE_CONTEXT_PARAMS.forEach(function (name) {
            if (!params.has(name)) {
                var val = readPageContextParam(name);
                if (val) params.set(name, val);
            }
        });

        navigateGridParams(params);
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
        if (wrap && wrap.classList.contains('grid-filter-input-wrap')) {
            syncClearButton(input);
            return wrap;
        }

        wrap = document.createElement('div');
        wrap.className = 'grid-filter-input-wrap';
        input.parentNode.insertBefore(wrap, input);
        wrap.appendChild(input);

        var clearBtn = document.createElement('button');
        clearBtn.type = 'button';
        clearBtn.className = 'btn btn-outline-secondary grid-filter-clear-btn';
        clearBtn.title = 'Clear this column filter';
        clearBtn.setAttribute('aria-label', 'Clear this column filter');
        clearBtn.innerHTML = '<i class="bi bi-x-lg"></i>';
        clearBtn.addEventListener('mousedown', function (e) {
            e.preventDefault();
        });
        clearBtn.addEventListener('click', function (e) {
            e.preventDefault();
            input.value = '';
            hideSuggest();
            applyGridFilters(input, getFieldName(input));
            syncClearFilterButtons();
            syncClearButton(input);
            input.focus();
        });
        wrap.appendChild(clearBtn);
        syncClearButton(input);
        return wrap;
    }

    function syncClearButton(input) {
        var wrap = input ? input.closest('.grid-filter-input-wrap') : null;
        if (!wrap) return;
        var btn = wrap.querySelector('.grid-filter-clear-btn');
        if (!btn) return;
        var hasValue = readComboboxValue(input).trim().length > 0;
        btn.hidden = !hasValue;
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
            syncClearButton(input);
            applyGridFilters(input, null);
            syncClearFilterButtons();
        });
        scroll.appendChild(btn);
    }

    function getScrollPanel(input) {
        return input ? input.closest('.grid-scroll-panel, .table-responsive') : null;
    }

    function getViewportBounds() {
        var vv = window.visualViewport;
        if (!vv) {
            return {
                top: 8,
                left: 8,
                right: window.innerWidth - 8,
                bottom: window.innerHeight - 8
            };
        }

        return {
            top: vv.offsetTop + 8,
            left: vv.offsetLeft + 8,
            right: vv.offsetLeft + vv.width - 8,
            bottom: vv.offsetTop + vv.height - 8
        };
    }

    function getSuggestSpace(input) {
        var wrap = input.closest('.grid-filter-input-wrap') || input;
        var rect = wrap.getBoundingClientRect();
        var viewport = getViewportBounds();
        var bounds = {
            top: viewport.top,
            left: viewport.left,
            right: viewport.right,
            bottom: viewport.bottom
        };

        var panel = getScrollPanel(input);
        if (panel) {
            var panelRect = panel.getBoundingClientRect();
            bounds.left = Math.max(bounds.left, panelRect.left + 2);
            bounds.right = Math.min(bounds.right, panelRect.right - 2);
        }

        return {
            rect: rect,
            bounds: bounds,
            spaceBelow: Math.max(0, viewport.bottom - rect.bottom - 2)
        };
    }

    function markSuggestFilterCell(input) {
        var cell = getFilterCell(input);
        document.querySelectorAll('th.grid-filter-cell.is-suggest-open').forEach(function (node) {
            if (node !== cell) {
                node.classList.remove('is-suggest-open');
            }
        });
        if (cell) {
            cell.classList.add('is-suggest-open');
        }
        return cell;
    }

    function clearSuggestOpenWrap() {
        document.querySelectorAll('th.grid-filter-cell.is-suggest-open').forEach(function (cell) {
            cell.classList.remove('is-suggest-open');
        });
    }

    function rectsOverlap(a, b, padding) {
        padding = padding || 0;
        return a.right > b.left + padding
            && a.left < b.right - padding
            && a.bottom > b.top + padding
            && a.top < b.bottom - padding;
    }

    function isSuggestAnchorVisible(input) {
        if (!input || !input.isConnected) return false;

        var anchor = input.closest('.grid-filter-input-wrap') || input;
        var rect = anchor.getBoundingClientRect();
        if (rect.width < 1 || rect.height < 1) return false;

        var panel = getScrollPanel(input);
        if (panel) {
            var panelRect = panel.getBoundingClientRect();
            if (!rectsOverlap(rect, panelRect, 2)) {
                return false;
            }
        }

        var viewport = {
            left: 0,
            top: 0,
            right: window.innerWidth,
            bottom: window.innerHeight
        };
        if (!rectsOverlap(rect, viewport, 4)) {
            return false;
        }

        return true;
    }

    function dismissSuggestOnScroll() {
        if (!activeInput || !suggestEl || suggestEl.hidden) return;
        if (!isSuggestAnchorVisible(activeInput)) {
            hideSuggest();
            return;
        }
        layoutSuggest(activeInput, false);
    }

    function bindGridScrollDismiss() {
        document.querySelectorAll('.grid-scroll-panel, .card-body .table-responsive').forEach(function (panel) {
            if (panel.dataset.suggestScrollBound === '1') return;
            panel.dataset.suggestScrollBound = '1';
            panel.addEventListener('scroll', dismissSuggestOnScroll, { passive: true });
        });
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

    function measureTextWidth(text, font) {
        if (!text) return 0;
        var canvas = measureTextWidth._canvas;
        if (!canvas) {
            canvas = document.createElement('canvas');
            measureTextWidth._canvas = canvas;
        }
        var ctx = canvas.getContext('2d');
        ctx.font = font || '500 15px system-ui, -apple-system, "Segoe UI", Roboto, sans-serif';
        return ctx.measureText(text).width;
    }

    function measureSuggestWidth(input, anchorRect, cellRect, textValues) {
        var inputRect = input.getBoundingClientRect();
        var anchorWidth = Math.max(
            cellRect ? cellRect.width : 0,
            anchorRect ? anchorRect.width : 0,
            inputRect.width,
            120
        );
        var font = window.getComputedStyle(input).font;
        var contentWidth = anchorWidth;
        var values = textValues && textValues.length ? textValues : lastSuggestValues;

        values.forEach(function (text) {
            var label = (text || '').toString().trim();
            if (!label) return;
            contentWidth = Math.max(contentWidth, measureTextWidth(label, font) + 32);
        });

        if (!values.length) {
            suggestEl.querySelectorAll('.grid-filter-suggest-item').forEach(function (item) {
                var label = (item.textContent || '').trim();
                if (!label) return;
                contentWidth = Math.max(contentWidth, measureTextWidth(label, font) + 32);
            });
        }

        var query = readComboboxValue(input).trim();
        if (query) {
            contentWidth = Math.max(contentWidth, measureTextWidth('Use: ' + query, font) + 32);
        }

        return Math.min(Math.max(contentWidth, anchorWidth, 168), window.innerWidth - 16);
    }

    function layoutSuggest(input, recalculateWidth) {
        if (!suggestEl || suggestEl.hidden || !input) return;

        if (!isSuggestAnchorVisible(input)) {
            hideSuggest();
            return;
        }

        markSuggestFilterCell(input);

        var space = getSuggestSpace(input);
        var rect = space.rect;
        var spaceBelow = space.spaceBelow;

        if (spaceBelow < 12) {
            hideSuggest();
            return;
        }

        if (suggestEl.parentElement !== document.body) {
            document.body.appendChild(suggestEl);
        }

        if (recalculateWidth || cachedSuggestWidth == null) {
            var cell = getFilterCell(input);
            var cellRect = cell ? cell.getBoundingClientRect() : null;
            cachedSuggestWidth = measureSuggestWidth(input, rect, cellRect);
        }

        var width = Math.min(
            cachedSuggestWidth,
            Math.max(168, space.bounds.right - space.bounds.left)
        );
        width = Math.max(width, rect.width, 168);

        var left = Math.max(space.bounds.left, Math.min(rect.left, space.bounds.right - width));
        var maxHeight = Math.min(220, spaceBelow);
        var top = rect.bottom + 2;

        suggestEl.classList.remove('is-above');
        suggestEl.style.position = 'fixed';
        suggestEl.style.top = top + 'px';
        suggestEl.style.bottom = 'auto';
        suggestEl.style.left = left + 'px';
        suggestEl.style.right = 'auto';
        suggestEl.style.width = width + 'px';
        suggestEl.style.minWidth = Math.max(rect.width, 168) + 'px';
        suggestEl.style.maxWidth = width + 'px';
        suggestEl.style.maxHeight = maxHeight + 'px';
        suggestEl.style.zIndex = '10070';
        suggestEl.style.display = 'block';

        var scroll = suggestEl.querySelector('.grid-filter-suggest-scroll');
        if (scroll) {
            scroll.style.maxHeight = Math.max(32, maxHeight - 8) + 'px';
        }
    }

    function positionSuggest(input, recalculateWidth) {
        layoutSuggest(input, recalculateWidth);
    }

    function syncOpenSuggest(input, generation) {
        if (!canShowSuggest(input, generation) || !suggestEl || suggestEl.hidden) return;
        positionSuggest(input, false);
    }

    function scheduleSyncOpenSuggest(input, generation) {
        if (!canShowSuggest(input, generation)) return;

        ensureGridHeaderLayout(input);
        positionSuggest(input, true);

        requestAnimationFrame(function () {
            if (!canShowSuggest(input, generation)) return;
            positionSuggest(input, false);
            requestAnimationFrame(function () {
                if (!canShowSuggest(input, generation)) return;
                positionSuggest(input, false);
            });
        });
    }

    function showSuggestBox(input, generation) {
        if (!canShowSuggest(input, generation)) return;

        suggestEl.hidden = false;
        suggestEl.removeAttribute('hidden');
        scheduleSyncOpenSuggest(input, generation);
    }

    function hideSuggest() {
        suggestGeneration += 1;
        cachedSuggestWidth = null;
        lastSuggestValues = [];
        clearSuggestOpenWrap();
        if (suggestEl) {
            suggestEl.hidden = true;
            suggestEl.classList.remove('is-above');
            suggestEl.style.display = 'none';
            suggestEl.innerHTML = '';
            suggestEl.style.top = '';
            suggestEl.style.bottom = '';
            suggestEl.style.left = '';
            suggestEl.style.width = '';
            suggestEl.style.minWidth = '';
            suggestEl.style.maxWidth = '';
            suggestEl.style.maxHeight = '';
            if (suggestEl.parentElement !== document.body) {
                document.body.appendChild(suggestEl);
            }
        }
        activeInput = null;
    }

    function canShowSuggest(input, generation) {
        return generation === suggestGeneration
            && input
            && input.isConnected
            && isSuggestAnchorVisible(input);
    }

    function renderSuggest(input, values, query, loading, generation) {
        if (!canShowSuggest(input, generation)) return;

        var box = ensureSuggestElement();
        if (!box) return;

        cachedSuggestWidth = null;
        lastSuggestValues = loading ? [] : (values || []).slice(0, 100);
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
                syncClearButton(input);
                applyGridFilters(input, null);
                syncClearFilterButtons();
            });
            createScroll.appendChild(create);
        }

        if (!canShowSuggest(input, generation)) return;

        showSuggestBox(input, generation);
        requestAnimationFrame(function () {
            if (!canShowSuggest(input, generation)) return;
            positionSuggest(input, true);
        });
    }

    function updateSuggest(input) {
        if (activeInput && activeInput !== input) {
            suggestGeneration += 1;
        }

        var query = readComboboxValue(input).trim();
        var requestId = (input.dataset.suggestRequestId = String((parseInt(input.dataset.suggestRequestId, 10) || 0) + 1));
        var generation = suggestGeneration;

        renderSuggest(input, [], query, true, generation);

        clearTimeout(loadTimer);
        loadTimer = setTimeout(function () {
            fetchDistinctValues(input, query).then(function (items) {
                if (input.dataset.suggestRequestId !== requestId) return;
                if (generation !== suggestGeneration) return;
                if (document.activeElement !== input && activeInput !== input) return;
                renderSuggest(input, items, query, false, generation);
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

        syncClearButton(el);

        el.addEventListener('focus', function () {
            ensureGridHeaderLayout(el);
            updateSuggest(el);
        });

        el.addEventListener('click', function () {
            updateSuggest(el);
        });

        el.addEventListener('input', function () {
            updateSuggest(el);
            syncClearButton(el);
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

        if (typeof ResizeObserver !== 'undefined') {
            var anchor = el.closest('.grid-filter-input-wrap') || el;
            var observer = new ResizeObserver(function () {
                if (activeInput !== el || !suggestEl || suggestEl.hidden) return;
                if (!isSuggestAnchorVisible(el)) {
                    hideSuggest();
                    return;
                }
                cachedSuggestWidth = null;
                positionSuggest(el, true);
            });
            observer.observe(anchor);
            var cell = getFilterCell(el);
            if (cell && cell !== anchor) observer.observe(cell);
        }
    }

    function cleanupLegacySuggestElements() {
        document.querySelectorAll('.grid-filter-suggest').forEach(function (node) {
            if (node !== suggestEl) {
                node.remove();
            }
        });
    }

    function initAll() {
        cleanupLegacySuggestElements();
        document.querySelectorAll('.grid-combobox').forEach(initCombobox);
        bindGridScrollDismiss();
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
        if (!activeInput || !suggestEl || suggestEl.hidden) return;
        if (!isSuggestAnchorVisible(activeInput)) {
            hideSuggest();
            return;
        }
        cachedSuggestWidth = null;
        positionSuggest(activeInput, true);
    });

    document.addEventListener('grid-scroll-ready', function () {
        bindGridScrollDismiss();
        if (activeInput) hideSuggest();
    });

    window.addEventListener('scroll', function () {
        if (!activeInput || !suggestEl || suggestEl.hidden) return;
        if (!isSuggestAnchorVisible(activeInput)) {
            hideSuggest();
            return;
        }
        layoutSuggest(activeInput, false);
    }, { passive: true, capture: true });

    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', function () {
            if (!activeInput || !suggestEl || suggestEl.hidden) return;
            cachedSuggestWidth = null;
            layoutSuggest(activeInput, true);
        });
        window.visualViewport.addEventListener('scroll', function () {
            if (!activeInput || !suggestEl || suggestEl.hidden) return;
            if (!isSuggestAnchorVisible(activeInput)) {
                hideSuggest();
                return;
            }
            layoutSuggest(activeInput, false);
        });
    }

    window.addEventListener('resize', function () {
        if (!activeInput || !suggestEl || suggestEl.hidden) return;
        cachedSuggestWidth = null;
        layoutSuggest(activeInput, true);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    window.addEventListener('load', initAll);
    window.krsApplyGridFilters = applyGridFilters;
    window.krsNavigateGridParams = navigateGridParams;
})();
