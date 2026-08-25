(function () {
    var loaderEl = null;
    var textEl = null;
    var asyncCount = 0;
    var navActive = false;

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
            navActive = true;
        }

        setVisible(true);
    }

    function hideLoader(mode) {
        if (mode === 'async') {
            asyncCount = Math.max(0, asyncCount - 1);
            if (asyncCount === 0 && !navActive) {
                setVisible(false);
            }
            return;
        }

        navActive = false;
        if (asyncCount === 0) {
            setVisible(false);
        }
    }

    function resetLoader() {
        asyncCount = 0;
        navActive = false;
        setVisible(false);
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

    function resolveFetchUrl(input) {
        if (typeof input === 'string') return input;
        if (input && typeof input.url === 'string') return input.url;
        return '';
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

    function shouldSkipLink(el) {
        if (!el || el.dataset.noLoader === 'true') return true;
        if (el.classList.contains('chassis-link')) return true;
        if (el.target === '_blank' || el.hasAttribute('download')) return true;
        if (el.getAttribute('data-bs-toggle') || el.getAttribute('data-toggle')) return true;
        if (el.getAttribute('role') === 'button' && (el.getAttribute('href') || '') === '#') return true;
        return !isSameOriginNavigation(el.href);
    }

    window.krsShowLoader = function (message) {
        showLoader(message || 'Please wait...', 'async');
    };

    window.krsHideLoader = function () {
        hideLoader('async');
    };

    window.krsNavigate = function (url, message) {
        if (!url) return;
        showLoader(message || 'Loading...', 'nav');
        window.location.assign(url);
    };

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.tagName !== 'FORM') return;
        if (form.dataset.noLoader === 'true') return;
        if (e.defaultPrevented) return;

        var message = form.dataset.loaderMessage || 'Processing...';
        showLoader(message, 'nav');
    }, true);

    document.addEventListener('click', function (e) {
        if (e.defaultPrevented) return;
        if (e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;

        var link = e.target.closest('a[href]');
        if (!link || shouldSkipLink(link)) return;

        var href = link.getAttribute('href') || '';
        if (!href || href === '#') return;

        showLoader(link.dataset.loaderMessage || 'Loading...', 'nav');
    }, true);

    if (window.fetch) {
        var nativeFetch = window.fetch.bind(window);
        window.fetch = function (input, init) {
            if (shouldSkipFetchLoader(input, init)) {
                return nativeFetch(input, init);
            }

            showLoader('Loading...', 'async');
            return nativeFetch(input, init)
                .then(function (response) {
                    return response;
                })
                .catch(function (error) {
                    throw error;
                })
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

    var nativeAssign = window.location.assign.bind(window.location);
    window.location.assign = function (url) {
        showLoader('Loading...', 'nav');
        return nativeAssign(url);
    };

    var nativeReplace = window.location.replace.bind(window.location);
    window.location.replace = function (url) {
        showLoader('Loading...', 'nav');
        return nativeReplace(url);
    };

    window.addEventListener('pageshow', function () {
        resetLoader();
    });

    window.addEventListener('load', function () {
        resetLoader();
    });
})();
