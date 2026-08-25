(function () {
    function fmt(value) {
        return value ? value : '-';
    }

    function ensureModalElements() {
        var modalEl = document.getElementById('vehicleDetailsModal');
        var bodyEl = document.getElementById('vehicleDetailsBody');
        if (modalEl && bodyEl) {
            return { modalEl: modalEl, bodyEl: bodyEl };
        }

        modalEl = document.createElement('div');
        modalEl.className = 'modal fade';
        modalEl.id = 'vehicleDetailsModal';
        modalEl.tabIndex = -1;
        modalEl.innerHTML =
            '<div class="modal-dialog modal-lg">' +
                '<div class="modal-content">' +
                    '<div class="modal-header">' +
                        '<h5 class="modal-title"><i class="bi bi-ev-front"></i> Vehicle Details</h5>' +
                        '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>' +
                    '</div>' +
                    '<div class="modal-body" id="vehicleDetailsBody"></div>' +
                '</div>' +
            '</div>';
        document.body.appendChild(modalEl);
        return { modalEl: modalEl, bodyEl: document.getElementById('vehicleDetailsBody') };
    }

    function renderDocumentCell(booking, kind) {
        var hasFile = kind === 'invoice' ? booking.hasInvoiceFile : booking.hasInsuranceFile;
        var viewUrl = kind === 'invoice' ? booking.invoiceViewUrl : booking.insuranceViewUrl;
        var downloadUrl = kind === 'invoice' ? booking.invoiceDownloadUrl : booking.insuranceDownloadUrl;
        var milestoneDate = kind === 'invoice' ? booking.invoiceDate : booking.insuranceDate;

        if (hasFile) {
            return '<a href="' + viewUrl + '" target="_blank" rel="noopener" class="btn btn-xs btn-outline-secondary btn-sm me-1"><i class="bi bi-eye"></i> View</a>'
                + '<a href="' + downloadUrl + '" class="btn btn-xs btn-outline-secondary btn-sm"><i class="bi bi-download"></i> Download</a>';
        }
        if (milestoneDate) {
            return '<span class="text-muted">Pending upload from dealer</span>';
        }
        return '<span class="text-muted">Not available yet</span>';
    }

    function renderBookingSection(booking) {
        if (!booking) {
            return '<p class="text-muted mt-3"><em>No booking submitted yet.</em></p>';
        }

        return ''
            + '<h6 class="mt-3 border-bottom pb-1">Booking & Delivery</h6>'
            + '<table class="table table-sm table-bordered">'
            + '<tr><td width="35%"><strong>Booking Status</strong></td><td><span class="badge bg-primary">' + fmt(booking.status) + '</span></td></tr>'
            + '<tr><td><strong>Customer</strong></td><td>' + fmt(booking.customerName) + (booking.isCompanyBooking ? ' (Company)' : '') + '</td></tr>'
            + '<tr><td><strong>Mobile / Alt</strong></td><td>' + fmt(booking.customerMobile) + ' / ' + fmt(booking.alternativeMobile) + '</td></tr>'
            + '<tr><td><strong>Email</strong></td><td>' + fmt(booking.customerEmail) + '</td></tr>'
            + '<tr><td><strong>Document / RTO</strong></td><td>' + fmt(booking.documentType) + ' · ' + fmt(booking.rtoLocation) + '</td></tr>'
            + '<tr><td><strong>Payment / Financier</strong></td><td>' + fmt(booking.paymentMode) + ' · ' + fmt(booking.financier) + '</td></tr>'
            + '<tr><td><strong>Nominee</strong></td><td>' + fmt(booking.nomineeName) + ' (' + fmt(booking.nomineeRelationship) + ') · ' + fmt(booking.nomineeDob) + '</td></tr>'
            + '<tr><td><strong>Submitted</strong></td><td>' + fmt(booking.submittedDate) + '</td></tr>'
            + '<tr><td><strong>PAP Received</strong></td><td>' + fmt(booking.paperReceivedDate) + '</td></tr>'
            + '<tr><td><strong>Invoice Date</strong></td><td>' + fmt(booking.invoiceDate) + '</td></tr>'
            + '<tr><td><strong>Insurance Date</strong></td><td>' + fmt(booking.insuranceDate) + '</td></tr>'
            + '<tr><td><strong>Agent Date</strong></td><td>' + fmt(booking.agentDate) + '</td></tr>'
            + '<tr><td><strong>Registration</strong></td><td>' + fmt(booking.registrationDate) + ' · RTO: ' + fmt(booking.rtoNumber) + '</td></tr>'
            + '<tr><td><strong>Number Plate</strong></td><td>' + fmt(booking.numberPlateReceivedDate) + '</td></tr>'
            + '<tr><td><strong>Subsidy ID</strong></td><td>' + fmt(booking.subsidyId) + '</td></tr>'
            + '<tr><td><strong>Subsidy Docs</strong></td><td>' + fmt(booking.subsidyDocsSubmittedDate) + '</td></tr>'
            + '<tr><td><strong>Invoice Document</strong></td><td>' + renderDocumentCell(booking, 'invoice') + '</td></tr>'
            + '<tr><td><strong>Insurance Document</strong></td><td>' + renderDocumentCell(booking, 'insurance') + '</td></tr>'
            + '</table>';
    }

    function renderVehicleDetails(data) {
        return ''
            + '<h6 class="border-bottom pb-1">Vehicle</h6>'
            + '<table class="table table-sm">'
            + '<tr><td width="35%"><strong>Chassis</strong></td><td><code>' + data.chassisNumber + '</code></td></tr>'
            + '<tr><td><strong>Model / Color</strong></td><td>' + data.modelName + ' · ' + data.colorName + '</td></tr>'
            + '<tr><td><strong>Status</strong></td><td><span class="badge bg-primary">' + fmt(data.statusName) + '</span></td></tr>'
            + '<tr><td><strong>Motor</strong></td><td>' + fmt(data.motorNo) + '</td></tr>'
            + '<tr><td><strong>Battery</strong></td><td>' + fmt(data.batteryNo) + '</td></tr>'
            + '<tr><td><strong>Charger</strong></td><td>' + fmt(data.chargerNo) + '</td></tr>'
            + '<tr><td><strong>Controller</strong></td><td>' + fmt(data.controllerNo) + '</td></tr>'
            + '<tr><td><strong>Converter</strong></td><td>' + fmt(data.converterNo) + '</td></tr>'
            + '<tr><td><strong>Order #</strong></td><td>' + fmt(data.orderNumber) + '</td></tr>'
            + '<tr><td><strong>Order Date</strong></td><td>' + fmt(data.orderDate) + '</td></tr>'
            + '<tr><td><strong>Allocated</strong></td><td>' + fmt(data.allocatedDate) + '</td></tr>'
            + '<tr><td><strong>Subdealer</strong></td><td>' + fmt(data.subdealerName) + '</td></tr>'
            + '<tr><td><strong>Current Price</strong></td><td>₹' + Number(data.currentPrice).toLocaleString('en-IN', { minimumFractionDigits: 2 }) + '</td></tr>'
            + '<tr><td><strong>Original Price</strong></td><td>' + (data.originalPrice != null ? '₹' + Number(data.originalPrice).toLocaleString('en-IN', { minimumFractionDigits: 2 }) : '-') + '</td></tr>'
            + '<tr><td><strong>Delivery Status</strong></td><td><span class="badge bg-primary">' + data.deliveryStatus + '</span></td></tr>'
            + '<tr><td><strong>Change History</strong></td><td><small style="white-space:pre-wrap">' + fmt(data.notes || data.correctionHistory) + '</small></td></tr>'
            + '</table>'
            + renderBookingSection(data.booking);
    }

    async function openVehicleDetailsModal(vehicleId) {
        var parts = ensureModalElements();
        var modalEl = parts.modalEl;
        var bodyEl = parts.bodyEl;

        if (!vehicleId) {
            bodyEl.innerHTML = '<div class="alert alert-danger mb-0">Vehicle id is missing for this chassis link.</div>';
            bootstrap.Modal.getOrCreateInstance(modalEl).show();
            return;
        }

        bodyEl.innerHTML = '<div class="text-center text-muted py-4"><span class="spinner-border spinner-border-sm"></span> Loading...</div>';
        bootstrap.Modal.getOrCreateInstance(modalEl).show();

        try {
            if (!window.KrsQueryString || typeof window.KrsQueryString.fetchGet !== 'function') {
                bodyEl.innerHTML = '<div class="alert alert-danger mb-0">Unable to load vehicle details (query helper missing).</div>';
                return;
            }

            var resp = await window.KrsQueryString.fetchGet('/Vehicles/DetailsJson', { id: String(vehicleId) }, { krsNoLoader: true });
            var data;
            try {
                data = await resp.json();
            } catch (parseError) {
                data = { success: false, message: 'Unexpected response from server.' };
            }

            if (!resp.ok || !data.success) {
                bodyEl.innerHTML = '<div class="alert alert-danger mb-0">' + fmt(data.message || 'Unable to load vehicle details.') + '</div>';
                return;
            }

            bodyEl.innerHTML = renderVehicleDetails(data);
        } catch (loadError) {
            bodyEl.innerHTML = '<div class="alert alert-danger mb-0">Failed to load vehicle details.</div>';
        }
    }

    document.addEventListener('click', function (e) {
        var link = e.target.closest('.chassis-link');
        if (!link) return;

        e.preventDefault();
        e.stopPropagation();
        openVehicleDetailsModal(link.dataset.id || link.getAttribute('data-vehicle-id'));
    }, true);
})();
