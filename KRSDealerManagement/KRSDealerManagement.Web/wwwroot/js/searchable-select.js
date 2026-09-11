(function () {
    if (typeof TomSelect === 'undefined') {
        console.warn('Tom Select is not loaded; searchable dropdowns are disabled.');
        return;
    }

    var optionObservers = new WeakMap();
    var refreshing = new WeakSet();

    function shouldEnhance(select) {
        if (!select || select.tagName !== 'SELECT') return false;
        if (select.dataset.krsSearchableInit === '1') return false;
        if (select.classList.contains('no-search-select')) return false;
        if (select.hasAttribute('data-no-search')) return false;
        return true;
    }

    function buildConfig(select) {
        var isSm = select.classList.contains('form-select-sm');
        return {
            plugins: ['dropdown_input'],
            allowEmptyOption: true,
            maxOptions: 5000,
            dropdownParent: 'body',
            create: false,
            sortField: { field: 'text', direction: 'asc' },
            onInitialize: function () {
                if (isSm) {
                    this.wrapper.classList.add('ts-sm');
                }
            }
        };
    }

    function disconnectOptionObserver(select) {
        var observer = optionObservers.get(select);
        if (observer) {
            observer.disconnect();
            optionObservers.delete(select);
        }
    }

    function destroy(select) {
        if (!select) return;
        disconnectOptionObserver(select);
        if (select.tomselect) {
            try {
                select.tomselect.destroy();
            } catch (e) {
                /* ignore */
            }
        }
        delete select.dataset.krsSearchableInit;
    }

    function watchOptions(select) {
        if (optionObservers.has(select)) return;

        var observer = new MutationObserver(function () {
            if (refreshing.has(select)) return;
            if (!select.isConnected) return;
            refresh(select);
        });

        observer.observe(select, { childList: true });
        optionObservers.set(select, observer);
    }

    function init(select) {
        if (!select || !shouldEnhance(select)) return null;

        destroy(select);

        try {
            var instance = new TomSelect(select, buildConfig(select));
            select.dataset.krsSearchableInit = '1';
            watchOptions(select);
            return instance;
        } catch (e) {
            console.warn('KrsSearchableSelect: failed to init select', select, e);
            return null;
        }
    }

    function refresh(select) {
        if (!select) return null;
        refreshing.add(select);
        try {
            var previousValue = select.value;
            destroy(select);
            select.value = previousValue;
            return init(select);
        } finally {
            refreshing.delete(select);
        }
    }

    function initAll(root) {
        (root || document).querySelectorAll('select').forEach(function (select) {
            init(select);
        });
    }

    function observeNewSelects() {
        if (!window.MutationObserver) return;

        var timer = null;
        var observer = new MutationObserver(function (mutations) {
            var selects = [];
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (!node || node.nodeType !== 1) return;
                    if (node.tagName === 'SELECT') selects.push(node);
                    if (node.querySelectorAll) {
                        node.querySelectorAll('select').forEach(function (el) {
                            selects.push(el);
                        });
                    }
                });
            });

            if (!selects.length) return;
            clearTimeout(timer);
            timer = setTimeout(function () {
                selects.forEach(init);
            }, 0);
        });

        observer.observe(document.body, { childList: true, subtree: true });
    }

    window.KrsSearchableSelect = {
        init: init,
        destroy: destroy,
        refresh: refresh,
        initAll: initAll
    };

    function boot() {
        initAll();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }

    window.addEventListener('load', boot);
    observeNewSelects();
})();
