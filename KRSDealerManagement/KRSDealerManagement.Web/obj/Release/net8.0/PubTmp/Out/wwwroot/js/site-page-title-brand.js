(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var brandRow = document.querySelector('.page-title-brand-row');
        if (!brandRow) {
            return;
        }

        var titleHost = brandRow.querySelector('.page-title-brand-row__title');
        var actionsHost = brandRow.querySelector('.page-title-brand-row__actions');
        var container = brandRow.parentElement;
        if (!titleHost || !container) {
            return;
        }

        var firstRow = null;
        for (var i = 0; i < container.children.length; i++) {
            var child = container.children[i];
            if (child === brandRow) {
                continue;
            }

            if (child.classList && child.classList.contains('row') && child.classList.contains('mb-3')) {
                firstRow = child;
                break;
            }
        }

        if (!firstRow) {
            return;
        }

        var cols = firstRow.querySelectorAll(':scope > [class*="col-"]');
        if (!cols.length) {
            return;
        }

        var titleCol = cols[0];
        while (titleCol.firstChild) {
            titleHost.appendChild(titleCol.firstChild);
        }

        if (cols.length > 1 && actionsHost) {
            var actionsCol = cols[1];
            var hasActions = actionsCol.querySelector('.btn, a.btn, button');
            if (hasActions) {
                while (actionsCol.firstChild) {
                    actionsHost.appendChild(actionsCol.firstChild);
                }
            }
        }

        firstRow.remove();
    });
})();
