(function () {
    var list = document.getElementById('dashboardWidgetSortable');
    if (!list) return;

    var modalEl = document.getElementById('dashboardWidgetsModal');
    if (modalEl && modalEl.parentElement !== document.body) {
        document.body.appendChild(modalEl);
    }

    var dragItem = null;

    list.addEventListener('dragstart', function (e) {
        var li = e.target.closest('li');
        if (!li || !list.contains(li)) return;
        dragItem = li;
        li.classList.add('dragging');
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', li.getAttribute('data-key') || '');
    });

    list.addEventListener('dragend', function () {
        if (dragItem) dragItem.classList.remove('dragging');
        dragItem = null;
    });

    list.addEventListener('dragover', function (e) {
        e.preventDefault();
        var target = e.target.closest('li');
        if (!target || target === dragItem || !list.contains(target)) return;
        var rect = target.getBoundingClientRect();
        var after = e.clientY > rect.top + rect.height / 2;
        if (after) {
            target.after(dragItem);
        } else {
            target.before(dragItem);
        }
    });

    var form = document.getElementById('dashboardWidgetsForm');
    if (form) {
        form.addEventListener('submit', function () {
            list.querySelectorAll('input[name="widgetOrder"]').forEach(function (input) {
                input.remove();
            });
            list.querySelectorAll('li[data-key]').forEach(function (li) {
                var hidden = document.createElement('input');
                hidden.type = 'hidden';
                hidden.name = 'widgetOrder';
                hidden.value = li.getAttribute('data-key') || '';
                form.appendChild(hidden);
            });
        });
    }
})();
