(function () {
    var MIN_VISIBLE_ROWS = 20;
    var DEFAULT_ROW_HEIGHT = 36;
    var resizeTimer;

    function measureHeaderRows(panel) {
        var headerRow = panel.querySelector('thead tr:not(.grid-column-filters)');
        var filterRow = panel.querySelector('thead tr.grid-column-filters');
        if (headerRow) {
            panel.style.setProperty('--grid-header-row-height', headerRow.getBoundingClientRect().height + 'px');
        }
        if (filterRow) {
            panel.style.setProperty('--grid-filter-row-height', filterRow.getBoundingClientRect().height + 'px');
        }
    }

    function applyCellTitles(panel) {
        panel.querySelectorAll('tbody td').forEach(function (td) {
            if (td.style.display === 'none') return;
            if (td.querySelector('button, a.btn, form, a.chassis-link')) return;
            var text = td.textContent.replace(/\s+/g, ' ').trim();
            if (!text) return;
            if (td.scrollWidth > td.clientWidth + 2) td.title = text;
            else td.removeAttribute('title');
        });
    }

    function isPaginatedGridCard(cardBody) {
        if (cardBody.querySelector('[aria-label="Pagination"]')) return true;
        if (cardBody.querySelector('.pagination')) return true;
        var bar = cardBody.querySelector('.d-flex.flex-wrap.justify-content-between.align-items-center');
        return bar && /Showing\s+\d+/i.test(bar.textContent);
    }

    function fitPanel(panel, table) {
        table = table || panel.querySelector('table');
        if (!table || !table.tHead) return;

        var top = panel.getBoundingClientRect().top;
        var cardBody = panel.parentElement;
        var paginationBar = cardBody.querySelector('.d-flex.flex-wrap.justify-content-between.align-items-center');
        var toolbar = cardBody.querySelector('.grid-column-toolbar');
        var reserve = 20;
        if (toolbar) {
            reserve += toolbar.getBoundingClientRect().height + 4;
        }
        if (paginationBar) {
            reserve += paginationBar.getBoundingClientRect().height + 12;
        } else {
            reserve += 44;
        }

        var sampleRow = panel.querySelector('tbody tr');
        var rowHeight = sampleRow ? sampleRow.getBoundingClientRect().height : DEFAULT_ROW_HEIGHT;
        if (rowHeight < 24) rowHeight = DEFAULT_ROW_HEIGHT;

        var theadHeight = panel.querySelector('thead')?.getBoundingClientRect().height || 90;
        var minHeight = theadHeight + (rowHeight * MIN_VISIBLE_ROWS);
        var viewportHeight = window.innerHeight - top - reserve;
        var height = Math.max(minHeight, viewportHeight);

        panel.classList.add('grid-scroll-panel');
        panel.style.maxHeight = height + 'px';
        measureHeaderRows(panel);
        applyCellTitles(panel);
        document.dispatchEvent(new CustomEvent('grid-scroll-ready'));
    }

    function fitGridScrollPanels() {
        document.querySelectorAll('.content .card .card-body .table-responsive').forEach(function (panel) {
            fitPanel(panel);
        });

        document.querySelectorAll('.content .card .card-body').forEach(function (cardBody) {
            if (!isPaginatedGridCard(cardBody)) return;
            if (cardBody.querySelector('.table-responsive')) return;

            var table = null;
            for (var i = 0; i < cardBody.children.length; i++) {
                var child = cardBody.children[i];
                if (child.tagName === 'TABLE' && child.tHead) {
                    table = child;
                    break;
                }
            }
            if (!table) return;

            var wrapper = table.parentElement;
            if (wrapper.classList.contains('grid-scroll-panel')) {
                fitPanel(wrapper, table);
                return;
            }

            var shell = document.createElement('div');
            shell.className = 'table-responsive grid-scroll-panel';
            cardBody.insertBefore(shell, table);
            shell.appendChild(table);
            fitPanel(shell, table);
        });
    }

    function scheduleFit() {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(fitGridScrollPanels, 120);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', fitGridScrollPanels);
    } else {
        fitGridScrollPanels();
    }

    window.addEventListener('resize', scheduleFit);
    window.addEventListener('load', fitGridScrollPanels);
    document.addEventListener('grid-layout-changed', scheduleFit);

    window.krsFitGridScroll = fitGridScrollPanels;
})();
