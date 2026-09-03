(function () {
    var root = document.getElementById('krsQuickActions');
    if (!root) return;

    var fab = document.getElementById('krsQuickActionsFab');
    var modalEl = document.getElementById('quickActionsModal');
    var isOpen = false;

    if (modalEl && modalEl.parentElement !== document.body) {
        document.body.appendChild(modalEl);
    }

    function setOpen(open) {
        isOpen = open;
        root.classList.toggle('is-open', open);
        if (fab) fab.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    if (fab) {
        fab.addEventListener('click', function (e) {
            e.stopPropagation();
            setOpen(!isOpen);
        });
    }

    document.addEventListener('click', function (e) {
        if (!isOpen) return;
        if (root.contains(e.target)) return;
        if (modalEl && modalEl.contains(e.target)) return;
        setOpen(false);
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && isOpen) setOpen(false);
    });

    if (modalEl) {
        modalEl.addEventListener('show.bs.modal', function () {
            setOpen(false);
        });
    }
})();
