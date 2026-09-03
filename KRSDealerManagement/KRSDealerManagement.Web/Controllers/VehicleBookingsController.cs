using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Helpers;
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

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MyBookedToCustomer(string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(
                UnifiedVehicleStatus.BookedToCustomer,
                SessionHelper.GetUserId(HttpContext.Session),
                null,
                searchTerm,
                page,
                pageSize,
                viewOnly: true,
                bookedToCustomerView: true,
                subdealerView: true);

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MyPaperReceived(string? searchTerm, int? page, int? pageSize)
            => ListMyBookingsAsync(UnifiedVehicleStatus.PaperReceived, searchTerm, page, pageSize);

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MyInvoiced(string? searchTerm, int? page, int? pageSize)
            => ListMyBookingsAsync(UnifiedVehicleStatus.Invoiced, searchTerm, page, pageSize);

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MyInsuranceCreated(string? searchTerm, int? page, int? pageSize)
            => ListMyBookingsAsync(UnifiedVehicleStatus.InsuranceCreated, searchTerm, page, pageSize);

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MyRtoRequested(string? searchTerm, int? page, int? pageSize)
            => ListMyBookingsAsync(UnifiedVehicleStatus.RtoRequested, searchTerm, page, pageSize);

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MySubsidyIdPending(string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(
                null,
                null,
                null,
                searchTerm,
                page,
                pageSize,
                viewOnly: true,
                subsidyIdPendingOnly: true,
                subdealerView: true);

        [AuthorizeRole(1, 4)]
        [AuthorizeMenuAny(StaffMenuAccess.VehicleBookings, StaffMenuAccess.BookedToCustomerView)]
        public Task<IActionResult> SubsidyIdPending(int? subdealerId, int? dealershipId, string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(
                null,
                subdealerId,
                dealershipId,
                searchTerm,
                page,
                pageSize,
                viewOnly: true,
                subsidyIdPendingOnly: true);

        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.VehicleBookings)]
        public Task<IActionResult> SubsidyDocsPending(int? subdealerId, int? dealershipId, string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(
                null,
                subdealerId,
                dealershipId,
                searchTerm,
                page,
                pageSize,
                viewOnly: true,
                subsidyDocsPendingOnly: true);

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MySubsidyDocsPending(string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(
                null,
                null,
                null,
                searchTerm,
                page,
                pageSize,
                viewOnly: true,
                subsidyDocsPendingOnly: true,
                subdealerView: true);

        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.VehicleBookings)]
        public Task<IActionResult> RegisteredAwaitingPlate(int? subdealerId, int? dealershipId, string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(
                null,
                subdealerId,
                dealershipId,
                searchTerm,
                page,
                pageSize,
                viewOnly: true,
                registeredAwaitingPlateOnly: true);

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public Task<IActionResult> MyRegisteredAwaitingPlate(string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(
                null,
                null,
                null,
                searchTerm,
                page,
                pageSize,
                viewOnly: true,
                registeredAwaitingPlateOnly: true,
                subdealerView: true);

        private Task<IActionResult> ListMyBookingsAsync(int status, string? searchTerm, int? page, int? pageSize)
            => ListBookingsAsync(status, null, null, searchTerm, page, pageSize, viewOnly: true, subdealerView: true);

        private async Task<IActionResult> ListBookingsAsync(
            int? status,
            int? subdealerId,
            int? dealershipId,
            string? searchTerm,
            int? page,
            int? pageSize,
            bool viewOnly,
            bool bookingPhaseOnly = false,
            bool bookedToCustomerView = false,
            bool subsidyIdPendingOnly = false,
            bool subsidyDocsPendingOnly = false,
            bool registeredAwaitingPlateOnly = false,
            bool subdealerView = false)
        {
            var (scopedIds, effectiveDealershipId, isAdmin) = await GetBookingScopeAsync(dealershipId);
            if (subdealerView)
            {
                var currentUserId = SessionHelper.GetUserId(HttpContext.Session);
                if (currentUserId.HasValue)
                    subdealerId = currentUserId;
            }
            else if (subdealerId.HasValue && !scopedIds.Contains(subdealerId.Value))
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
                    VehicleStatus = vehicleStatus,
                    RegistrationNumber = v?.RegistrationNumber
                });
            }

            var list = rows.AsEnumerable();
            if (subsidyIdPendingOnly)
            {
                list = list.Where(x => BookingStageFilter.IsSubsidyIdPending(
                    x.Booking.InvoiceDate,
                    x.Booking.InsuranceDate,
                    x.Booking.SubsidyId,
                    x.VehicleStatus));
            }
            else if (subsidyDocsPendingOnly)
            {
                list = list.Where(x => BookingStageFilter.IsSubsidyDocsPending(
                    x.Booking.SubsidyId,
                    x.Booking.FaceVerificationPath,
                    x.Booking.RcImagePath,
                    x.Booking.BoothPhotoPath,
                    x.Booking.SubsidyUndertakingPath,
                    x.VehicleStatus));
            }
            else if (registeredAwaitingPlateOnly)
            {
                list = list.Where(x => BookingStageFilter.IsRegisteredAwaitingNumberPlate(
                    x.VehicleStatus,
                    x.Booking.PaperReceivedDate,
                    x.Booking.InvoiceDate,
                    x.Booking.InsuranceDate,
                    x.Booking.AgentDate,
                    x.Booking.RegistrationDate,
                    x.Booking.SubsidyId,
                    x.Booking.NumberPlateReceivedDate,
                    x.Booking.NumberPlateReceivedBy));
            }
            else if (status.HasValue)
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
            ViewBag.ShowDealershipFilter = isAdmin && !subdealerView;
            ViewBag.ShowSubdealerFilter = !subdealerView;
            ViewBag.IsSubdealerView = subdealerView;
            ViewBag.LockStatusFilter = (status.HasValue || subsidyIdPendingOnly || subsidyDocsPendingOnly || registeredAwaitingPlateOnly) && viewOnly;
            ViewBag.ShowManage = !viewOnly || subdealerView;
            ViewBag.PageTitle = subsidyIdPendingOnly
                ? "Subsidy ID Pending"
                : subsidyDocsPendingOnly
                    ? "Subsidy Docs Pending"
                    : registeredAwaitingPlateOnly
                        ? "Registered"
                        : GetBookingPageTitle(status, viewOnly, bookedToCustomerView);
            ViewBag.BookingPhaseOnly = bookingPhaseOnly;
            ViewBag.BookedToCustomerView = bookedToCustomerView;
            ViewBag.SubsidyIdPendingOnly = subsidyIdPendingOnly;
            ViewBag.SubsidyDocsPendingOnly = subsidyDocsPendingOnly;
            ViewBag.RegisteredAwaitingPlateOnly = registeredAwaitingPlateOnly;
            var stageHelp = BookingGridStageInfo.GetHelp(
                status,
                bookingPhaseOnly,
                bookedToCustomerView,
                subsidyIdPendingOnly,
                subsidyDocsPendingOnly,
                registeredAwaitingPlateOnly);
            ViewBag.GridStageShowing = stageHelp.Showing;
            ViewBag.GridStageRemovedWhen = stageHelp.RemovedWhen;
            ViewBag.GridStagePurpose = stageHelp.ScreenPurpose;
            ViewBag.GridStageHint = $"Showing: {stageHelp.Showing} · Removed when: {stageHelp.RemovedWhen}";
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
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
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
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
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

            var validationError = BookingFormValidationHelper.ValidateCreateBooking(
                customerName, customerMobile, alternativeMobile, customerEmail, eAadhaarPassword,
                nomineeRelationship, isCompanyBooking, eAadhaarFile, documentFile, gstCertificateFile,
                customerPhoto, chassisPhoto, customerSign);
            if (validationError != null)
            {
                TempData["Error"] = validationError;
                return RedirectToAction(nameof(Book), new { vehicleId });
            }

            customerName = BookingFormValidationHelper.NormalizeCustomerName(customerName);
            customerEmail = BookingFormValidationHelper.NormalizeEmail(customerEmail);
            customerMobile = customerMobile.Trim();
            alternativeMobile = alternativeMobile.Trim();
            eAadhaarPassword = eAadhaarPassword.Trim();
            nomineeRelationship = nomineeRelationship.Trim();

            try
            {
                var root = _env;
                var booking = new VehicleBooking
                {
                    VehicleId = vehicleId,
                    SubdealerId = userId.Value,
                    BookingStatus = UnifiedVehicleStatus.BookedToCustomer,
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
                    NomineeName = nomineeName.Trim(),
                    NomineeDob = nomineeDob.Date,
                    NomineeRelationship = nomineeRelationship,
                    EAadhaarPath = await BookingFileHelper.SaveEAadhaarPdfAsync(eAadhaarFile, root),
                    DocumentPath = await BookingFileHelper.SaveIdentityDocumentPdfAsync(documentFile, root),
                    GstCertificatePath = isCompanyBooking ? await BookingFileHelper.SaveGstCertificatePdfAsync(gstCertificateFile!, root) : null,
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
                vehicleEntity.DeliveryDate = null;
                vehicleEntity.ModifiedDate = DateTime.UtcNow;
                if (!await _unitOfWork.Vehicles.UpdateAsync(vehicleEntity))
                    throw new InvalidOperationException("Failed to update vehicle status after booking.");

                await VehicleHistoryHelper.LogSubdealerEventAsync(
                    _unitOfWork, vehicleId, "BookedToCustomer", userId,
                    $"Customer {customerName.Trim()} ({customerMobile.Trim()}).");

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

        [AuthorizeRole(1, 2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null) { TempData["Error"] = "Booking not found."; return RedirectToAction("Index", "Vehicles"); }

            if (isAdmin)
            {
                if (!await CanAccessBooking(booking))
                    return RedirectToAction("AccessDenied", "Account");
            }
            else if (booking.SubdealerId != userId)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Index", "Vehicles");
            }
            else if (booking.InvoiceDate.HasValue)
            {
                TempData["Error"] = "This booking can no longer be edited — the vehicle has been invoiced.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }

            var vehicle = await LoadBookingVehicleAsync(booking, userId.Value);
            if (vehicle == null) { TempData["Error"] = "Vehicle not found."; return RedirectToAction("Index", "Vehicles"); }

            await LoadBookingFormViewBags(booking.RtoLocationId);
            ViewBag.Vehicle = vehicle;
            ViewBag.Booking = booking;
            ViewBag.HasGstCertificate = !string.IsNullOrWhiteSpace(booking.GstCertificatePath);
            ViewBag.IsAdminEdit = isAdmin;
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
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

            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null) { TempData["Error"] = "Booking not found."; return RedirectToAction("Index", "Vehicles"); }

            if (isAdmin)
            {
                if (!await CanAccessBooking(booking))
                    return RedirectToAction("AccessDenied", "Account");
            }
            else if (booking.SubdealerId != userId)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Index", "Vehicles");
            }
            else if (booking.InvoiceDate.HasValue)
            {
                TempData["Error"] = "This booking can no longer be edited.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }

            var validationError = BookingFormValidationHelper.ValidateEditBooking(
                customerName, customerMobile, alternativeMobile, customerEmail, eAadhaarPassword,
                nomineeRelationship, isCompanyBooking, !string.IsNullOrWhiteSpace(booking.GstCertificatePath),
                eAadhaarFile, documentFile, gstCertificateFile, customerPhoto, chassisPhoto, customerSign);
            if (validationError != null)
            {
                TempData["Error"] = validationError;
                return RedirectToAction(nameof(Edit), new { id });
            }

            customerName = BookingFormValidationHelper.NormalizeCustomerName(customerName);
            customerEmail = BookingFormValidationHelper.NormalizeEmail(customerEmail);
            customerMobile = customerMobile.Trim();
            alternativeMobile = alternativeMobile.Trim();
            eAadhaarPassword = eAadhaarPassword.Trim();
            nomineeRelationship = nomineeRelationship.Trim();

            try
            {
                var root = _env;
                var cmd = new UpdateSubdealerBookingCommand
                {
                    VehicleBookingId = id,
                    SubdealerId = booking.SubdealerId,
                    AllowAdminOverride = isAdmin,
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
                    NomineeName = nomineeName.Trim(),
                    NomineeDob = nomineeDob,
                    NomineeRelationship = nomineeRelationship,
                    EditReason = editReason,
                    UpdatedBy = userId.Value,
                    UpdatedByName = SessionHelper.GetFullName(HttpContext.Session) ?? SessionHelper.GetUsername(HttpContext.Session)
                };

                if (eAadhaarFile != null && eAadhaarFile.Length > 0)
                    cmd.EAadhaarPath = await BookingFileHelper.SaveEAadhaarPdfAsync(eAadhaarFile, root);
                if (documentFile != null && documentFile.Length > 0)
                    cmd.DocumentPath = await BookingFileHelper.SaveIdentityDocumentPdfAsync(documentFile, root);
                if (gstCertificateFile != null && gstCertificateFile.Length > 0)
                    cmd.GstCertificatePath = await BookingFileHelper.SaveGstCertificatePdfAsync(gstCertificateFile, root);
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
            DateTime? registrationDate, string? rtoNumber,
            IFormFile? invoiceFile, IFormFile? insuranceFile, bool confirmPriceAdjustment = false)
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

            var previousInvoiceDate = booking.InvoiceDate;
            var newInvoiceDate = invoiceDate;
            var invoiceDateChanged = newInvoiceDate.HasValue && newInvoiceDate != previousInvoiceDate;
            var previousVehicleStatus = vehicle?.Status ?? booking.BookingStatus;
            var hadSubsidyId = !string.IsNullOrWhiteSpace(booking.SubsidyId);
            var beforePaper = booking.PaperReceivedDate;
            var beforeInsurance = booking.InsuranceDate;
            var beforeAgent = booking.AgentDate;
            var beforeRegistration = booking.RegistrationDate;
            var beforeRto = booking.RtoNumber;
            var beforeSubsidyId = booking.SubsidyId;
            var hadInvoiceFile = !string.IsNullOrWhiteSpace(booking.InvoicePath);
            var hadInsuranceFile = !string.IsNullOrWhiteSpace(booking.InsurancePath);

            try
            {
                var root = _env;
                var uploadedInvoice = invoiceFile != null && invoiceFile.Length > 0;
                var uploadedInsurance = insuranceFile != null && insuranceFile.Length > 0;
                if (uploadedInvoice)
                    booking.InvoicePath = await BookingFileHelper.SaveInvoiceDocumentAsync(invoiceFile, root);
                if (uploadedInsurance)
                    booking.InsurancePath = await BookingFileHelper.SaveInsuranceDocumentAsync(insuranceFile, root);

                booking.PaperReceivedDate = paperReceivedDate;
                booking.InvoiceDate = newInvoiceDate;
                booking.InsuranceDate = insuranceDate;
                booking.AgentDate = agentDate;
                booking.RegistrationDate = registrationDate;
                booking.RtoNumber = rtoNumber?.Trim();
                if (!string.IsNullOrWhiteSpace(booking.RtoNumber) && vehicle != null)
                    vehicle.RegistrationNumber = booking.RtoNumber;
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

                if (vehicle != null && vehicle.Status != previousVehicleStatus)
                {
                    await VehicleHistoryHelper.LogStatusChangeAsync(
                        _unitOfWork, vehicle.VehicleId, vehicle.Status, userId);
                }

                if (!hadSubsidyId && !string.IsNullOrWhiteSpace(booking.SubsidyId))
                {
                    await VehicleHistoryHelper.LogSubdealerEventAsync(
                        _unitOfWork, booking.VehicleId, "SubsidyIdCreated", userId,
                        $"Subsidy ID {booking.SubsidyId.Trim()}");
                }

                var historyNotes = new List<string>();
                void Track(string? note) { if (!string.IsNullOrWhiteSpace(note)) historyNotes.Add(note); }
                Track(VehicleBookingHistoryHelper.DescribeDateTimeChange("Paper received", beforePaper, booking.PaperReceivedDate));
                Track(VehicleBookingHistoryHelper.DescribeDateTimeChange("Invoice date", previousInvoiceDate, booking.InvoiceDate));
                Track(VehicleBookingHistoryHelper.DescribeDateTimeChange("Insurance date", beforeInsurance, booking.InsuranceDate));
                Track(VehicleBookingHistoryHelper.DescribeDateTimeChange("Agent date", beforeAgent, booking.AgentDate));
                Track(VehicleBookingHistoryHelper.DescribeDateTimeChange("Registration date", beforeRegistration, booking.RegistrationDate));
                Track(VehicleBookingHistoryHelper.DescribeTextChange("RTO number", beforeRto, booking.RtoNumber));
                Track(VehicleBookingHistoryHelper.DescribeTextChange("Subsidy ID", beforeSubsidyId, booking.SubsidyId));
                if (uploadedInvoice)
                    Track(hadInvoiceFile ? "Invoice document replaced" : "Invoice document uploaded");
                if (uploadedInsurance)
                    Track(hadInsuranceFile ? "Insurance document replaced" : "Insurance document uploaded");

                await VehicleBookingHistoryHelper.LogChangesAsync(
                    _unitOfWork, booking.VehicleId, userId, historyNotes);
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
                var preview = await _priceService.GetInvoicePriceChangePreviewAsync(booking.VehicleId, newInvoiceDate.Value.Date);
                if (!preview.HasCatalogPrice)
                {
                    TempData["Error"] = preview.ErrorMessage ?? "No catalogue price for the invoice date.";
                    return this.RedirectEncrypted(nameof(Manage), new { id });
                }

                if (preview.WouldChange && !confirmPriceAdjustment)
                {
                    TempData["Error"] = $"Catalogue price differs (current ₹{preview.CurrentVehiclePrice:N2}, invoice date price ₹{preview.CatalogPrice:N2}). Confirm the price adjustment and save again.";
                    return this.RedirectEncrypted(nameof(Manage), new { id });
                }

                try
                {
                    var priceAdjusted = preview.WouldChange && confirmPriceAdjustment
                        ? await _priceService.ApplyPriceOnInvoiceAsync(booking.VehicleId, newInvoiceDate.Value.Date, userId ?? 0)
                        : false;
                    TempData["Success"] = priceAdjusted
                        ? $"Booking updated. Price changed from ₹{preview.CurrentVehiclePrice:N2} to ₹{preview.CatalogPrice:N2}; dealer account adjusted."
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

        [HttpGet]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> InvoicePricePreview(int id, DateTime invoiceDate)
        {
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null)
                return Json(new { success = false, message = "Booking not found." });

            var preview = await _priceService.GetInvoicePriceChangePreviewAsync(booking.VehicleId, invoiceDate);
            if (!preview.HasCatalogPrice)
                return Json(new { success = false, message = preview.ErrorMessage ?? "No catalogue price for this invoice date." });

            return Json(new
            {
                success = true,
                wouldChange = preview.WouldChange,
                currentPrice = preview.CurrentVehiclePrice,
                catalogPrice = preview.CatalogPrice,
                delta = preview.Delta
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public async Task<IActionResult> MarkDelivered(int id, DateTime deliveryDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null || booking.SubdealerId != userId.Value)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Index", "Vehicles");
            }

            try
            {
                await _mediator.Send(new MarkVehicleDeliveredCommand
                {
                    VehicleId = booking.VehicleId,
                    VehicleBookingId = booking.VehicleBookingId,
                    DeliveryDate = deliveryDate,
                    MarkedBy = userId.Value
                });
                TempData["Success"] = "Vehicle marked as delivered.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }

            return this.RedirectEncrypted(nameof(Manage), new { id });
        }

        [AuthorizeRole(1, 2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public async Task<IActionResult> SubsidyDocs(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null) { TempData["Error"] = "Not found."; return RedirectToAction("Index", "Vehicles"); }

            if (isAdmin)
            {
                if (!await CanAccessBooking(booking))
                    return RedirectToAction("AccessDenied", "Account");
            }
            else if (booking.SubdealerId != userId)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction("Index", "Vehicles");
            }

            if (string.IsNullOrWhiteSpace(booking.SubsidyId)) { TempData["Error"] = "Subsidy ID not yet assigned by dealer."; return RedirectToAction("Index", "Vehicles"); }
            if (!isAdmin && BookingStageFilter.HasAllSubsidyDocs(
                booking.FaceVerificationPath,
                booking.RcImagePath,
                booking.BoothPhotoPath,
                booking.SubsidyUndertakingPath))
            {
                TempData["Info"] = "All subsidy documents are already uploaded.";
                return this.RedirectEncrypted(nameof(Manage), new { id });
            }

            var vehicle = (await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId));
            ViewBag.Vehicle = vehicle;
            ViewBag.CustomerNameCaps = booking.CustomerName.Trim().ToUpperInvariant();
            ViewBag.IsAdminEdit = isAdmin;
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public async Task<IActionResult> SubsidyDocs(int id, IFormFile? faceVerification, IFormFile? rcImage,
            IFormFile? boothPhoto, IFormFile? subsidyUndertaking)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null) { TempData["Error"] = "Not found."; return RedirectToAction("Index", "Vehicles"); }

            if (isAdmin)
            {
                if (!await CanAccessBooking(booking))
                    return RedirectToAction("AccessDenied", "Account");
            }
            else if (booking.SubdealerId != userId)
            {
                TempData["Error"] = "Not found.";
                return RedirectToAction("Index", "Vehicles");
            }

            if (string.IsNullOrWhiteSpace(booking.SubsidyId)) { TempData["Error"] = "Subsidy ID required first."; return RedirectToAction("Index", "Vehicles"); }

            try
            {
                if ((faceVerification == null || faceVerification.Length == 0)
                    && (rcImage == null || rcImage.Length == 0)
                    && (boothPhoto == null || boothPhoto.Length == 0)
                    && (subsidyUndertaking == null || subsidyUndertaking.Length == 0))
                {
                    TempData["Error"] = "Select at least one document to upload.";
                    return RedirectToAction(nameof(SubsidyDocs), new { id });
                }

                var root = _env;
                booking.SubsidyCustomerNameCaps = booking.CustomerName.Trim().ToUpperInvariant();
                var replacedDocs = new List<string>();
                if (faceVerification != null && faceVerification.Length > 0)
                {
                    booking.FaceVerificationPath = await BookingFileHelper.SaveImageAsync(faceVerification, root);
                    replacedDocs.Add("Face verification");
                }
                if (rcImage != null && rcImage.Length > 0)
                {
                    booking.RcImagePath = await BookingFileHelper.SaveImageAsync(rcImage, root);
                    replacedDocs.Add("RC image");
                }
                if (boothPhoto != null && boothPhoto.Length > 0)
                {
                    booking.BoothPhotoPath = await BookingFileHelper.SaveImageAsync(boothPhoto, root);
                    replacedDocs.Add("Booth photo");
                }
                if (subsidyUndertaking != null && subsidyUndertaking.Length > 0)
                {
                    booking.SubsidyUndertakingPath = await BookingFileHelper.SaveImageAsync(subsidyUndertaking, root);
                    replacedDocs.Add("Subsidy undertaking");
                }

                var allComplete = BookingStageFilter.HasAllSubsidyDocs(
                    booking.FaceVerificationPath,
                    booking.RcImagePath,
                    booking.BoothPhotoPath,
                    booking.SubsidyUndertakingPath);
                var hadSubmittedDate = booking.SubsidyDocsSubmittedDate.HasValue;

                if (replacedDocs.Count > 0)
                {
                    var firstComplete = allComplete && !hadSubmittedDate;
                    if (allComplete)
                        booking.SubsidyDocsSubmittedDate = DateTime.UtcNow;

                    var action = firstComplete ? "SubsidyDocsSubmitted" : "SubsidyDocsUpdated";
                    var remarks = firstComplete
                        ? "All subsidy documents submitted"
                        : $"Updated: {string.Join(", ", replacedDocs)}";

                    await VehicleHistoryHelper.LogSubdealerEventAsync(
                        _unitOfWork, booking.VehicleId, action, userId, remarks);
                }

                booking.ModifiedBy = userId;
                booking.ModifiedDate = DateTime.UtcNow;
                await _unitOfWork.VehicleBookings.UpdateAsync(booking);
                TempData["Success"] = allComplete && !hadSubmittedDate
                    ? "All subsidy documents submitted."
                    : replacedDocs.Count > 0
                        ? "Subsidy document(s) updated."
                        : "Document saved. Upload the remaining documents when ready.";
                return this.RedirectEncrypted(nameof(SubsidyDocs), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(SubsidyDocs), new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesBookingStages)]
        public async Task<IActionResult> NumberPlateReceived(int id, DateTime? numberPlateReceivedDate, string? numberPlateReceivedBy)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null || booking.SubdealerId != userId)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Index", "Vehicles");
            }

            if (!numberPlateReceivedDate.HasValue || string.IsNullOrWhiteSpace(numberPlateReceivedBy))
            {
                TempData["Error"] = "Number plate received date and received-by name are required.";
                return RedirectToAction(nameof(MyRegisteredAwaitingPlate));
            }

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId);
            if (!BookingStageFilter.IsRegisteredAwaitingNumberPlate(
                vehicle?.Status ?? booking.BookingStatus,
                booking.PaperReceivedDate,
                booking.InvoiceDate,
                booking.InsuranceDate,
                booking.AgentDate,
                booking.RegistrationDate,
                booking.SubsidyId,
                booking.NumberPlateReceivedDate,
                booking.NumberPlateReceivedBy))
            {
                TempData["Error"] = "This vehicle is not awaiting number plate receipt.";
                return RedirectToAction(nameof(MyRegisteredAwaitingPlate));
            }

            booking.NumberPlateReceivedDate = numberPlateReceivedDate.Value;
            booking.NumberPlateReceivedBy = numberPlateReceivedBy.Trim();
            booking.ModifiedBy = userId;
            booking.ModifiedDate = DateTime.UtcNow;
            await _unitOfWork.VehicleBookings.UpdateAsync(booking);
            await VehicleHistoryHelper.LogSubdealerEventAsync(
                _unitOfWork,
                booking.VehicleId,
                "NumberPlateReceived",
                userId,
                $"{numberPlateReceivedDate.Value:yyyy-MM-dd HH:mm} — received by {numberPlateReceivedBy.Trim()}");
            TempData["Success"] = "Number plate received details saved.";
            return RedirectToAction(nameof(MyRegisteredAwaitingPlate));
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Download(string path)
        {
            if (!await CanAccessBookingFileAsync(path))
                return FileDownloadHelper.RedirectWithMessage(this, FileDownloadHelper.AccessDeniedMessage, "VehicleBookings");

            var full = BookingFileHelper.ResolvePath(_env, path);
            if (string.IsNullOrEmpty(full))
                return FileDownloadHelper.RedirectMissingFile(this, "VehicleBookings");

            var contentType = BookingFileHelper.GetContentType(full);
            return PhysicalFile(full, contentType, Path.GetFileName(full));
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> ViewFile(string path)
        {
            if (!await CanAccessBookingFileAsync(path))
                return FileDownloadHelper.RedirectWithMessage(this, FileDownloadHelper.AccessDeniedMessage, "VehicleBookings");

            var full = BookingFileHelper.ResolvePath(_env, path);
            if (string.IsNullOrEmpty(full))
                return FileDownloadHelper.RedirectMissingFile(this, "VehicleBookings");

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

        private async Task<KRSDealerManagement.Application.DTOs.VehicleDto?> LoadBookingVehicleAsync(VehicleBooking booking, int userId)
        {
            if (SessionHelper.IsSystemAdmin(HttpContext.Session))
            {
                var vehicles = await _mediator.Send(new GetVehiclesQuery { SubdealerId = booking.SubdealerId });
                return vehicles.FirstOrDefault(v => v.VehicleId == booking.VehicleId);
            }

            return await LoadVehicleOrNull(booking.VehicleId, userId);
        }

        private async Task LoadBookingFormViewBags(int? selectedRtoLocationId = null)
        {
            ViewBag.DocumentTypes = (await _unitOfWork.DocumentTypes.GetAllAsync()).Where(d => d.IsActive).OrderBy(d => d.TypeName);
            ViewBag.RtoDistricts = (await _unitOfWork.RtoDistricts.GetAllAsync()).Where(d => d.IsActive).OrderBy(d => d.DistrictName);
            ViewBag.RtoLocations = (await _unitOfWork.RtoLocations.GetAllAsync()).Where(r => r.IsActive).OrderBy(r => r.LocationName);
            if (selectedRtoLocationId is int locationId)
            {
                var location = (await _unitOfWork.RtoLocations.GetAllAsync()).FirstOrDefault(r => r.RtoLocationId == locationId);
                ViewBag.SelectedRtoDistrictId = location?.RtoDistrictId;
                ViewBag.SelectedRtoLocationId = locationId;
            }
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
                && BookingStageFilter.IsSubsidyDocsPending(
                    booking.SubsidyId,
                    booking.FaceVerificationPath,
                    booking.RcImagePath,
                    booking.BoothPhotoPath,
                    booking.SubsidyUndertakingPath,
                    vehicle?.Status ?? booking.BookingStatus);
            ViewBag.CanMarkDelivered = SessionHelper.IsSubdealer(HttpContext.Session)
                && vehicle != null
                && vehicle.Status != UnifiedVehicleStatus.Delivered;
            ViewBag.MinDeliveryDate = vehicle?.PurchaseOrderId is int poId
                ? (await _unitOfWork.PurchaseOrders.GetByIdAsync(poId))?.CreatedDate.ToString("yyyy-MM-ddTHH:mm")
                : vehicle?.CreatedDate.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.MaxDeliveryDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DeliveryDate = vehicle?.DeliveryDate.HasValue == true
                ? FormDateTimeHelper.FormatDisplay(vehicle.DeliveryDate)
                : null;
            ViewBag.CanEditBooking = SessionHelper.IsSystemAdmin(HttpContext.Session)
                || (SessionHelper.IsSubdealer(HttpContext.Session) && !booking.InvoiceDate.HasValue);
            ViewBag.CanEditSubsidyDocs = SessionHelper.IsSystemAdmin(HttpContext.Session)
                || (SessionHelper.IsSubdealer(HttpContext.Session)
                    && !string.IsNullOrWhiteSpace(booking.SubsidyId));
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

            var fileRoot = _env;
            ViewBag.InvoiceFileAvailable = BookingFileHelper.IsFileAvailable(fileRoot, booking.InvoicePath);
            ViewBag.InsuranceFileAvailable = BookingFileHelper.IsFileAvailable(fileRoot, booking.InsurancePath);
        }

        private async Task SyncBookingStatusFromMilestonesAsync(VehicleBooking booking)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(booking.VehicleId);
            if (vehicle == null || vehicle.Status == UnifiedVehicleStatus.Delivered)
                return;

            var previousStatus = vehicle.Status;
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

            if (effective != previousStatus)
            {
                var userId = SessionHelper.GetUserId(HttpContext.Session);
                await VehicleHistoryHelper.LogStatusChangeAsync(
                    _unitOfWork, vehicle.VehicleId, effective, userId);
            }
        }

        private async Task<(HashSet<int> ScopedIds, int? EffectiveDealershipId, bool IsAdmin)> GetBookingScopeAsync(int? dealershipId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var isAdmin = SessionHelper.IsSystemAdmin(HttpContext.Session);
            var effectiveDealershipId = scope ?? dealershipId;

            if (SessionHelper.IsSubdealer(HttpContext.Session))
            {
                var userId = SessionHelper.GetUserId(HttpContext.Session);
                return userId.HasValue
                    ? (new HashSet<int> { userId.Value }, SessionHelper.GetDealershipId(HttpContext.Session), false)
                    : (new HashSet<int>(), effectiveDealershipId, false);
            }

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

        private static string GetBookingPageTitle(int? status, bool viewOnly = false, bool bookedToCustomerView = false)
        {
            if (viewOnly && (bookedToCustomerView || status == UnifiedVehicleStatus.BookedToCustomer))
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
