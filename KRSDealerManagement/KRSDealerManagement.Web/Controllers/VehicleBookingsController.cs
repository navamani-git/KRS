using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Infrastructure.Repositories;
using MediatR;

namespace KRSDealerManagement.Web.Controllers
{
  public class VehicleBookingsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly IStatusLookupService _statuses;
        private readonly IWebHostEnvironment _env;
        private readonly IVehiclePriceService _priceService;

        public VehicleBookingsController(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            IStatusLookupService statuses,
            IWebHostEnvironment env,
            IVehiclePriceService priceService)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _statuses = statuses;
            _env = env;
            _priceService = priceService;
        }

        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.BookedToCustomerView)]
        public Task<IActionResult> BookedToCustomer(int? subdealerId, int? dealershipId, string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(UnifiedVehicleStatus.BookedToCustomer, subdealerId, dealershipId, searchTerm, page, pageSize, viewOnly: true, bookedToCustomerView: true);

        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.VehicleBookings)]
        public Task<IActionResult> Process(int? subdealerId, int? dealershipId, string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(null, subdealerId, dealershipId, searchTerm, page, pageSize, viewOnly: false, bookingPhaseOnly: true);

        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.VehicleBookings)]
        public Task<IActionResult> Index(int? status, int? subdealerId, int? dealershipId, string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(status, subdealerId, dealershipId, searchTerm, page, pageSize, viewOnly: true);

        private async Task<IActionResult> ListBookingsAsync(
            int? status,
            int? subdealerId,
            int? dealershipId,
            string? searchTerm,
            int? page,
            int? pageSize,
            bool viewOnly,
            bool bookingPhaseOnly = false,
            bool bookedToCustomerView = false)
        {
            var (scopedIds, effectiveDealershipId, isAdmin) = await GetBookingScopeAsync(dealershipId);
            if (subdealerId.HasValue && !scopedIds.Contains(subdealerId.Value))
                subdealerId = null;

            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToList();
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);

            var rows = new List<VehicleBookingGridRowDto>();
            foreach (var b in bookings.Where(b => scopedIds.Contains(b.SubdealerId)))
            {
                vehicles.TryGetValue(b.VehicleId, out var v);
                users.TryGetValue(b.SubdealerId, out var u);
                var vehicleStatus = v?.Status ?? b.BookingStatus;
                var statusName = await _statuses.GetNameAsync(StatusCategories.Vehicle, vehicleStatus);
                rows.Add(new VehicleBookingGridRowDto
                {
                    Booking = b,
                    VehicleId = b.VehicleId,
                    Chassis = v?.ChassisNumber ?? "-",
                    Subdealer = u?.GetFullName() ?? "Unknown",
                    StatusName = statusName,
                    VehicleStatus = vehicleStatus
                });
            }

            var list = rows.AsEnumerable();
            if (status.HasValue)
                list = list.Where(x => BookingStageFilter.MatchesStage(
                    x.VehicleStatus,
                    status.Value,
                    x.Booking.PaperReceivedDate,
                    x.Booking.InvoiceDate,
                    x.Booking.InsuranceDate,
                    x.Booking.AgentDate,
                    x.Booking.RegistrationDate,
                    x.Booking.SubsidyId));
            else if (bookingPhaseOnly)
                list = list.Where(x => BookingStageFilter.IsBookingPhase(
                    BookingStageFilter.ResolveEffectiveStage(
                        x.VehicleStatus,
                        x.Booking.PaperReceivedDate,
                        x.Booking.InvoiceDate,
                        x.Booking.InsuranceDate,
                        x.Booking.AgentDate,
                        x.Booking.RegistrationDate,
                        x.Booking.SubsidyId)));
            if (subdealerId.HasValue)
                list = list.Where(x => x.Booking.SubdealerId == subdealerId.Value);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                list = list.Where(x => x.Chassis.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || x.Subdealer.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || (x.Booking.CustomerName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.VehicleBookings);
            var items = GridScreenFilterHelper.ApplyVehicleBookings(
                list.OrderByDescending(x => x.Booking.SubmittedDate),
                columnFilters).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(items, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle);
            ViewBag.Subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = effectiveDealershipId });
            ViewBag.Dealerships = isAdmin
                ? await _mediator.Send(new GetDealershipsQuery { IsActive = true })
                : Enumerable.Empty<DealershipDto>();
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSubdealerId = subdealerId;
            ViewBag.SelectedDealershipId = effectiveDealershipId;
            ViewBag.ShowDealershipFilter = isAdmin;
            ViewBag.LockStatusFilter = status.HasValue && viewOnly;
            ViewBag.ShowManage = !viewOnly;
            ViewBag.PageTitle = GetBookingPageTitle(status, viewOnly);
            ViewBag.BookingPhaseOnly = bookingPhaseOnly;
            ViewBag.BookedToCustomerView = bookedToCustomerView;
            var stageInfo = BookingGridStageInfo.Describe(status, bookingPhaseOnly, bookedToCustomerView);
            ViewBag.GridStageShowing = stageInfo.Showing;
            ViewBag.GridStageRemovedWhen = stageInfo.RemovedWhen;
            ViewBag.GridStageHint = BookingGridStageInfo.FormatForHeader(status, bookingPhaseOnly, bookedToCustomerView);
            ViewBag.SearchTerm = searchTerm;
            return View("Index", pageItems);
        }

        [AuthorizeRole(1, 4)]
        [AuthorizeMenuAny(StaffMenuAccess.VehicleBookings, StaffMenuAccess.BookedToCustomerView)]
        public async Task<IActionResult> Export(int? status, int? subdealerId, int? dealershipId, string? searchTerm)
        {
            var (scopedIds, _, _) = await GetBookingScopeAsync(dealershipId);
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToList();
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var statusMap = (await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle))
                .ToDictionary(s => s.StatusValue, s => s.StatusName);

            var list = bookings
                .Where(b => scopedIds.Contains(b.SubdealerId))
                .Select(b =>
                {
                    vehicles.TryGetValue(b.VehicleId, out var v);
                    users.TryGetValue(b.SubdealerId, out var u);
                    return new { Booking = b, Chassis = v?.ChassisNumber ?? "-", Subdealer = u?.GetFullName() ?? "Unknown", VehicleStatus = v?.Status ?? b.BookingStatus };
                });

            if (status.HasValue)
                list = list.Where(x => BookingStageFilter.MatchesStage(
                    x.VehicleStatus,
                    status.Value,
                    x.Booking.PaperReceivedDate,
                    x.Booking.InvoiceDate,
                    x.Booking.InsuranceDate,
                    x.Booking.AgentDate,
                    x.Booking.RegistrationDate,
                    x.Booking.SubsidyId));
            else
                list = list.Where(x => BookingStageFilter.IsBookingPhase(
                    BookingStageFilter.ResolveEffectiveStage(
                        x.VehicleStatus,
                        x.Booking.PaperReceivedDate,
                        x.Booking.InvoiceDate,
                        x.Booking.InsuranceDate,
                        x.Booking.AgentDate,
                        x.Booking.RegistrationDate,
                        x.Booking.SubsidyId)));
            if (subdealerId.HasValue) list = list.Where(x => x.Booking.SubdealerId == subdealerId.Value);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                list = list.Where(x => x.Chassis.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || x.Subdealer.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || (x.Booking.CustomerName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var items = list.OrderByDescending(x => x.Booking.SubmittedDate).ToList();
            var headers = new[] { "ID", "Chassis", "Subdealer", "Customer", "Mobile", "Status", "Submitted", "Invoice Doc", "Insurance Doc" };
            var rows = items.Select(x =>
            {
                var b = x.Booking;
                var statusLabel = statusMap.TryGetValue(
                    vehicles.TryGetValue(b.VehicleId, out var veh) ? veh.Status : b.BookingStatus, out var sn)
                    ? sn : b.BookingStatus.ToString();
                return (IReadOnlyList<object?>)new List<object?>
                {
                    b.VehicleBookingId, x.Chassis, x.Subdealer, b.CustomerName, b.CustomerMobile, statusLabel, b.SubmittedDate,
                    string.IsNullOrWhiteSpace(b.InvoicePath) ? "No" : "Yes",
                    string.IsNullOrWhiteSpace(b.InsurancePath) ? "No" : "Yes"
                };
            });

            return ExcelExportHelper.ToFileResult(this, $"vehicle_bookings_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Bookings");
        }

        [AuthorizeRole(1, 4)]
        [AuthorizeMenuAny(StaffMenuAccess.VehicleBookings, StaffMenuAccess.BookedToCustomerView)]
        [HttpGet]
        public async Task<IActionResult> SubdealersForFilter(int? dealershipId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var effectiveDealershipId = scope ?? dealershipId;
            var subdealers = await _mediator.Send(new GetSubdealersQuery
            {
                IsActive = true,
                DealershipId = effectiveDealershipId
            });

            return Json(subdealers.Select(s => new { id = s.UserId, name = s.GetFullName() }));
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> Book(int vehicleId)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var vehicle = await LoadVehicleOrNull(vehicleId, userId.Value);
            if (vehicle == null) { TempData["Error"] = "Vehicle not found."; return RedirectToAction("Index", "Vehicles"); }

            var existing = (await _unitOfWork.VehicleBookings.GetAllAsync()).FirstOrDefault(b => b.VehicleId == vehicleId);
            if (existing != null)
            {
                TempData["Info"] = "This vehicle is already booked.";
                return this.RedirectEncrypted(nameof(Manage), new { id = existing.VehicleBookingId });
            }

            if (!vehicle.CanBook)
            {
                TempData["Error"] = vehicle.IsAwaitingDealerApproval
                    ? "This vehicle is awaiting dealer approval before booking."
                    : "This vehicle cannot be booked in its current status.";
                return RedirectToAction("Index", "Vehicles");
            }

            await LoadBookingFormViewBags();
            ViewBag.Vehicle = vehicle;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> Book(int vehicleId, string customerName, bool isCompanyBooking,
            string customerMobile, string alternativeMobile, string customerEmail,
            string eAadhaarPassword, int documentTypeId, int rtoLocationId, bool fancyNumber,
            string paymentMode, int financeNameId, string nomineeName, DateTime nomineeDob, string nomineeRelationship,
            IFormFile eAadhaarFile, IFormFile documentFile, IFormFile? gstCertificateFile,
            IFormFile customerPhoto, IFormFile chassisPhoto, IFormFile customerSign)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var vehicle = await LoadVehicleOrNull(vehicleId, userId.Value);
            if (vehicle == null) { TempData["Error"] = "Vehicle not found."; return RedirectToAction("Index", "Vehicles"); }

            var existingBooking = (await _unitOfWork.VehicleBookings.GetAllAsync()).FirstOrDefault(b => b.VehicleId == vehicleId);
            if (existingBooking != null)
            {
                TempData["Info"] = "This vehicle is already booked.";
                return this.RedirectEncrypted(nameof(Manage), new { id = existingBooking.VehicleBookingId });
            }

            if (!vehicle.CanBook)
            {
                TempData["Error"] = vehicle.IsAwaitingDealerApproval
                    ? "This vehicle is awaiting dealer approval before booking."
                    : "This vehicle cannot be booked in its current status.";
                return RedirectToAction("Index", "Vehicles");
            }

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerMobile)
                || string.IsNullOrWhiteSpace(alternativeMobile) || string.IsNullOrWhiteSpace(customerEmail)
                || string.IsNullOrWhiteSpace(eAadhaarPassword) || string.IsNullOrWhiteSpace(nomineeName)
                || string.IsNullOrWhiteSpace(nomineeRelationship))
            {
                TempData["Error"] = "Please fill all mandatory fields.";
                return RedirectToAction(nameof(Book), new { vehicleId });
            }

            if (isCompanyBooking && (gstCertificateFile == null || gstCertificateFile.Length == 0))
            {
                TempData["Error"] = "GST Certificate is required for company bookings.";
                return RedirectToAction(nameof(Book), new { vehicleId });
            }

            try
            {
                var root = _env.WebRootPath ?? _env.ContentRootPath;
                var booking = new VehicleBooking
                {
                    VehicleId = vehicleId,
                    SubdealerId = userId.Value,
                    BookingStatus = UnifiedVehicleStatus.BookedToCustomer,
                    CustomerName = customerName.Trim(),
                    IsCompanyBooking = isCompanyBooking,
                    CustomerMobile = customerMobile.Trim(),
                    AlternativeMobile = alternativeMobile.Trim(),
                    CustomerEmail = customerEmail.Trim(),
                    EAadhaarPassword = eAadhaarPassword.Trim(),
                    DocumentTypeId = documentTypeId,
                    RtoLocationId = rtoLocationId,
                    FancyNumber = fancyNumber,
                    PaymentMode = paymentMode,
                    FinanceNameId = financeNameId,
                    NomineeName = nomineeName.Trim(),
                    NomineeDob = nomineeDob.Date,
                    NomineeRelationship = nomineeRelationship.Trim(),
                    EAadhaarPath = await BookingFileHelper.SavePdfAsync(eAadhaarFile, root),
                    DocumentPath = await BookingFileHelper.SavePdfAsync(documentFile, root),
                    GstCertificatePath = isCompanyBooking ? await BookingFileHelper.SavePdfAsync(gstCertificateFile!, root) : null,
                    CustomerPhotoPath = await BookingFileHelper.SaveImageAsync(customerPhoto, root),
                    ChassisPhotoPath = await BookingFileHelper.SaveImageAsync(chassisPhoto, root),
                    CustomerSignPath = await BookingFileHelper.SaveImageAsync(customerSign, root),
                    SubmittedDate = DateTime.UtcNow,
                    CreatedBy = userId.Value,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };

                var id = await _unitOfWork.VehicleBookings.AddAsync(booking);

                var vehicleEntity = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId)
                    ?? throw new InvalidOperationException("Vehicle record not found after booking.");

                vehicleEntity.Status = UnifiedVehicleStatus.BookedToCustomer;
                vehicleEntity.ModifiedDate = DateTime.UtcNow;
                if (!await _unitOfWork.Vehicles.UpdateAsync(vehicleEntity))
                    throw new InvalidOperationException("Failed to update vehicle status after booking.");

                TempData["Success"] = "Vehicle booked successfully.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }
            catch (Exception ex)
            {
                var partial = (await _unitOfWork.VehicleBookings.GetAllAsync()).FirstOrDefault(b => b.VehicleId == vehicleId);
                if (partial != null)
                {
                    TempData["Error"] = $"Booking was saved but could not be completed: {ex.Message}";
                    return this.RedirectEncrypted(nameof(Manage), new { id = partial.VehicleBookingId });
                }

                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Book), new { vehicleId });
            }
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null || booking.SubdealerId != userId) { TempData["Error"] = "Booking not found."; return RedirectToAction("Index", "Vehicles"); }
            if (booking.InvoiceDate.HasValue) { TempData["Error"] = "This booking can no longer be edited — the vehicle has been invoiced."; return this.RedirectEncrypted(nameof(Manage), new { id }); }

            var vehicle = await LoadVehicleOrNull(booking.VehicleId, userId.Value);
            if (vehicle == null) { TempData["Error"] = "Vehicle not found."; return RedirectToAction("Index", "Vehicles"); }

            await LoadBookingFormViewBags();
            ViewBag.Vehicle = vehicle;
            ViewBag.Booking = booking;
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> Edit(int id, string customerName, bool isCompanyBooking,
            string customerMobile, string alternativeMobile, string customerEmail,
            string eAadhaarPassword, int documentTypeId, int rtoLocationId, bool fancyNumber,
            string paymentMode, int financeNameId, string nomineeName, DateTime nomineeDob, string nomineeRelationship,
            string? editReason,
            IFormFile? eAadhaarFile, IFormFile? documentFile, IFormFile? gstCertificateFile,
            IFormFile? customerPhoto, IFormFile? chassisPhoto, IFormFile? customerSign)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null || booking.SubdealerId != userId) { TempData["Error"] = "Booking not found."; return RedirectToAction("Index", "Vehicles"); }
            if (booking.InvoiceDate.HasValue) { TempData["Error"] = "This booking can no longer be edited."; return this.RedirectEncrypted(nameof(Manage), new { id }); }

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(customerMobile)
                || string.IsNullOrWhiteSpace(alternativeMobile) || string.IsNullOrWhiteSpace(customerEmail)
                || string.IsNullOrWhiteSpace(eAadhaarPassword) || string.IsNullOrWhiteSpace(nomineeName)
                || string.IsNullOrWhiteSpace(nomineeRelationship))
            {
                TempData["Error"] = "Please fill all mandatory fields.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            if (isCompanyBooking && gstCertificateFile == null && string.IsNullOrWhiteSpace(booking.GstCertificatePath))
            {
                TempData["Error"] = "GST Certificate is required for company bookings.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            try
            {
                var root = _env.WebRootPath ?? _env.ContentRootPath;
                var cmd = new UpdateSubdealerBookingCommand
                {
                    VehicleBookingId = id,
                    SubdealerId = userId.Value,
                    CustomerName = customerName,
                    IsCompanyBooking = isCompanyBooking,
                    CustomerMobile = customerMobile,
                    AlternativeMobile = alternativeMobile,
                    CustomerEmail = customerEmail,
                    EAadhaarPassword = eAadhaarPassword,
                    DocumentTypeId = documentTypeId,
                    RtoLocationId = rtoLocationId,
                    FancyNumber = fancyNumber,
                    PaymentMode = paymentMode,
                    FinanceNameId = financeNameId,
                    NomineeName = nomineeName,
                    NomineeDob = nomineeDob,
                    NomineeRelationship = nomineeRelationship,
                    EditReason = editReason,
                    UpdatedBy = userId.Value,
                    UpdatedByName = SessionHelper.GetFullName(HttpContext.Session) ?? SessionHelper.GetUsername(HttpContext.Session)
                };

                if (eAadhaarFile != null && eAadhaarFile.Length > 0)
                    cmd.EAadhaarPath = await BookingFileHelper.SavePdfAsync(eAadhaarFile, root);
                if (documentFile != null && documentFile.Length > 0)
                    cmd.DocumentPath = await BookingFileHelper.SavePdfAsync(documentFile, root);
                if (gstCertificateFile != null && gstCertificateFile.Length > 0)
                    cmd.GstCertificatePath = await BookingFileHelper.SavePdfAsync(gstCertificateFile, root);
                if (customerPhoto != null && customerPhoto.Length > 0)
                    cmd.CustomerPhotoPath = await BookingFileHelper.SaveImageAsync(customerPhoto, root);
                if (chassisPhoto != null && chassisPhoto.Length > 0)
                    cmd.ChassisPhotoPath = await BookingFileHelper.SaveImageAsync(chassisPhoto, root);
                if (customerSign != null && customerSign.Length > 0)
                    cmd.CustomerSignPath = await BookingFileHelper.SaveImageAsync(customerSign, root);

                var ok = await _mediator.Send(cmd);
                if (!ok) { TempData["Error"] = "Could not update booking."; return RedirectToAction(nameof(Edit), new { id }); }

                TempData["Success"] = "Booking details updated.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Manage(int id)
        {
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null) { TempData["Error"] = "Booking not found."; return RedirectToAction("Index", "Vehicles"); }

            if (!await CanAccessBooking(booking)) return RedirectToAction("AccessDenied", "Account");

            await SyncBookingStatusFromMilestonesAsync(booking);
            await LoadManageViewBags(booking);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Manage(int id, int bookingStatus, string? subsidyId,
            DateTime? paperReceivedDate, DateTime? invoiceDate, DateTime? insuranceDate, DateTime? agentDate,
            DateTime? registrationDate, string? rtoNumber, DateTime? numberPlateReceivedDate,
            IFormFile? invoiceFile, IFormFile? insuranceFile)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null) { TempData["Error"] = "Booking not found."; return RedirectToAction(nameof(Index)); }

            if (bookingStatus == UnifiedVehicleStatus.Delivered)
            {
                TempData["Error"] = "Delivered status can only be set by the subdealer.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId);
            if (vehicle != null && vehicle.Status == UnifiedVehicleStatus.Delivered)
                bookingStatus = UnifiedVehicleStatus.Delivered;

            var previousInvoiceDate = booking.InvoiceDate?.Date;
            var newInvoiceDate = invoiceDate?.Date;
            var invoiceDateChanged = newInvoiceDate.HasValue && newInvoiceDate != previousInvoiceDate;

            try
            {
                var root = _env.WebRootPath ?? _env.ContentRootPath;
                if (invoiceFile != null && invoiceFile.Length > 0)
                    booking.InvoicePath = await BookingFileHelper.SaveInvoiceDocumentAsync(invoiceFile, root);
                if (insuranceFile != null && insuranceFile.Length > 0)
                    booking.InsurancePath = await BookingFileHelper.SaveInsuranceDocumentAsync(insuranceFile, root);

                booking.PaperReceivedDate = paperReceivedDate?.Date;
                booking.InvoiceDate = newInvoiceDate;
                booking.InsuranceDate = insuranceDate?.Date;
                booking.AgentDate = agentDate?.Date;
                booking.RegistrationDate = registrationDate?.Date;
                booking.RtoNumber = rtoNumber?.Trim();
                booking.NumberPlateReceivedDate = numberPlateReceivedDate?.Date;
                if (!string.IsNullOrWhiteSpace(subsidyId))
                {
                    booking.SubsidyId = subsidyId.Trim();
                    booking.SubsidyCustomerNameCaps = booking.CustomerName.Trim().ToUpperInvariant();
                }
                else
                {
                    booking.SubsidyId = null;
                    booking.SubsidyCustomerNameCaps = null;
                }

                var statusError = BookingStageFilter.ValidateBookingStatusSelection(
                    bookingStatus,
                    booking.PaperReceivedDate,
                    booking.InvoiceDate,
                    booking.InsuranceDate,
                    booking.AgentDate,
                    booking.RegistrationDate,
                    booking.SubsidyId);
                if (statusError != null)
                {
                    TempData["Error"] = statusError;
                    return this.RedirectEncrypted(nameof(Manage), new { id });
                }

                if (vehicle != null && vehicle.Status == UnifiedVehicleStatus.Delivered)
                    bookingStatus = UnifiedVehicleStatus.Delivered;

                booking.BookingStatus = bookingStatus;
                if (vehicle != null)
                {
                    vehicle.Status = bookingStatus;
                    vehicle.ModifiedBy = userId;
                    vehicle.ModifiedDate = DateTime.UtcNow;
                    if (!await _unitOfWork.Vehicles.UpdateAsync(vehicle))
                        throw new InvalidOperationException("Failed to update vehicle status.");
                }
                booking.ModifiedBy = userId;
                booking.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.VehicleBookings.UpdateAsync(booking);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message.Contains("CK_VehicleStatus", StringComparison.OrdinalIgnoreCase)
                    ? "Could not save: the database still restricts vehicle status to values 1–4. Run FIX_VEHICLE_STATUS_CHECK.sql on this database."
                    : $"Could not save booking: {ex.Message}";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }

            if (invoiceDateChanged && newInvoiceDate.HasValue)
            {
                try
                {
                    var priceAdjusted = await _priceService.ApplyPriceOnInvoiceAsync(
                        booking.VehicleId, newInvoiceDate.Value, userId ?? 0);
                    TempData["Success"] = priceAdjusted
                        ? "Booking updated. Catalogue price for the invoice date was applied and the dealer account was adjusted."
                        : "Booking updated.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Booking saved but price could not be applied: {ex.Message}";
                }
            }
            else
            {
                TempData["Success"] = (invoiceFile != null && invoiceFile.Length > 0) || (insuranceFile != null && insuranceFile.Length > 0)
                    ? "Booking updated. Document(s) saved."
                    : "Booking updated.";
            }

            return this.RedirectEncrypted(nameof(Manage), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> MarkDelivered(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null || booking.SubdealerId != userId.Value)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Index", "Vehicles");
            }

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId);
            if (vehicle != null && vehicle.Status == UnifiedVehicleStatus.Delivered)
            {
                TempData["Info"] = "Vehicle is already marked as delivered.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }
            if (vehicle == null || vehicle.Status != UnifiedVehicleStatus.SubsidyIdCreated)
            {
                TempData["Error"] = "Vehicle can be marked delivered only after subsidy ID is created.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }

            booking.BookingStatus = UnifiedVehicleStatus.Delivered;
            vehicle.Status = UnifiedVehicleStatus.Delivered;
            booking.ModifiedBy = userId;
            booking.ModifiedDate = DateTime.UtcNow;
            vehicle.ModifiedBy = userId;
            vehicle.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.VehicleBookings.UpdateAsync(booking);
            await _unitOfWork.Vehicles.UpdateAsync(vehicle);

            TempData["Success"] = "Vehicle marked as delivered.";
            return this.RedirectEncrypted(nameof(Manage), new { id });
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> SubsidyDocs(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null || booking.SubdealerId != userId) { TempData["Error"] = "Not found."; return RedirectToAction("Index", "Vehicles"); }
            if (string.IsNullOrWhiteSpace(booking.SubsidyId)) { TempData["Error"] = "Subsidy ID not yet assigned by dealer."; return RedirectToAction("Index", "Vehicles"); }
            if (booking.SubsidyDocsSubmittedDate.HasValue) { TempData["Info"] = "Subsidy documents already submitted."; return this.RedirectEncrypted(nameof(Manage), new { id }); }

            var vehicle = (await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId));
            ViewBag.Vehicle = vehicle;
            ViewBag.CustomerNameCaps = booking.CustomerName.Trim().ToUpperInvariant();
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> SubsidyDocs(int id, IFormFile faceVerification, IFormFile rcImage,
            IFormFile boothPhoto, IFormFile subsidyUndertaking)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null || booking.SubdealerId != userId) { TempData["Error"] = "Not found."; return RedirectToAction("Index", "Vehicles"); }
            if (string.IsNullOrWhiteSpace(booking.SubsidyId)) { TempData["Error"] = "Subsidy ID required first."; return RedirectToAction("Index", "Vehicles"); }

            try
            {
                var root = _env.WebRootPath ?? _env.ContentRootPath;
                booking.SubsidyCustomerNameCaps = booking.CustomerName.Trim().ToUpperInvariant();
                booking.FaceVerificationPath = await BookingFileHelper.SaveImageAsync(faceVerification, root);
                booking.RcImagePath = await BookingFileHelper.SaveImageAsync(rcImage, root);
                booking.BoothPhotoPath = await BookingFileHelper.SaveImageAsync(boothPhoto, root);
                booking.SubsidyUndertakingPath = await BookingFileHelper.SaveImageAsync(subsidyUndertaking, root);
                booking.SubsidyDocsSubmittedDate = DateTime.UtcNow;
                booking.ModifiedBy = userId;
                booking.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.VehicleBookings.UpdateAsync(booking);
                TempData["Success"] = "Subsidy documents submitted.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(SubsidyDocs), new { id });
            }
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Download(string path)
        {
            if (!await CanAccessBookingFileAsync(path)) return Forbid();
            var full = BookingFileHelper.ResolvePath(_env.WebRootPath ?? _env.ContentRootPath, path);
            if (string.IsNullOrEmpty(full)) return NotFound();
            var contentType = BookingFileHelper.GetContentType(full);
            return PhysicalFile(full, contentType, Path.GetFileName(full));
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> ViewFile(string path)
        {
            if (!await CanAccessBookingFileAsync(path)) return Forbid();
            var full = BookingFileHelper.ResolvePath(_env.WebRootPath ?? _env.ContentRootPath, path);
            if (string.IsNullOrEmpty(full)) return NotFound();
            var contentType = BookingFileHelper.GetContentType(full);
            return PhysicalFile(full, contentType);
        }

        private async Task<bool> CanAccessBooking(VehicleBooking booking)
        {
            if (SessionHelper.IsSubdealer(HttpContext.Session))
                return booking.SubdealerId == SessionHelper.GetUserId(HttpContext.Session);
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var scoped = await _mediator.Send(new GetSubdealersQuery { DealershipId = scope });
            return scoped.Any(s => s.UserId == booking.SubdealerId);
        }

        private async Task<bool> CanAccessBookingFileAsync(string? path)
        {
            if (!BookingFileHelper.IsStoredBookingFilePath(path))
                return false;

            var booking = _unitOfWork.VehicleBookings is VehicleBookingRepository repo
                ? await repo.GetByStoredFilePathAsync(path!)
                : (await _unitOfWork.VehicleBookings.GetAllAsync())
                    .FirstOrDefault(b => BookingFileHelper.BookingContainsFilePath(b, path!));

            return booking != null && await CanAccessBooking(booking);
        }

        private async Task<KRSDealerManagement.Application.DTOs.VehicleDto?> LoadVehicleOrNull(int vehicleId, int subdealerId)
        {
            var vehicles = await _mediator.Send(new GetVehiclesQuery { SubdealerId = subdealerId });
            return vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        }

        private async Task LoadBookingFormViewBags()
        {
            ViewBag.DocumentTypes = (await _unitOfWork.DocumentTypes.GetAllAsync()).Where(d => d.IsActive).OrderBy(d => d.TypeName);
            ViewBag.RtoLocations = (await _unitOfWork.RtoLocations.GetAllAsync()).Where(r => r.IsActive).OrderBy(r => r.LocationName);
            ViewBag.FinanceNames = (await _unitOfWork.FinanceNames.GetAllAsync()).Where(f => f.IsActive).OrderBy(f => f.FinanceName);
            ViewBag.PaymentModes = VehiclePaymentModes.All;
        }

        private async Task LoadManageViewBags(VehicleBooking booking)
        {
            ViewBag.IsStaff = SessionHelper.IsStaff(HttpContext.Session);
            var allStatuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle);
            ViewBag.Statuses = SessionHelper.IsStaff(HttpContext.Session)
                ? allStatuses.Where(s => s.StatusValue >= UnifiedVehicleStatus.BookedToCustomer
                    && UnifiedVehicleStatus.IsStaffAssignable(s.StatusValue))
                : allStatuses.Where(s => s.StatusValue >= UnifiedVehicleStatus.BookedToCustomer);
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId);
            var models = (await _unitOfWork.VehicleModels.GetAllAsync()).ToDictionary(m => m.ModelId);
            var colors = (await _unitOfWork.VehicleColors.GetAllAsync()).ToDictionary(c => c.ColorId);
            ViewBag.Chassis = vehicle?.ChassisNumber;
            ViewBag.ModelName = models.GetValueOrDefault(vehicle?.ModelId ?? 0)?.ModelName;
            ViewBag.ColorName = colors.GetValueOrDefault(vehicle?.ColorId ?? 0)?.ColorName;
            ViewBag.PaymentModeLabel = VehiclePaymentModes.GetLabel(booking.PaymentMode);
            ViewBag.CanSubmitSubsidyDocs = SessionHelper.IsSubdealer(HttpContext.Session)
                && !string.IsNullOrWhiteSpace(booking.SubsidyId)
                && !booking.SubsidyDocsSubmittedDate.HasValue;
            ViewBag.CanMarkDelivered = SessionHelper.IsSubdealer(HttpContext.Session)
                && vehicle?.Status == UnifiedVehicleStatus.SubsidyIdCreated;
            ViewBag.CanEditBooking = SessionHelper.IsSubdealer(HttpContext.Session)
                && !booking.InvoiceDate.HasValue;
            ViewBag.VehicleStatus = vehicle == null
                ? booking.BookingStatus
                : BookingStageFilter.ResolveEffectiveStage(
                    vehicle.Status,
                    booking.PaperReceivedDate,
                    booking.InvoiceDate,
                    booking.InsuranceDate,
                    booking.AgentDate,
                    booking.RegistrationDate,
                    booking.SubsidyId);

            var fileRoot = _env.WebRootPath ?? _env.ContentRootPath;
            ViewBag.InvoiceFileAvailable = BookingFileHelper.IsFileAvailable(fileRoot, booking.InvoicePath);
            ViewBag.InsuranceFileAvailable = BookingFileHelper.IsFileAvailable(fileRoot, booking.InsurancePath);
        }

        private async Task SyncBookingStatusFromMilestonesAsync(VehicleBooking booking)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId);
            if (vehicle == null || vehicle.Status == UnifiedVehicleStatus.Delivered)
                return;

            var expected = BookingStageFilter.ResolveFromMilestones(
                booking.PaperReceivedDate,
                booking.InvoiceDate,
                booking.InsuranceDate,
                booking.AgentDate,
                booking.RegistrationDate,
                booking.SubsidyId);

            var effective = Math.Max(Math.Max(vehicle.Status, booking.BookingStatus), expected);
            if (effective == vehicle.Status && effective == booking.BookingStatus)
                return;

            vehicle.Status = effective;
            booking.BookingStatus = effective;
            booking.ModifiedDate = DateTime.UtcNow;
            vehicle.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.Vehicles.UpdateAsync(vehicle);
            await _unitOfWork.VehicleBookings.UpdateAsync(booking);
        }

        private async Task<(HashSet<int> ScopedIds, int? EffectiveDealershipId, bool IsAdmin)> GetBookingScopeAsync(int? dealershipId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var effectiveDealershipId = scope ?? dealershipId;

            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToList();
            var subRole = roles.FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));

            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.IsActive && (subRole == null || a.RoleId == subRole.RoleId));

            if (effectiveDealershipId.HasValue)
                assignments = assignments.Where(a => a.DealershipId == effectiveDealershipId.Value);

            var scopedIds = assignments.Select(a => a.UserId).ToHashSet();
            return (scopedIds, effectiveDealershipId, isAdmin);
        }

        private static string GetBookingPageTitle(int? status, bool viewOnly = false)
        {
            if (viewOnly && status == UnifiedVehicleStatus.BookedToCustomer)
                return "Booked to Customer";

            if (!viewOnly && !status.HasValue)
                return "Vehicle Booking Process";

            return status switch
            {
                UnifiedVehicleStatus.PaperReceived => "Paper Received",
                UnifiedVehicleStatus.Invoiced => "Invoiced",
                UnifiedVehicleStatus.InsuranceCreated => "Insurance Created",
                UnifiedVehicleStatus.RtoRequested => "RTO Requested",
                UnifiedVehicleStatus.Registered => "Registered",
                UnifiedVehicleStatus.Delivered => "Delivered",
                _ => "Vehicle Booking Process"
            };
        }
    }
}
