(function () {
    var loaderEl = null;
    var textEl = null;
    var asyncCount = 0;
    var navCount = 0;
    var navFallbackTimer = null;
    var nativeAssign = window.location.assign.bind(window.location);
    var nativeReplace = window.location.replace.bind(window.location);

    function getLoaderElements() {
        if (!loaderEl) {
            loaderEl = document.getElementById('krsPageLoader');
            textEl = loaderEl ? loaderEl.querySelector('.krs-page-loader-text') : null;
        }
        return loaderEl;
    }

    function setVisible(show) {
        var el = getLoaderElements();
        if (!el) return;
        el.hidden = !show;
        el.classList.toggle('krs-page-loader-visible', show);
        el.setAttribute('aria-busy', show ? 'true' : 'false');
    }

    function syncVisible() {
        setVisible(asyncCount > 0 || navCount > 0);
    }

    function showLoader(message, mode) {
        var el = getLoaderElements();
        if (!el) return;

        if (message && textEl) {
            textEl.textContent = message;
        } else if (textEl && !textEl.textContent) {
            textEl.textContent = 'Please wait...';
        }

        if (mode === 'async') {
            asyncCount++;
        } else {
            navCount++;
        }

        syncVisible();
    }

    function hideLoader(mode) {
        if (mode === 'async') {
            asyncCount = Math.max(0, asyncCount - 1);
        } else {
            navCount = Math.max(0, navCount - 1);
        }

        syncVisible();
    }

    function resetLoader() {
        asyncCount = 0;
        navCount = 0;
        clearNavFallback();
        syncVisible();
    }

    function clearNavFallback() {
        if (navFallbackTimer) {
            window.clearTimeout(navFallbackTimer);
            navFallbackTimer = null;
        }
    }

    function isSameOriginNavigation(href) {
        if (!href || href.charAt(0) === '#') return false;
        if (/^(javascript:|mailto:|tel:)/i.test(href)) return false;

        try {
            var url = new URL(href, window.location.href);
            return url.origin === window.location.origin;
        } catch (e) {
            return false;
        }
    }

    function isFileDownloadUrl(href) {
        if (!href) return false;

        try {
            var path = new URL(href, window.location.href).pathname.toLowerCase();
            return /\/export[^/]*$/i.test(path)
                || /\/downloadtemplate$/i.test(path)
                || /\/download$/i.test(path)
                || /\/viewfile$/i.test(path);
        } catch (e) {
            return false;
        }
    }

    function scheduleNavLoaderFallback() {
        clearNavFallback();

        var navigated = false;
        function onPageHide() {
            navigated = true;
        }

        window.addEventListener('pagehide', onPageHide, { once: true });
        navFallbackTimer = window.setTimeout(function () {
            navFallbackTimer = null;
            window.removeEventListener('pagehide', onPageHide);
            if (!navigated && navCount > 0) {
                navCount = 0;
                syncVisible();
            }
        }, 1200);
    }

    function beginNavLoader(message) {
        showLoader(message || 'Loading...', 'nav');
        scheduleNavLoaderFallback();
    }

    function shouldSkipFetchLoader(input, init) {
        if (init && init.krsNoLoader) return true;

        var url = '';
        if (typeof input === 'string') {
            url = input;
        } else if (input && typeof input.url === 'string') {
            url = input.url;
        }

        url = url.toLowerCase();
        if (!url) return false;

        return url.indexOf('/grids/distinctvalues') >= 0;
    }

    function shouldSkipLink(el) {
        if (!el || el.dataset.noLoader === 'true') return true;
        if (el.classList.contains('chassis-link')) return true;
        if (el.target === '_blank' || el.hasAttribute('download')) return true;
        if (isFileDownloadUrl(el.href)) return true;
        if (el.getAttribute('data-bs-toggle') || el.getAttribute('data-toggle')) return true;
        if (el.getAttribute('role') === 'button' && (el.getAttribute('href') || '') === '#') return true;
        return !isSameOriginNavigation(el.href);
    }

    function shouldSkipForm(form) {
        if (!form || form.tagName !== 'FORM') return true;
        if (form.dataset.noLoader === 'true') return true;
        return false;
    }

    window.krsShowLoader = function (message) {
        showLoader(message || 'Please wait...', 'async');
    };

    window.krsHideLoader = function () {
        hideLoader('async');
    };

    window.krsResetLoader = function () {
        resetLoader();
    };

    window.krsNavigate = function (url, message) {
        if (!url) return;
        if (isFileDownloadUrl(url)) {
            nativeAssign(url);
            return;
        }

        beginNavLoader(message || 'Loading...');
        nativeAssign(url);
    };

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (shouldSkipForm(form)) return;
        if (e.defaultPrevented) return;

        beginNavLoader(form.dataset.loaderMessage || 'Processing...');
    });

    document.addEventListener('click', function (e) {
        if (e.defaultPrevented) return;
        if (e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

        var link = e.target.closest('a[href]');
        if (!link || shouldSkipLink(link)) return;

        var href = link.getAttribute('href') || '';
        if (!href || href === '#') return;

        beginNavLoader(link.dataset.loaderMessage || 'Loading...');
    });

    if (window.fetch) {
        var nativeFetch = window.fetch.bind(window);
        window.fetch = function (input, init) {
            if (shouldSkipFetchLoader(input, init)) {
                return nativeFetch(input, init);
            }

            showLoader('Loading...', 'async');
            return nativeFetch(input, init)
                .finally(function () {
                    hideLoader('async');
                });
        };
    }

    if (window.XMLHttpRequest) {
        var xhrProto = XMLHttpRequest.prototype;
        var nativeOpen = xhrProto.open;
        var nativeSend = xhrProto.send;

        xhrProto.open = function (method, url) {
            this._krsSkipLoader = ((url || '') + '').toLowerCase().indexOf('/grids/distinctvalues') >= 0;
            return nativeOpen.apply(this, arguments);
        };

        xhrProto.send = function () {
            if (!this._krsSkipLoader) {
                showLoader('Loading...', 'async');
                this.addEventListener('loadend', function () {
                    hideLoader('async');
                }, { once: true });
            }
            return nativeSend.apply(this, arguments);
        };
    }

    if (window.jQuery) {
        jQuery(document).ajaxStart(function () {
            showLoader('Loading...', 'async');
        });
        jQuery(document).ajaxStop(function () {
            hideLoader('async');
        });
    }

    window.location.assign = function (url) {
        if (isFileDownloadUrl(url)) {
            return nativeAssign(url);
        }

        beginNavLoader('Loading...');
        return nativeAssign(url);
    };

    window.location.replace = function (url) {
        if (isFileDownloadUrl(url)) {
            return nativeReplace(url);
        }

        beginNavLoader('Loading...');
        return nativeReplace(url);
    };

    window.addEventListener('pageshow', function () {
        resetLoader();
    });

    window.addEventListener('load', function () {
        resetLoader();
    });
})();
