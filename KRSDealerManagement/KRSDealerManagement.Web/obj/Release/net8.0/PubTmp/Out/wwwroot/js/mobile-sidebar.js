(function () {
    var mobileQuery = window.matchMedia('(max-width: 991.98px)');
    var html = document.documentElement;
    var bodyObserver = null;

    function isMobileNav() {
        return mobileQuery.matches;
    }

    function isDesktopNav() {
        return !mobileQuery.matches;
    }

    function isSidebarOpen() {
        return document.body.classList.contains('sidebar-open');
    }

    function isDesktopCollapsed() {
        return isDesktopNav() && document.body.classList.contains('sidebar-collapse');
    }

    function clearScrollLock() {
        var scrollY = parseInt(document.body.dataset.krsScrollY || '', 10);

        html.classList.remove('krs-nav-scroll-lock');
        html.style.overflow = '';
        html.style.height = '';
        html.style.position = '';

        document.body.classList.remove('krs-scroll-lock');
        document.body.style.top = '';
        document.body.style.left = '';
        document.body.style.right = '';
        document.body.style.position = '';
        document.body.style.width = '';
        document.body.style.height = '';
        document.body.style.overflow = '';
        delete document.body.dataset.krsScrollY;

        if (!Number.isNaN(scrollY) && scrollY > 0) {
            window.scrollTo(0, scrollY);
        }
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

    function persistCollapsedSidebarState() {
        if (!window.localStorage) return;

        try {
            localStorage.setItem('AdminLTE:Sidebar:state', 'collapsed');
        } catch (error) {
            // ignore storage errors
        }
    }

    function applyDesktopCollapsedMenu() {
        if (!isDesktopNav()) return;

        var body = document.body;
        body.classList.remove('sidebar-mini', 'sidebar-open');
        body.classList.add('sidebar-collapse');
        persistCollapsedSidebarState();
    }

    function applyMobileCollapsedMenu() {
        if (!isMobileNav()) return;

        var body = document.body;
        body.classList.remove('sidebar-mini', 'sidebar-open');
        body.classList.add('sidebar-collapse');
        clearScrollLock();
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
            clearScrollLock();
            return;
        }

        if (isDesktopCollapsed()) {
            document.body.classList.remove('sidebar-open');
            clearScrollLock();
            return;
        }

        toggleSidebarViaAdminLTE();
        window.setTimeout(clearScrollLock, 0);
        window.setTimeout(clearScrollLock, 150);
    }

    function syncMobileNavMode() {
        document.body.classList.toggle('krs-mobile-nav', isMobileNav());
        if (!isMobileNav()) {
            clearScrollLock();
            applyDesktopCollapsedMenu();
        } else {
            applyMobileCollapsedMenu();
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

    function getSidebarNavScroller() {
        return document.querySelector('.main-sidebar .krs-sidebar-nav')
            || document.querySelector('.main-sidebar .sidebar > nav');
    }

    /** Keep the expanded section header near the top of the menu scroller (not the page bottom). */
    function scrollTreeviewOnExpand(item) {
        var scroller = getSidebarNavScroller();
        if (!scroller || !item) return;

        window.requestAnimationFrame(function () {
            var anchor = item.querySelector(':scope > a.nav-link') || item;
            var scrollerRect = scroller.getBoundingClientRect();
            var anchorRect = anchor.getBoundingClientRect();
            var nextTop = scroller.scrollTop + (anchorRect.top - scrollerRect.top) - 8;

            scroller.scrollTo({
                top: Math.max(0, nextTop),
                behavior: 'smooth'
            });
        });
    }

    function bindMobileTreeview() {
        document.querySelectorAll('.main-sidebar .has-treeview > a.nav-link').forEach(function (link) {
            if (link.dataset.krsTreeBound === '1') return;
            link.dataset.krsTreeBound = '1';

            link.addEventListener('click', function (event) {
                if (!isMobileNav() && !isDesktopCollapsed()) return;

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
                    scrollTreeviewOnExpand(item);
                }
            }, true);
        });
    }

    function bindTreeviewScrollObserver() {
        var nav = getSidebarNavScroller();
        if (!nav || nav.dataset.krsTreeScrollObs === '1') return;
        nav.dataset.krsTreeScrollObs = '1';

        var scrollFromUser = false;

        nav.addEventListener('click', function (event) {
            if (isMobileNav() || isDesktopCollapsed()) return;

            var link = event.target.closest('.has-treeview > a.nav-link');
            if (!link) return;
            var href = link.getAttribute('href');
            if (href && href !== '#') return;
            scrollFromUser = true;
        }, true);

        new MutationObserver(function (mutations) {
            if (isMobileNav() || isDesktopCollapsed()) return;

            mutations.forEach(function (mutation) {
                if (mutation.attributeName !== 'class') return;
                if (!scrollFromUser) return;

                var item = mutation.target;
                if (!(item instanceof HTMLElement)) return;
                if (!item.classList.contains('has-treeview')) return;
                if (!item.classList.contains('menu-open')) return;

                scrollFromUser = false;
                scrollTreeviewOnExpand(item);
            });
        }).observe(nav, {
            attributes: true,
            attributeFilter: ['class'],
            subtree: true
        });
    }

    function bindNavLinkClose() {
        document.querySelectorAll('.main-sidebar a.nav-link[href]').forEach(function (link) {
            var href = link.getAttribute('href');
            if (!href || href === '#') return;
            if (link.dataset.krsMobileNavBound === '1') return;
            link.dataset.krsMobileNavBound = '1';
            link.addEventListener('click', function () {
                if (isMobileNav() || isDesktopCollapsed()) {
                    closeSidebar();
                }
            });
        });
    }

    function bindDesktopCollapseFlyout() {
        document.querySelectorAll('[data-widget="pushmenu"]').forEach(function (btn) {
            if (btn.dataset.krsDesktopFlyoutBound === '1') return;
            btn.dataset.krsDesktopFlyoutBound = '1';

            btn.addEventListener('click', function (event) {
                if (!isDesktopNav()) return;

                event.preventDefault();
                event.stopPropagation();
                event.stopImmediatePropagation();
                document.body.classList.toggle('sidebar-open');
                return false;
            }, true);
        });
    }

    function bindPushmenuSync() {
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
            .on('shown.lte.pushmenu collapsed.lte.pushmenu expanded.lte.pushmenu', function () {
                document.body.classList.remove('sidebar-mini');

                if (isDesktopNav()) {
                    applyDesktopCollapsedMenu();
                } else if (!isSidebarOpen()) {
                    clearScrollLock();
                }

                syncScrollLock();
            });
    }

    document.addEventListener('click', function (event) {
        if (!isSidebarOpen()) return;

        var sidebar = document.querySelector('.main-sidebar');
        var toggle = document.querySelector('[data-widget="pushmenu"]');
        if (!sidebar) return;

        var target = event.target;
        if (sidebar.contains(target)) return;
        if (toggle && (toggle === target || toggle.contains(target))) return;

        if (isMobileNav() || isDesktopCollapsed()) {
            closeSidebar();
        }
    });

    mobileQuery.addEventListener('change', syncMobileNavMode);

    function initMobileNav() {
        clearScrollLock();
        observeBodyClasses();
        syncMobileNavMode();
        bindMobileTreeview();
        bindTreeviewScrollObserver();
        bindNavLinkClose();
        bindDesktopCollapseFlyout();
        bindPushmenuSync();
        bindAdminLteEvents();

        window.setTimeout(function () {
            if (isMobileNav()) {
                applyMobileCollapsedMenu();
            } else {
                applyDesktopCollapsedMenu();
            }
            clearScrollLock();
        }, 0);
        window.setTimeout(function () {
            if (isMobileNav()) {
                applyMobileCollapsedMenu();
            } else {
                applyDesktopCollapsedMenu();
            }
            clearScrollLock();
        }, 120);
    }

    window.addEventListener('pageshow', function () {
        if (isMobileNav()) {
            applyMobileCollapsedMenu();
        } else {
            applyDesktopCollapsedMenu();
        }
        clearScrollLock();
        syncScrollLock();
    });

    window.addEventListener('orientationchange', function () {
        window.setTimeout(function () {
            if (isMobileNav()) {
                applyMobileCollapsedMenu();
            } else {
                applyDesktopCollapsedMenu();
            }
            clearScrollLock();
            syncScrollLock();
        }, 150);
    });

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initMobileNav);
    } else {
        initMobileNav();
    }
})();
