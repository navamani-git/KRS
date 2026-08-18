using Microsoft.AspNetCore.Mvc;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Shared.Constants;
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
        [AuthorizeMenu(StaffMenuAccess.VehicleBookings)]
        public async Task<IActionResult> Index(int? status, int? subdealerId, string? searchTerm, int? page)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToList();
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var scopedIds = (await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope }))
                .Select(s => s.UserId).ToHashSet();

            var list = bookings
                .Where(b => scopedIds.Contains(b.SubdealerId))
                .Select(b =>
                {
                    vehicles.TryGetValue(b.VehicleId, out var v);
                    users.TryGetValue(b.SubdealerId, out var u);
                    return new { Booking = b, Chassis = v?.ChassisNumber ?? "-", Subdealer = u?.GetFullName() ?? "Unknown", VehicleStatus = v?.Status ?? b.BookingStatus };
                });

            if (status.HasValue) list = list.Where(x =>
            {
                vehicles.TryGetValue(x.Booking.VehicleId, out var v);
                return v != null && v.Status == status.Value;
            });
            if (subdealerId.HasValue) list = list.Where(x => x.Booking.SubdealerId == subdealerId.Value);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                list = list.Where(x => x.Chassis.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || x.Subdealer.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || (x.Booking.CustomerName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var items = list.OrderByDescending(x => x.Booking.SubmittedDate).ToList();
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(items, page);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle);
            ViewBag.Subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSubdealerId = subdealerId;
            ViewBag.SearchTerm = searchTerm;
            return View(pageItems);
        }

        [AuthorizeRole(1, 4)]
        [AuthorizeMenu(StaffMenuAccess.VehicleBookings)]
        public async Task<IActionResult> Export(int? status, int? subdealerId, string? searchTerm)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToList();
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var scopedIds = (await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope }))
                .Select(s => s.UserId).ToHashSet();
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

            if (status.HasValue) list = list.Where(x =>
            {
                vehicles.TryGetValue(x.Booking.VehicleId, out var v);
                return v != null && v.Status == status.Value;
            });
            if (subdealerId.HasValue) list = list.Where(x => x.Booking.SubdealerId == subdealerId.Value);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var t = searchTerm.Trim();
                list = list.Where(x => x.Chassis.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || x.Subdealer.Contains(t, StringComparison.OrdinalIgnoreCase)
                    || (x.Booking.CustomerName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var items = list.OrderByDescending(x => x.Booking.SubmittedDate).ToList();
            var headers = new[] { "ID", "Chassis", "Subdealer", "Customer", "Mobile", "Status", "Submitted" };
            var rows = items.Select(x =>
            {
                var b = x.Booking;
                var statusLabel = statusMap.TryGetValue(
                    vehicles.TryGetValue(b.VehicleId, out var veh) ? veh.Status : b.BookingStatus, out var sn)
                    ? sn : b.BookingStatus.ToString();
                return (IReadOnlyList<object?>)new List<object?>
                {
                    b.VehicleBookingId, x.Chassis, x.Subdealer, b.CustomerName, b.CustomerMobile, statusLabel, b.SubmittedDate
                };
            });

            return ExcelExportHelper.ToFileResult(this, $"vehicle_bookings_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Bookings");
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

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Manage(int id)
        {
            var booking = await _unitOfWork.VehicleBookings.GetByIdAsync(id);
            if (booking == null) { TempData["Error"] = "Booking not found."; return RedirectToAction("Index", "Vehicles"); }

            if (!await CanAccessBooking(booking)) return RedirectToAction("AccessDenied", "Account");

            await LoadManageViewBags(booking);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Manage(int id, int bookingStatus, string? subsidyId,
            DateTime? paperReceivedDate, DateTime? invoiceDate, DateTime? insuranceDate, DateTime? agentDate,
            DateTime? registrationDate, string? rtoNumber, DateTime? numberPlateReceivedDate)
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
                booking.BookingStatus = bookingStatus;
                if (vehicle != null)
                {
                    vehicle.Status = bookingStatus;
                    vehicle.ModifiedBy = userId;
                    vehicle.ModifiedDate = DateTime.UtcNow;
                    if (!await _unitOfWork.Vehicles.UpdateAsync(vehicle))
                        throw new InvalidOperationException("Failed to update vehicle status.");
                }
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
                TempData["Success"] = "Booking updated.";
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
        public IActionResult Download(string path)
        {
            var full = BookingFileHelper.ResolvePath(_env.WebRootPath ?? _env.ContentRootPath, path);
            if (string.IsNullOrEmpty(full)) return NotFound();
            var contentType = BookingFileHelper.GetContentType(full);
            return PhysicalFile(full, contentType, Path.GetFileName(full));
        }

        [AuthorizeRole(1, 2, 4)]
        public IActionResult ViewFile(string path)
        {
            var full = BookingFileHelper.ResolvePath(_env.WebRootPath ?? _env.ContentRootPath, path);
            if (string.IsNullOrEmpty(full)) return NotFound();
            var contentType = BookingFileHelper.GetContentType(full);
            return PhysicalFile(full, contentType);
        }

        private async Task<KRSDealerManagement.Application.DTOs.VehicleDto?> LoadVehicleOrNull(int vehicleId, int subdealerId)
        {
            var vehicles = await _mediator.Send(new GetVehiclesQuery { SubdealerId = subdealerId });
            return vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
        }

        private async Task<bool> CanAccessBooking(VehicleBooking booking)
        {
            if (SessionHelper.IsSubdealer(HttpContext.Session))
                return booking.SubdealerId == SessionHelper.GetUserId(HttpContext.Session);
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var scoped = await _mediator.Send(new GetSubdealersQuery { DealershipId = scope });
            return scoped.Any(s => s.UserId == booking.SubdealerId);
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
            ViewBag.VehicleStatus = vehicle?.Status ?? booking.BookingStatus;
        }
    }
}
