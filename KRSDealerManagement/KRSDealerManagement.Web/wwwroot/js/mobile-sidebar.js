(function () {
    var mobileQuery = window.matchMedia('(max-width: 991.98px)');
    var html = document.documentElement;
    var bodyObserver = null;

    function isMobileNav() {
        return mobileQuery.matches;
    }

    function isSidebarOpen() {
        return document.body.classList.contains('sidebar-open');
    }

    function clearScrollLock() {
        html.classList.remove('krs-nav-scroll-lock');
        document.body.classList.remove('krs-scroll-lock');
        document.body.style.top = '';
        document.body.style.position = '';
        document.body.style.width = '';
        document.body.style.overflow = '';
        delete document.body.dataset.krsScrollY;
    }

    function syncScrollLock() {
        if (!isMobileNav()) {
            clearScrollLock();
            return;
        }

        if (isSidebarOpen()) {
            html.classList.add('krs-nav-scroll-lock');
        } else {
            clearScrollLock();
        }
    }

    function toggleSidebarViaAdminLTE() {
        var toggle = document.querySelector('[data-widget="pushmenu"]');
        if (toggle) {
            toggle.click();
            return;
        }

        document.body.classList.toggle('sidebar-open');
        syncScrollLock();
    }

    function closeSidebar() {
        if (!isSidebarOpen()) {
            syncScrollLock();
            return;
        }
        toggleSidebarViaAdminLTE();
    }

    function syncMobileNavMode() {
        document.body.classList.toggle('krs-mobile-nav', isMobileNav());
        if (!isMobileNav()) {
            clearScrollLock();
        } else {
            syncScrollLock();
        }
    }

    function observeBodyClasses() {
        if (bodyObserver || !document.body) return;

        var syncTimer = null;
        var lastSidebarOpen = null;

        function scheduleSync() {
            if (syncTimer) window.clearTimeout(syncTimer);
            syncTimer = window.setTimeout(function () {
                syncTimer = null;
                var open = isSidebarOpen();
                if (open === lastSidebarOpen) return;
                lastSidebarOpen = open;
                syncScrollLock();
            }, 0);
        }

        bodyObserver = new MutationObserver(function (mutations) {
            var sidebarChanged = mutations.some(function (mutation) {
                if (mutation.attributeName !== 'class') return false;
                var before = mutation.oldValue || '';
                var after = document.body.className || '';
                return before.indexOf('sidebar-open') !== after.indexOf('sidebar-open');
            });

            if (!sidebarChanged) return;
            scheduleSync();
        });

        bodyObserver.observe(document.body, {
            attributes: true,
            attributeFilter: ['class'],
            attributeOldValue: true
        });
    }

    function bindMobileTreeview() {
        document.querySelectorAll('.main-sidebar .has-treeview > a.nav-link').forEach(function (link) {
            if (link.dataset.krsTreeBound === '1') return;
            link.dataset.krsTreeBound = '1';

            link.addEventListener('click', function (event) {
                if (!isMobileNav()) return;

                var href = link.getAttribute('href');
                if (href && href !== '#') return;

                event.preventDefault();
                event.stopImmediatePropagation();

                var item = link.closest('.nav-item.has-treeview');
                if (!item) return;

                var willOpen = !item.classList.contains('menu-open');
                var parentList = item.parentElement;
                if (parentList) {
                    parentList.querySelectorAll(':scope > .nav-item.has-treeview.menu-open').forEach(function (openItem) {
                        if (openItem !== item) openItem.classList.remove('menu-open');
                    });
                }

                item.classList.toggle('menu-open', willOpen);

                if (willOpen) {
                    window.setTimeout(function () {
                        var sidebar = document.querySelector('.main-sidebar');
                        if (!sidebar) return;
                        var itemBottom = item.getBoundingClientRect().bottom;
                        var sidebarBottom = sidebar.getBoundingClientRect().bottom;
                        if (itemBottom > sidebarBottom - 24) {
                            sidebar.scrollTop += itemBottom - sidebarBottom + 64;
                        }
                    }, 100);
                }
            }, true);
        });
    }

    function bindMobileSidebar() {
        document.querySelectorAll('.main-sidebar a.nav-link[href]').forEach(function (link) {
            var href = link.getAttribute('href');
            if (!href || href === '#') return;
            if (link.dataset.krsMobileNavBound === '1') return;
            link.dataset.krsMobileNavBound = '1';
            link.addEventListener('click', function () {
                if (isMobileNav()) closeSidebar();
            });
        });

        document.querySelectorAll('[data-widget="pushmenu"]').forEach(function (btn) {
            if (btn.dataset.krsPushmenuBound === '1') return;
            btn.dataset.krsPushmenuBound = '1';
            btn.addEventListener('click', function () {
                if (!isMobileNav()) return;
                window.setTimeout(syncScrollLock, 0);
                window.setTimeout(syncScrollLock, 120);
            });
        });
    }

    function bindAdminLteEvents() {
        if (!window.jQuery) return;
        window.jQuery(document)
            .on('shown.lte.pushmenu collapsed.lte.pushmenu expanded.lte.pushmenu', syncScrollLock);
    }

    document.addEventListener('click', function (event) {
        if (!isMobileNav() || !isSidebarOpen()) return;

        var sidebar = document.querySelector('.main-sidebar');
        var toggle = document.querySelector('[data-widget="pushmenu"]');
        if (!sidebar) return;

        var target = event.target;
        if (sidebar.contains(target)) return;
        if (toggle && (toggle === target || toggle.contains(target))) return;

        closeSidebar();
    });

    mobileQuery.addEventListener('change', syncMobileNavMode);

    function initMobileNav() {
        clearScrollLock();
        observeBodyClasses();
        syncMobileNavMode();
        bindMobileTreeview();
        bindMobileSidebar();
        bindAdminLteEvents();
    }

    window.addEventListener('pageshow', syncScrollLock);
    window.addEventListener('orientationchange', function () {
        window.setTimeout(syncScrollLock, 150);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initMobileNav);
    } else {
        initMobileNav();
    }
})();
