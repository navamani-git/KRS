(function () {
    var WIDTH_STORAGE = 'krs-grid-widths:v12:';
    var MIN_WIDTH = 28;
    var MAX_WIDTH = 480;
    var AUTO_FILL_MAX = 160;

    // ID, Chassis, Subdealer, Customer, Mobile, Status, dates×4, Inv/Ins doc, Registered, Actions
    var DEFAULT_WIDTHS = {
        vehicle_bookings: [52, 200, 160, 120, 100, 100, 82, 82, 82, 82, 64, 64, 82, 58],
        showroom_stock: [44, 90, 200, 100, 90, 90, 88, 72, 88],
        // Admin: #, Subdealer, Order Date, Order #, Allocated, Model, Color, Chassis, Price, Delivery, Status, Actions
        vehicles_admin_12: [42, 120, 94, 100, 138, 168, 100, 158, 104, 92, 100, 58],
        // Subdealer: #, Order Date, Order #, Allocated, Model, Color, Chassis, Source, Price, Delivery, Status, Actions
        vehicles_subdealer_12: [42, 94, 100, 138, 168, 100, 158, 84, 104, 92, 100, 58],
        vehicles_13: [42, 120, 94, 100, 138, 168, 100, 158, 84, 104, 92, 100, 58]
    };

    var fillTimer;

    function sumWidths(widths) {
        return widths.reduce(function (sum, w) { return sum + w; }, 0);
    }

    function isColumnVisible(table, index) {
        var headerRow = getHeaderRow(table);
        if (!headerRow || !headerRow.cells[index]) return true;
        var th = headerRow.cells[index];
        if (th.getAttribute('aria-hidden') === 'true') return false;
        return th.style.display !== 'none';
    }

    function getVisibleColumnIndexes(table, widths) {
        var visible = [];
        for (var i = 0; i < widths.length; i++) {
            if (isColumnVisible(table, i)) visible.push(i);
        }
        return visible;
    }

    function sumVisibleWidths(table, widths) {
        var total = 0;
        for (var i = 0; i < widths.length; i++) {
            if (isColumnVisible(table, i)) total += widths[i];
        }
        return total;
    }

    function getPanelWidth(table) {
        var panel = table.closest('.grid-scroll-panel, .table-responsive');
        if (panel) {
            var panelWidth = panel.clientWidth || panel.getBoundingClientRect().width;
            if (panelWidth > 0) return Math.floor(panelWidth);
        }

        var cardBody = table.closest('.card-body');
        if (cardBody) {
            var bodyWidth = cardBody.clientWidth || cardBody.getBoundingClientRect().width;
            if (bodyWidth > 0) return Math.floor(bodyWidth);
        }

        var card = table.closest('.card');
        if (card) {
            var cardWidth = card.clientWidth || card.getBoundingClientRect().width;
            if (cardWidth > 0) return Math.floor(cardWidth);
        }

        return 0;
    }

    function getFlexColumnIndexes(table) {
        var headerRow = getHeaderRow(table);
        if (!headerRow) return [];
        var flex = [];
        Array.from(headerRow.cells).forEach(function (th, index) {
            if (th.dataset.gridColFlex === '1' || th.classList.contains('grid-col-flex')) {
                flex.push(index);
            }
        });
        return flex;
    }

    function distributeExtraWidth(widths, extra, targetIndexes, maxCap) {
        if (maxCap === undefined) maxCap = MAX_WIDTH;
        var result = widths.slice();
        if (extra <= 0 || !targetIndexes.length) return result;

        var remaining = extra;
        var guard = 0;
        while (remaining > 0 && guard < 200) {
            guard += 1;
            var active = targetIndexes.filter(function (i) { return result[i] < maxCap; });
            if (!active.length) break;
            var share = Math.max(1, Math.floor(remaining / active.length));
            active.forEach(function (i) {
                if (remaining <= 0) return;
                var add = Math.min(share, maxCap - result[i], remaining);
                result[i] += add;
                remaining -= add;
            });
        }
        return result;
    }

    function sumIndexes(widths, indexes) {
        var total = 0;
        indexes.forEach(function (i) { total += widths[i]; });
        return total;
    }

    function expandToPanelWidth(widths, visibleIndexes, panelWidth, flexIndexes) {
        var result = widths.slice();
        var total = sumIndexes(result, visibleIndexes);
        if (panelWidth <= 0 || total >= panelWidth) {
            return { widths: result, tableWidth: total };
        }

        var extra = panelWidth - total;
        if (flexIndexes.length) {
            result = distributeExtraWidth(result, extra, flexIndexes, AUTO_FILL_MAX);
            total = sumIndexes(result, visibleIndexes);
            extra = panelWidth - total;
        }

        if (extra > 0 && flexIndexes.length) {
            result = distributeExtraWidth(result, extra, flexIndexes, MAX_WIDTH);
            total = sumIndexes(result, visibleIndexes);
        }

        return { widths: result, tableWidth: total };
    }

    function isLockedColumn(table, index) {
        if (isChassisColumn(table, index)) return true;
        var headerRow = getHeaderRow(table);
        var th = headerRow && headerRow.cells[index];
        if (!th) return false;
        return th.classList.contains('grid-col-actions') || th.classList.contains('grid-col-rownum');
    }

    function headerContentWidth(table, colIndex) {
        var headerRow = getHeaderRow(table);
        var width = MIN_WIDTH;
        if (headerRow && headerRow.cells[colIndex]) {
            width = Math.max(width, measureCellContentWidth(headerRow.cells[colIndex]) + 18);
        }
        return Math.max(MIN_WIDTH, Math.min(MAX_WIDTH, Math.ceil(width)));
    }

    function ensureContentWidths(table, fallback) {
        if (table._krsContentWidths && table._krsContentWidths.length === fallback.length) {
            return table._krsContentWidths.slice();
        }

        var measured = fallback.slice();
        fitColumnsToContent(table, measured);
        findChassisColumnIndexes(table).forEach(function (index) {
            measured[index] = Math.max(measured[index], 176);
        });
        table._krsContentWidths = measured.slice();
        return measured;
    }

    function shrinkFlexibleColumns(widths, flexible, floors, overflow) {
        var result = widths.slice();
        var room = 0;
        flexible.forEach(function (index) {
            room += Math.max(0, result[index] - floors[index]);
        });
        if (room <= 0 || overflow <= 0) return result;

        var cut = Math.min(overflow, room);
        var remaining = cut;
        flexible.forEach(function (index) {
            var can = Math.max(0, result[index] - floors[index]);
            if (can <= 0 || remaining <= 0) return;
            var delta = Math.min(can, remaining, Math.max(1, Math.round(cut * can / room)));
            result[index] -= delta;
            remaining -= delta;
        });
        flexible.forEach(function (index) {
            if (remaining <= 0) return;
            var can = result[index] - floors[index];
            if (can <= 0) return;
            var delta = Math.min(can, remaining);
            result[index] -= delta;
            remaining -= delta;
        });
        return result;
    }

    function fitGridToScreen(table, contentWidths) {
        var panelWidth = getPanelWidth(table);
        var widths = contentWidths.slice();
        var visible = getVisibleColumnIndexes(table, widths);
        var floors = widths.map(function (_, index) { return headerContentWidth(table, index); });

        findChassisColumnIndexes(table).forEach(function (index) {
            if (visible.indexOf(index) < 0) return;
            widths[index] = Math.max(widths[index], 176);
        });

        var total = sumIndexes(widths, visible);
        if (panelWidth <= 0) {
            return { widths: widths, tableWidth: total, needsRefit: true };
        }

        var locked = visible.filter(function (index) { return isLockedColumn(table, index); });
        var flexible = visible.filter(function (index) { return locked.indexOf(index) < 0; });

        if (total < panelWidth && flexible.length) {
            widths = distributeExtraWidth(widths, panelWidth - total, flexible, 100000);
        } else if (total > panelWidth && flexible.length) {
            widths = shrinkFlexibleColumns(widths, flexible, floors, total - panelWidth);
        }

        return {
            widths: widths,
            tableWidth: sumIndexes(widths, visible),
            needsRefit: false
        };
    }

    function pinWidthsToPanel(table, widths) {
        var panelWidth = getPanelWidth(table);
        var visible = getVisibleColumnIndexes(table, widths);
        var result = widths.slice();
        var overrides = table._krsWidthOverrides || {};
        var pinned = [];

        Object.keys(overrides).forEach(function (key) {
            var index = parseInt(key, 10);
            var width = parseInt(overrides[key], 10);
            if (isNaN(index) || isNaN(width) || visible.indexOf(index) < 0) return;
            result[index] = Math.max(MIN_WIDTH, Math.min(MAX_WIDTH, width));
            pinned.push(index);
        });

        if (panelWidth <= 0) {
            return { widths: result, tableWidth: sumIndexes(result, visible), needsRefit: true };
        }

        var headerFloors = result.map(function (_, index) { return headerContentWidth(table, index); });
        var minFloors = result.map(function () { return MIN_WIDTH; });
        var donors = visible.filter(function (index) {
            return pinned.indexOf(index) < 0 && !isLockedColumn(table, index);
        });

        var total = sumIndexes(result, visible);
        if (total < panelWidth && donors.length) {
            result = distributeExtraWidth(result, panelWidth - total, donors, 100000);
        } else if (total > panelWidth && donors.length) {
            result = shrinkFlexibleColumns(result, donors, headerFloors, total - panelWidth);
            total = sumIndexes(result, visible);
            if (total > panelWidth) {
                result = shrinkFlexibleColumns(result, donors, minFloors, total - panelWidth);
            }
        }

        total = sumIndexes(result, visible);
        if (total > panelWidth) {
            var chassisDonors = visible.filter(function (index) {
                return pinned.indexOf(index) < 0 && isChassisColumn(table, index);
            });
            result = shrinkFlexibleColumns(result, chassisDonors, minFloors, total - panelWidth);
        }

        total = sumIndexes(result, visible);
        if (total > panelWidth && pinned.length) {
            result = shrinkFlexibleColumns(result, pinned, minFloors, total - panelWidth);
        }

        total = sumIndexes(result, visible);
        var diff = panelWidth - total;
        if (diff !== 0 && visible.length) {
            var adjustIndex = -1;
            for (var i = visible.length - 1; i >= 0; i--) {
                if (pinned.indexOf(visible[i]) < 0) {
                    adjustIndex = visible[i];
                    break;
                }
            }
            if (adjustIndex < 0) adjustIndex = visible[visible.length - 1];
            result[adjustIndex] = Math.max(MIN_WIDTH, result[adjustIndex] + diff);
        }

        return { widths: result, tableWidth: panelWidth, needsRefit: false };
    }

    function fitVisibleColumnsToPanel(table, widths) {
        var contentWidths = ensureContentWidths(table, widths);
        var fitted = fitGridToScreen(table, contentWidths);
        return pinWidthsToPanel(table, fitted.widths);
    }

    function scheduleWidthRefit(table) {
        table._krsRefitAttempts = (table._krsRefitAttempts || 0) + 1;
        if (table._krsRefitAttempts > 4) return;

        if (table._krsRefitTimer) clearTimeout(table._krsRefitTimer);
        table._krsRefitTimer = setTimeout(function () {
            table._krsRefitTimer = null;
            if (typeof window.krsInitGridResize === 'function') {
                window.krsInitGridResize();
            }
        }, 120);
    }

    function getHeaderRow(table) {
        return table.querySelector('thead tr:not(.grid-column-filters)') || table.querySelector('thead tr');
    }

    function resolveGridId(table) {
        if (table.dataset.gridId) return table.dataset.gridId;
        var el = table.querySelector('[data-grid-id]');
        if (el && el.dataset.gridId) {
            table.dataset.gridId = el.dataset.gridId;
            return el.dataset.gridId;
        }
        var headerRow = getHeaderRow(table);
        return 'cols-' + (headerRow ? headerRow.cells.length : 0);
    }

    function getStorageKey(table) {
        return WIDTH_STORAGE + window.location.pathname + ':' + resolveGridId(table);
    }

    function getHeaderLabels(table) {
        var headerRow = getHeaderRow(table);
        if (!headerRow) return [];
        return Array.from(headerRow.cells).map(function (th) {
            var labelEl = th.querySelector('.grid-col-header-text');
            return (labelEl ? labelEl.textContent : th.textContent).replace(/\s+/g, ' ').trim().toLowerCase();
        });
    }

    function getVehiclesPreset(table, count) {
        var labels = getHeaderLabels(table);
        var hasSubdealerCol = labels.indexOf('subdealer') >= 0;
        var hasSourceCol = labels.indexOf('source') >= 0;
        if (count === 12 && hasSubdealerCol && !hasSourceCol) {
            return DEFAULT_WIDTHS.vehicles_admin_12.slice();
        }
        if (count === 12 && hasSourceCol) {
            return DEFAULT_WIDTHS.vehicles_subdealer_12.slice();
        }
        if (count === 13) return DEFAULT_WIDTHS.vehicles_13.slice();
        return null;
    }

    function getDefaultWidths(table, count) {
        var gridId = resolveGridId(table);
        if (gridId === 'vehicle_bookings' || gridId.indexOf('vehicle_bookings') >= 0) {
            var preset = DEFAULT_WIDTHS.vehicle_bookings;
            if (preset.length === count) return preset.slice();
        }
        if (gridId === 'showroom_stock' || gridId.indexOf('showroom_stock') >= 0) {
            var stockPreset = DEFAULT_WIDTHS.showroom_stock;
            if (stockPreset.length === count) return stockPreset.slice();
        }
        if (gridId === 'vehicles' || gridId.indexOf('vehicles') >= 0) {
            var vehiclesPreset = getVehiclesPreset(table, count);
            if (vehiclesPreset) return vehiclesPreset;
        }
        return Array.from({ length: count }, function (_, i) {
            return i === 0 ? 42 : (i === count - 1 ? 58 : 78);
        });
    }

    function loadWidths(table, defaults) {
        try {
            var raw = sessionStorage.getItem(getStorageKey(table));
            if (!raw) return defaults.slice();
            var saved = JSON.parse(raw);
            if (!Array.isArray(saved) || saved.length !== defaults.length) return defaults.slice();
            return saved.map(function (w, i) {
                var n = parseInt(w, 10);
                if (isNaN(n)) return defaults[i];
                return Math.max(MIN_WIDTH, Math.min(MAX_WIDTH, n));
            });
        } catch (e) {
            return defaults.slice();
        }
    }

    function saveWidths(table, widths) {
        try {
            sessionStorage.setItem(getStorageKey(table), JSON.stringify(widths));
        } catch (e) {
            // ignore
        }
    }

    function ensureColgroup(table, count) {
        var colgroup = table.querySelector('colgroup');
        if (!colgroup) {
            colgroup = document.createElement('colgroup');
            table.insertBefore(colgroup, table.firstChild);
        }
        while (colgroup.children.length < count) {
            colgroup.appendChild(document.createElement('col'));
        }
        while (colgroup.children.length > count) {
            colgroup.removeChild(colgroup.lastChild);
        }
        return colgroup;
    }

    function applyCellWidth(cell, widthPx) {
        if (!cell || cell.style.display === 'none') return;
        cell.style.padding = '';
        cell.style.borderWidth = '';
        var w = widthPx + 'px';
        cell.style.width = w;
        cell.style.minWidth = w;
        cell.style.maxWidth = w;
        cell.style.boxSizing = 'border-box';
        if (cell.closest('tr.grid-column-filters')) {
            cell.style.overflow = 'visible';
        } else {
            cell.style.overflow = 'hidden';
        }
    }

    function clearHiddenColumnLayout(cell) {
        if (!cell) return;
        cell.style.width = '0';
        cell.style.minWidth = '0';
        cell.style.maxWidth = '0';
        cell.style.padding = '0';
        cell.style.borderWidth = '0';
        cell.style.left = '';
    }

    function getColumnCells(table, colIndex) {
        var cells = [];
        var headerRow = getHeaderRow(table);
        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (headerRow && headerRow.cells[colIndex]) cells.push(headerRow.cells[colIndex]);
        if (filterRow && filterRow.cells[colIndex]) cells.push(filterRow.cells[colIndex]);
        table.querySelectorAll('tbody tr').forEach(function (row) {
            if (row.cells[colIndex]) cells.push(row.cells[colIndex]);
        });
        return cells;
    }

    function isStickyColumn(table, colIndex) {
        var headerRow = getHeaderRow(table);
        if (!headerRow || !headerRow.cells[colIndex]) return false;
        var th = headerRow.cells[colIndex];
        return th.classList.contains('grid-sticky-col') || th.classList.contains('grid-sticky-col-2');
    }

    function getStickyColumnCells(table, colIndex) {
        var cells = [];
        var headerRow = getHeaderRow(table);
        if (headerRow && headerRow.cells[colIndex]) {
            cells.push(headerRow.cells[colIndex]);
        }
        table.querySelectorAll('tbody tr').forEach(function (row) {
            var cell = row.cells[colIndex];
            if (!cell) return;
            if (cell.classList.contains('grid-sticky-col') || cell.classList.contains('grid-sticky-col-2')) {
                cells.push(cell);
            }
        });
        return cells;
    }

    function syncStickyColumns(table, effectiveWidths) {
        var headerRow = getHeaderRow(table);
        if (!headerRow) return;

        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (filterRow) {
            Array.from(filterRow.cells).forEach(function (cell) {
                cell.style.removeProperty('left');
                cell.style.removeProperty('z-index');
            });
        }

        for (var i = 0; i < headerRow.cells.length; i++) {
            if (!isStickyColumn(table, i)) continue;
            getStickyColumnCells(table, i).forEach(function (cell) {
                cell.style.removeProperty('left');
            });
        }

        table.style.removeProperty('--grid-sticky-col-1-width');
    }

    function applyWidths(table, colgroup, widths) {
        var resolved = fitVisibleColumnsToPanel(table, widths);
        var effective = resolved.widths;

        Array.from(colgroup.children).forEach(function (col, index) {
            if (index >= effective.length) return;
            if (!isColumnVisible(table, index)) {
                col.style.display = 'none';
                col.style.visibility = 'collapse';
                col.style.width = '0';
                col.style.minWidth = '0';
                col.style.maxWidth = '0';
                return;
            }
            col.style.display = '';
            col.style.visibility = 'visible';
            var w = effective[index] + 'px';
            col.style.width = w;
            col.style.minWidth = w;
            col.style.maxWidth = w;
        });

        var headerRow = getHeaderRow(table);
        var filterRow = table.querySelector('thead tr.grid-column-filters');
        [headerRow, filterRow].forEach(function (row) {
            if (!row) return;
            Array.from(row.cells).forEach(function (cell, index) {
                if (index >= effective.length) return;
                if (!isColumnVisible(table, index)) {
                    clearHiddenColumnLayout(cell);
                    return;
                }
                applyCellWidth(cell, effective[index]);
            });
        });

        table.querySelectorAll('tbody tr').forEach(function (row) {
            Array.from(row.cells).forEach(function (cell, index) {
                if (index >= effective.length) return;
                if (!isColumnVisible(table, index)) {
                    clearHiddenColumnLayout(cell);
                    return;
                }
                applyCellWidth(cell, effective[index]);
            });
        });

        table.style.tableLayout = 'fixed';
        table.style.setProperty('width', resolved.tableWidth + 'px', 'important');
        table.style.setProperty('min-width', '0', 'important');
        table.style.setProperty('max-width', resolved.tableWidth + 'px', 'important');

        syncStickyColumns(table, effective);

        if (shouldScheduleRefit(table, widths, resolved)) {
            scheduleWidthRefit(table);
        } else {
            table._krsRefitAttempts = 0;
        }
    }

    function shouldScheduleRefit(table, widths, resolved) {
        if (!resolved.needsRefit) return false;
        return getPanelWidth(table) <= 0;
    }

    function measureCellContentWidth(cell) {
        if (!cell || cell.style.display === 'none') return 0;
        var clone = cell.cloneNode(true);
        clone.style.cssText = 'position:absolute;left:-9999px;top:0;visibility:hidden;'
            + 'white-space:nowrap;width:auto;min-width:0;max-width:none;overflow:visible;'
            + 'height:auto;display:inline-block;box-sizing:border-box;';
        document.body.appendChild(clone);
        var width = clone.getBoundingClientRect().width;
        document.body.removeChild(clone);
        return width;
    }

    function isColumnExpanded(table, colIndex) {
        return !!(table._krsWidthOverrides && table._krsWidthOverrides[colIndex] != null);
    }

    function isChassisHeaderLabel(label) {
        if (!label) return false;
        var text = label.replace(/\s+/g, ' ').trim();
        if (!text) return false;
        return /^chassis(\b|\.|#|\s)/i.test(text)
            || /^chassis$/i.test(text)
            || /^chassis\s*no\.?$/i.test(text)
            || /^vin$/i.test(text);
    }

    function isChassisColumn(table, colIndex) {
        return findChassisColumnIndexes(table).indexOf(colIndex) >= 0;
    }

    function findChassisColumnIndexes(table) {
        var indexes = new Set();
        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (filterRow) {
            filterRow.querySelectorAll('th[data-filter-key="chassis"], th[data-filter-key="vin"]').forEach(function (th) {
                var idx = parseInt(th.getAttribute('data-grid-col-index') || '', 10);
                if (!isNaN(idx)) indexes.add(idx);
            });
        }

        var headerRow = getHeaderRow(table);
        if (headerRow) {
            Array.from(headerRow.cells).forEach(function (th, index) {
                var labelEl = th.querySelector('.grid-col-header-text');
                var label = labelEl ? labelEl.textContent : th.textContent;
                if (isChassisHeaderLabel(label)) indexes.add(index);
            });
        }

        return Array.from(indexes).filter(function (i) { return i >= 0; }).sort(function (a, b) { return a - b; });
    }

    function columnContentWidth(table, colIndex) {
        var maxW = MIN_WIDTH;
        var headerRow = getHeaderRow(table);
        if (headerRow && headerRow.cells[colIndex]) {
            maxW = Math.max(maxW, measureCellContentWidth(headerRow.cells[colIndex]));
        }
        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (filterRow && filterRow.cells[colIndex]) {
            maxW = Math.max(maxW, measureCellContentWidth(filterRow.cells[colIndex]));
        }
        table.querySelectorAll('tbody tr').forEach(function (row) {
            if (!row.cells[colIndex]) return;
            maxW = Math.max(maxW, measureCellContentWidth(row.cells[colIndex]));
        });
        var pad = isChassisColumn(table, colIndex) ? 20 : 12;
        return Math.max(MIN_WIDTH, Math.min(MAX_WIDTH, Math.ceil(maxW) + pad));
    }

    function fitColumnsToContent(table, widths) {
        for (var i = 0; i < widths.length; i++) {
            if (!isColumnVisible(table, i)) continue;
            widths[i] = columnContentWidth(table, i);
        }
    }

    function autoExpandChassisColumns(table, colgroup, widths, defaults) {
        findChassisColumnIndexes(table).forEach(function (colIndex) {
            if (!isColumnVisible(table, colIndex)) return;
            autoFitColumn(table, colIndex, colgroup, widths, defaults);
        });
    }

    function shrinkColumn(table, colIndex, colgroup, widths, defaults) {
        if (table._krsWidthOverrides) delete table._krsWidthOverrides[colIndex];
        var headerRow = getHeaderRow(table);
        if (headerRow && headerRow.cells[colIndex]) {
            delete headerRow.cells[colIndex].dataset.gridColExpanded;
        }
        applyWidths(table, colgroup, widths);
        saveWidths(table, widths);
        updateExpandButton(table, colIndex, widths, defaults);
        notifyLayoutChanged();
    }

    function updateExpandButton(table, colIndex, widths, defaults) {
        var headerRow = getHeaderRow(table);
        if (!headerRow || !headerRow.cells[colIndex]) return;

        var btn = headerRow.cells[colIndex].querySelector('.grid-col-expand-btn');
        if (!btn) return;

        var expanded = isColumnExpanded(table, colIndex);
        var icon = btn.querySelector('i');
        if (icon) {
            icon.className = expanded ? 'bi bi-arrows-angle-contract' : 'bi bi-arrows-angle-expand';
        }
        btn.title = expanded ? 'Shrink column to default width' : 'Expand column to fit content';
        btn.setAttribute('aria-label', btn.title);
        btn.classList.toggle('is-expanded', expanded);
    }

    function updateAllExpandButtons(table, widths, defaults) {
        for (var i = 0; i < widths.length; i++) {
            updateExpandButton(table, i, widths, defaults);
        }
    }

    function autoFitColumn(table, colIndex, colgroup, widths, defaults) {
        var maxW = MIN_WIDTH;
        var headerRow = getHeaderRow(table);
        if (headerRow && headerRow.cells[colIndex]) {
            maxW = Math.max(maxW, measureCellContentWidth(headerRow.cells[colIndex]));
        }
        var filterRow = table.querySelector('thead tr.grid-column-filters');
        if (filterRow && filterRow.cells[colIndex]) {
            maxW = Math.max(maxW, measureCellContentWidth(filterRow.cells[colIndex]));
        }
        table.querySelectorAll('tbody tr').forEach(function (row) {
            if (!row.cells[colIndex]) return;
            maxW = Math.max(maxW, measureCellContentWidth(row.cells[colIndex]));
        });
        var pad = isChassisColumn(table, colIndex) ? 20 : 12;
        var fitted = Math.max(MIN_WIDTH, Math.min(MAX_WIDTH, Math.ceil(maxW) + pad));
        table._krsWidthOverrides = table._krsWidthOverrides || {};
        table._krsWidthOverrides[colIndex] = fitted;
        widths[colIndex] = fitted;
        if (headerRow && headerRow.cells[colIndex]) {
            headerRow.cells[colIndex].dataset.gridColExpanded = '1';
        }
        applyWidths(table, colgroup, widths);
        saveWidths(table, widths);
        updateExpandButton(table, colIndex, widths, defaults);
        notifyLayoutChanged();
    }

    function updateStickyOffset(table, firstColWidth) {
        table.style.setProperty('--grid-sticky-col-1-width', firstColWidth + 'px');
    }

    function notifyLayoutChanged() {
        if (typeof window.krsFitGridScroll === 'function') {
            window.krsFitGridScroll();
        }
    }

    function addToolbarButton(table, defaults, colgroup, widths) {
        var cardBody = table.closest('.card-body');
        if (!cardBody) return;
        var toolbar = cardBody.querySelector('.grid-column-toolbar');
        if (!toolbar || toolbar.querySelector('.grid-reset-widths-btn')) return;

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-sm btn-outline-secondary grid-reset-widths-btn';
        btn.innerHTML = '<i class="bi bi-arrow-counterclockwise"></i> Reset widths';
        btn.title = 'Reset all column widths to compact defaults';
        btn.addEventListener('click', function () {
            table._krsUserSized = false;
            table._krsWidthOverrides = {};
            table._krsContentWidths = null;
            try { sessionStorage.removeItem(getStorageKey(table)); } catch (e) { }
            for (var i = 0; i < defaults.length; i++) widths[i] = defaults[i];
            applyWidths(table, colgroup, widths);
            updateAllExpandButtons(table, widths, defaults);
            notifyLayoutChanged();
        });
        toolbar.appendChild(btn);
    }

    function addExpandButton(th, table, colIndex, colgroup, widths, defaults) {
        if (th.querySelector('.grid-col-expand-btn')) {
            updateExpandButton(table, colIndex, widths, defaults);
            return;
        }

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-link btn-sm p-0 ms-1 grid-col-expand-btn';
        btn.innerHTML = '<i class="bi bi-arrows-angle-expand"></i>';
        btn.title = 'Expand column to fit content';
        btn.setAttribute('aria-label', btn.title);
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (isColumnExpanded(table, colIndex)) {
                shrinkColumn(table, colIndex, colgroup, widths, defaults);
            } else {
                autoFitColumn(table, colIndex, colgroup, widths, defaults);
            }
        });
        th.appendChild(btn);
        updateExpandButton(table, colIndex, widths, defaults);
    }

    function displayedColumnWidth(table, colIndex, fallback) {
        var headerRow = getHeaderRow(table);
        var cell = headerRow && headerRow.cells[colIndex];
        var parsed = cell ? parseInt(cell.style.width, 10) : 0;
        return parsed > 0 ? parsed : fallback;
    }

    function startResize(table, colIndex, colgroup, widths, defaults, startX) {
        var startWidth = displayedColumnWidth(table, colIndex, widths[colIndex]);
        table._krsWidthOverrides = table._krsWidthOverrides || {};
        document.body.classList.add('grid-col-resizing');

        function onMove(e) {
            var delta = e.clientX - startX;
            var next = Math.max(MIN_WIDTH, Math.min(MAX_WIDTH, startWidth + delta));
            table._krsWidthOverrides[colIndex] = next;
            widths[colIndex] = next;
            applyWidths(table, colgroup, widths);
            notifyLayoutChanged();
        }

        function onUp() {
            document.body.classList.remove('grid-col-resizing');
            saveWidths(table, widths);
            updateExpandButton(table, colIndex, widths, defaults);
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
        }

        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
    }

    function wrapHeaderLabel(th) {
        if (th.querySelector('.grid-col-header-text')) return;

        var span = document.createElement('span');
        span.className = 'grid-col-header-text';
        var moved = false;

        Array.from(th.childNodes).forEach(function (node) {
            if (node.nodeType === 1) {
                var el = node;
                if (el.classList.contains('grid-col-resize-handle')
                    || el.classList.contains('grid-col-expand-btn')
                    || el.classList.contains('grid-col-sort')) {
                    return;
                }
            }
            span.appendChild(node);
            moved = true;
        });

        if (moved && span.textContent.trim()) {
            th.insertBefore(span, th.firstChild);
        }
    }

    function initTable(table) {
        if (!table.classList.contains('grid-excel-table')) return;

        var headerRow = getHeaderRow(table);
        if (!headerRow || !headerRow.cells.length) return;

        var count = headerRow.cells.length;
        var defaults = getDefaultWidths(table, count);
        var colgroup = ensureColgroup(table, count);
        var widths = table._krsColWidths || loadWidths(table, defaults);
        table._krsColWidths = widths;
        table._krsColDefaults = defaults;

        applyWidths(table, colgroup, widths);
        addToolbarButton(table, defaults, colgroup, widths);
        updateAllExpandButtons(table, widths, defaults);

        window.requestAnimationFrame(function () {
            if (!table._krsUserSized) {
                table._krsContentWidths = null;
                applyWidths(table, colgroup, widths);
            }
        });

        if (table.dataset.gridResizeInit === '1') {
            notifyLayoutChanged();
            return;
        }
        table.dataset.gridResizeInit = '1';

        Array.from(headerRow.cells).forEach(function (th, index) {
            if (th.querySelector('.grid-col-resize-handle')) return;
            th.classList.add('grid-th-resizable');
            wrapHeaderLabel(th);

            var handle = document.createElement('span');
            handle.className = 'grid-col-resize-handle';
            handle.setAttribute('role', 'separator');
            handle.setAttribute('aria-orientation', 'vertical');
            handle.title = 'Drag to resize · double-click to reset width';

            th.addEventListener('dblclick', function (e) {
                if (e.target.closest('.grid-col-resize-handle, .grid-col-expand-btn')) return;
                e.preventDefault();
                if (isColumnExpanded(table, index)) {
                    shrinkColumn(table, index, colgroup, widths, defaults);
                } else {
                    autoFitColumn(table, index, colgroup, widths, defaults);
                }
            });

            addExpandButton(th, table, index, colgroup, widths, defaults);

            handle.addEventListener('mousedown', function (e) {
                e.preventDefault();
                e.stopPropagation();
                startResize(table, index, colgroup, widths, defaults, e.clientX);
            });

            handle.addEventListener('dblclick', function (e) {
                e.preventDefault();
                e.stopPropagation();
                shrinkColumn(table, index, colgroup, widths, defaults);
            });

            th.appendChild(handle);
        });

        notifyLayoutChanged();
    }

    function initAll() {
        document.querySelectorAll('table.grid-excel-table').forEach(initTable);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            setTimeout(initAll, 0);
        });
    } else {
        setTimeout(initAll, 0);
    }

    window.addEventListener('load', function () {
        initAll();
        setTimeout(initAll, 150);
    });
    window.addEventListener('resize', function () {
        clearTimeout(fillTimer);
        fillTimer = setTimeout(initAll, 120);
    });
    document.addEventListener('grid-layout-changed', function () {
        setTimeout(initAll, 50);
    });

    window.krsInitGridResize = initAll;
})();
