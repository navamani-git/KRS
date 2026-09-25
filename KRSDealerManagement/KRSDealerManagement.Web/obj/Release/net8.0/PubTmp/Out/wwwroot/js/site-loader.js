(function () {
    var loaderEl = null;
    var textEl = null;
    var asyncCount = 0;
    var navCount = 0;
    var initialLoadActive = document.readyState !== 'complete';
    var pageLoadEventFired = document.readyState === 'complete';
    var pageTitle = document.documentElement.dataset.krsPageTitle || document.title;
    var nativeAssign = window.location.assign.bind(window.location);
    var nativeReplace = window.location.replace.bind(window.location);

    document.documentElement.classList.add('krs-page-loading');
    setLoadingTitle();

    function getLoaderElements() {
        if (!loaderEl) {
            loaderEl = document.getElementById('krsPageLoader');
            textEl = loaderEl ? loaderEl.querySelector('.krs-page-loader-text') : null;
        }
        return loaderEl;
    }

    function setLoadingTitle() {
        if (document.title.indexOf('\u27F3 ') !== 0) {
            document.title = '\u27F3 Loading...';
        }
    }

    function restorePageTitle() {
        document.title = pageTitle;
    }

    function setVisible(show) {
        var el = getLoaderElements();
        if (!el) return;
        el.hidden = !show;
        el.classList.toggle('krs-page-loader-visible', show);
        el.setAttribute('aria-busy', show ? 'true' : 'false');
    }

    function syncVisible() {
        var show = initialLoadActive || asyncCount > 0 || navCount > 0;
        setVisible(show);
        if (show) {
            document.documentElement.classList.add('krs-page-loading');
            setLoadingTitle();
        } else {
            document.documentElement.classList.remove('krs-page-loading');
            restorePageTitle();
        }
    }

    function tryFinishInitialLoad() {
        if (!initialLoadActive || !pageLoadEventFired || asyncCount > 0) {
            return;
        }

        initialLoadActive = false;
        syncVisible();
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
        }

        syncVisible();
        tryFinishInitialLoad();
    }

    function resetLoader() {
        asyncCount = 0;
        navCount = 0;
        initialLoadActive = false;
        syncVisible();
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

    function isLongRunningForm(form) {
        if (!form || form.tagName !== 'FORM') return false;
        if (form.dataset.longSubmit === 'true') return true;
        var enc = (form.getAttribute('enctype') || '').toLowerCase();
        return enc.indexOf('multipart') >= 0;
    }

    function beginNavLoader(message) {
        showLoader(message || 'Loading...', 'nav');
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

        return url.indexOf('/grids/distinctvalues') >= 0
            || isDevConnection(url);
    }

    function isDevConnection(url) {
        if (!url) return false;
        return url.indexOf('aspnetcore-browser-refresh') >= 0
            || url.indexOf('browserlink') >= 0
            || url.indexOf('browser-refresh') >= 0
            || url.indexOf('/negotiate') >= 0
            || url.indexOf('/_framework/') >= 0
            || url.indexOf('/_vs/') >= 0;
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

    function bindJQueryAjax() {
        if (!window.jQuery || window.jQuery.__krsLoaderBound) return;
        window.jQuery.__krsLoaderBound = true;
        window.jQuery(document).ajaxStart(function () {
            showLoader('Loading...', 'async');
        });
        window.jQuery(document).ajaxStop(function () {
            hideLoader('async');
        });
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
            var request = nativeFetch(input, init).then(function (response) {
                if (response.status !== 401) return response;
                return response.clone().json().then(function (body) {
                    if (body && body.loginRequired) {
                        nativeAssign(body.redirectUrl || '/Account/Login');
                    }
                    return response;
                }).catch(function () {
                    return response;
                });
            });

            if (shouldSkipFetchLoader(input, init)) {
                return request;
            }

            showLoader('Loading...', 'async');
            return request.finally(function () {
                hideLoader('async');
            });
        };
    }

    if (window.XMLHttpRequest) {
        var xhrProto = XMLHttpRequest.prototype;
        var nativeOpen = xhrProto.open;
        var nativeSend = xhrProto.send;

        xhrProto.open = function (method, url) {
            var requestUrl = ((url || '') + '').toLowerCase();
            this._krsSkipLoader = requestUrl.indexOf('/grids/distinctvalues') >= 0
                || isDevConnection(requestUrl);
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

    function initBackButton() {
        var btn = document.getElementById('krsBackButton');
        if (!btn || btn.dataset.krsBackInit === '1') return;
        btn.dataset.krsBackInit = '1';

        btn.addEventListener('click', function () {
            var fallbackUrl = btn.getAttribute('data-fallback-url') || '/';
            if (window.history.length > 1) {
                beginNavLoader('Loading...');
                window.history.back();
                return;
            }

            if (window.krsNavigate) {
                window.krsNavigate(fallbackUrl, 'Loading...');
            } else {
                nativeAssign(fallbackUrl);
            }
        });
    }

    function onPageFullyReady() {
        pageLoadEventFired = true;
        tryFinishInitialLoad();
    }

    syncVisible();

    if (!pageLoadEventFired) {
        window.addEventListener('load', onPageFullyReady, { once: true });
    } else {
        onPageFullyReady();
    }

    window.addEventListener('pageshow', function (e) {
        if (e.persisted) {
            pageLoadEventFired = true;
            resetLoader();
        }
    });

    bindJQueryAjax();
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            bindJQueryAjax();
            initBackButton();
        });
    } else {
        initBackButton();
    }

    window.setTimeout(bindJQueryAjax, 0);
})();
