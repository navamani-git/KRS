using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Web.Models;
using KRSDealerManagement.Web.Services;

namespace KRSDealerManagement.Web.Controllers
{
    public class VehiclesController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;
        private readonly IQueryStringCrypto _queryCrypto;

        public VehiclesController(
            IMediator mediator,
            IUnitOfWork unitOfWork,
            IStatusLookupService statuses,
            IQueryStringCrypto queryCrypto)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _statuses = statuses;
            _queryCrypto = queryCrypto;
        }

        private async Task<GetVehiclesQuery> BuildQueryAsync(
            int? subdealerId,
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            string? dealershipLocation = null)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var query = new GetVehiclesQuery
            {
                FromDate = from,
                ToDate = to,
                SearchTerm = searchTerm,
                DealershipLocation = dealershipLocation
            };

            if (SessionHelper.IsSubdealer(HttpContext.Session))
                query.SubdealerId = userId;
            else
            {
                query.DealershipId = SessionHelper.GetDealershipScope(HttpContext.Session);
                if (subdealerId.HasValue) query.SubdealerId = subdealerId;
            }
            return query;
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Index(
            int? subdealerId,
            string? searchTerm,
            string? dealershipLocation,
            DateTime? fromDate,
            DateTime? toDate,
            int? page,
            int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (SessionHelper.IsSubdealer(HttpContext.Session))
            {
                if (!SessionHelper.HasMenuAccess(HttpContext.Session, MenuKeys.VehiclesView))
                    return RedirectToAction("AccessDenied", "Account");
            }
            else if (!SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Vehicles))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var isSubdealer = SessionHelper.IsSubdealer(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Vehicles);
            var query = await BuildQueryAsync(subdealerId, searchTerm, fromDate, toDate, dealershipLocation);
            query.ColumnFilters = columnFilters;
            if (isSubdealer)
                query.ExcludeRejected = true;

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var vehicles = await _mediator.Send(query);
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(vehicles, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedSubdealerId = subdealerId;
            ViewBag.SelectedDealershipLocation = dealershipLocation;
            ViewBag.IsSubdealer = isSubdealer;
            ViewBag.IsAdmin = SessionHelper.IsAdmin(HttpContext.Session);

            if (!ViewBag.IsSubdealer)
            {
                var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
                var allSubdealers = (await _mediator.Send(new GetSubdealersQuery
                {
                    IsActive = true,
                    DealershipId = scope
                })).ToList();
                var dealerships = (await _unitOfWork.Dealerships.GetAllAsync())
                    .Where(d => d.IsActive && (!scope.HasValue || d.DealershipId == scope.Value))
                    .OrderBy(d => d.Location ?? d.DealershipName)
                    .ToList();
                ViewBag.DealershipLocations = dealerships
                    .Select(d => d.Location?.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(l => l)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(dealershipLocation))
                {
                    var locDealershipIds = dealerships
                        .Where(d => string.Equals(d.Location?.Trim(), dealershipLocation.Trim(), StringComparison.OrdinalIgnoreCase))
                        .Select(d => d.DealershipId)
                        .ToHashSet();
                    var orgRoles = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                        .Where(r => r.IsActive && r.DealershipId.HasValue && locDealershipIds.Contains(r.DealershipId.Value))
                        .Select(r => r.UserId)
                        .ToHashSet();
                    ViewBag.Subdealers = allSubdealers.Where(s => orgRoles.Contains(s.UserId)).ToList();
                    if (ViewBag.Subdealers is List<UserDto> filtered && filtered.Count == 1 && !subdealerId.HasValue)
                        ViewBag.SelectedSubdealerId = filtered[0].UserId;
                }
                else
                {
                    ViewBag.Subdealers = allSubdealers;
                }
            }

            return View(pageItems);
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> Rejected(
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            int? page,
            int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (!SessionHelper.HasMenuAccess(HttpContext.Session, MenuKeys.VehiclesView))
                return RedirectToAction("AccessDenied", "Account");

            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Vehicles);
            var query = await BuildQueryAsync(null, searchTerm, fromDate, toDate);
            query.ColumnFilters = columnFilters;
            query.RejectedOnly = true;
            if (!fromDate.HasValue && !toDate.HasValue)
            {
                query.FromDate = null;
                query.ToDate = null;
            }

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var vehicles = await _mediator.Send(query);
            var (pageItems, pageInfo) = ListPagingHelper.Paginate(vehicles, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            ViewBag.FromDate = !fromDate.HasValue ? "" : from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = !toDate.HasValue ? "" : to.ToString("yyyy-MM-dd");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.IsSubdealer = true;
            ViewBag.IsAdmin = false;
            ViewBag.IsRejectedView = true;

            return View("Index", pageItems);
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Export(
            int? subdealerId,
            string? searchTerm,
            DateTime? fromDate,
            DateTime? toDate,
            string? dealershipLocation = null)
        {
            var isSubdealer = SessionHelper.IsSubdealer(HttpContext.Session);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Vehicles);
            var query = await BuildQueryAsync(subdealerId, searchTerm, fromDate, toDate, dealershipLocation);
            query.ColumnFilters = columnFilters;
            if (isSubdealer)
                query.ExcludeRejected = true;

            var vehicles = (await _mediator.Send(query)).ToList();

            var vehicleEntities = (await _unitOfWork.Vehicles.GetAllAsync())
                .ToDictionary(v => v.VehicleId);
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .GroupBy(b => b.VehicleId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.SubmittedDate).First());
            var documentTypes = (await _unitOfWork.DocumentTypes.GetAllAsync())
                .ToDictionary(d => d.DocumentTypeId, d => d.TypeName);
            var rtoLocations = (await _unitOfWork.RtoLocations.GetAllAsync())
                .ToDictionary(r => r.RtoLocationId, r => r.LocationName);
            var financeNames = (await _unitOfWork.FinanceNames.GetAllAsync())
                .ToDictionary(f => f.FinanceNameId, f => f.FinanceName);
            var bookingStatusNames = (await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle))
                .Where(s => s.StatusValue >= UnifiedVehicleStatus.BookedToCustomer)
                .ToDictionary(s => s.StatusValue, s => s.StatusName);

            var includeSubdealer = !SessionHelper.IsSubdealer(HttpContext.Session);
            var (headers, rows) = VehicleExportHelper.Build(
                vehicles,
                vehicleEntities,
                bookings,
                documentTypes,
                rtoLocations,
                financeNames,
                bookingStatusNames,
                includeSubdealer);

            return ExcelExportHelper.ToFileResult(this, $"vehicles_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Vehicles");
        }

        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> DetailsJson(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return Unauthorized();

            var query = new GetVehiclesQuery();
            if (SessionHelper.IsSubdealer(HttpContext.Session))
                query.SubdealerId = userId.Value;
            else
                query.DealershipId = SessionHelper.GetDealershipScope(HttpContext.Session);

            var vehicle = (await _mediator.Send(query)).FirstOrDefault(v => v.VehicleId == id);
            if (vehicle == null)
                return Json(new { success = false, message = "Vehicle not found or you do not have access." });

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == id);
            var bookingStatuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle);
            string? bookingStatusName = null;
            if (booking != null)
                bookingStatusName = bookingStatuses.FirstOrDefault(s => s.StatusValue == vehicle.Status)?.StatusName;

            object? bookingData = null;
            if (booking != null)
            {
                var docTypes = (await _unitOfWork.DocumentTypes.GetAllAsync()).ToDictionary(d => d.DocumentTypeId);
                var rtoLocs = (await _unitOfWork.RtoLocations.GetAllAsync()).ToDictionary(r => r.RtoLocationId);
                var financeNames = (await _unitOfWork.FinanceNames.GetAllAsync()).ToDictionary(f => f.FinanceNameId);
                docTypes.TryGetValue(booking.DocumentTypeId, out var docType);
                rtoLocs.TryGetValue(booking.RtoLocationId, out var rto);
                financeNames.TryGetValue(booking.FinanceNameId, out var finance);

                bookingData = new
                {
                    booking.VehicleBookingId,
                    status = bookingStatusName ?? vehicle.Status.ToString(),
                    booking.CustomerName,
                    booking.IsCompanyBooking,
                    booking.CustomerMobile,
                    booking.AlternativeMobile,
                    booking.CustomerEmail,
                    documentType = docType?.TypeName,
                    rtoLocation = rto?.LocationName,
                    fancyNumber = booking.FancyNumber ? "Yes" : "No",
                    paymentMode = VehiclePaymentModes.GetLabel(booking.PaymentMode),
                    financier = finance?.FinanceName,
                    nomineeName = booking.NomineeName,
                    nomineeDob = booking.NomineeDob.ToString("yyyy-MM-dd"),
                    nomineeRelationship = booking.NomineeRelationship,
                    submittedDate = booking.SubmittedDate.ToString("yyyy-MM-dd HH:mm"),
                    paperReceivedDate = booking.PaperReceivedDate?.ToString("yyyy-MM-dd"),
                    invoiceDate = booking.InvoiceDate?.ToString("yyyy-MM-dd"),
                    insuranceDate = booking.InsuranceDate?.ToString("yyyy-MM-dd"),
                    agentDate = booking.AgentDate?.ToString("yyyy-MM-dd"),
                    registrationDate = booking.RegistrationDate?.ToString("yyyy-MM-dd"),
                    rtoNumber = booking.RtoNumber,
                    numberPlateReceivedDate = booking.NumberPlateReceivedDate?.ToString("yyyy-MM-dd"),
                    subsidyId = booking.SubsidyId,
                    subsidyDocsSubmittedDate = booking.SubsidyDocsSubmittedDate?.ToString("yyyy-MM-dd"),
                    subsidyCustomerNameCaps = booking.SubsidyCustomerNameCaps,
                    hasInvoiceFile = !string.IsNullOrWhiteSpace(booking.InvoicePath),
                    hasInsuranceFile = !string.IsNullOrWhiteSpace(booking.InsurancePath),
                    invoiceViewUrl = BookingFileUrls.View(Url, _queryCrypto, booking.InvoicePath),
                    invoiceDownloadUrl = BookingFileUrls.Download(Url, _queryCrypto, booking.InvoicePath),
                    insuranceViewUrl = BookingFileUrls.View(Url, _queryCrypto, booking.InsurancePath),
                    insuranceDownloadUrl = BookingFileUrls.Download(Url, _queryCrypto, booking.InsurancePath)
                };
            }

            var entity = await _unitOfWork.Vehicles.GetByIdAsync(id);

            return Json(new
            {
                success = true,
                vehicle.VehicleId,
                vehicle.ModelName,
                vehicle.ColorName,
                vehicle.ChassisNumber,
                statusName = vehicle.StatusName ?? vehicle.GetStatusDisplay(),
                vehicle.MotorNo,
                vehicle.BatteryNo,
                vehicle.ChargerNo,
                vehicle.ControllerNo,
                vehicle.ConverterNo,
                vehicle.OrderNumber,
                orderDate = vehicle.OrderDate?.ToString("yyyy-MM-dd"),
                allocatedDate = vehicle.AllocatedDate?.ToString("yyyy-MM-dd HH:mm"),
                vehicle.SubdealerName,
                vehicle.CurrentPrice,
                originalPrice = entity?.OriginalPrice,
                vehicleStatus = entity?.Status,
                notes = entity?.Notes,
                correctionHistory = entity?.Notes,
                deliveryStatus = vehicle.GetDeliveryStatusDisplay(),
                deliveryDate = vehicle.DeliveryDate?.ToString("yyyy-MM-dd"),
                vehicle.CreatedDate,
                booking = bookingData
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        public async Task<IActionResult> RaiseReturn(int vehicleId, string returnReason)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (!SessionHelper.HasMenuAccess(HttpContext.Session, MenuKeys.VehiclesView))
                return RedirectToAction("AccessDenied", "Account");

            if (string.IsNullOrWhiteSpace(returnReason))
            {
                TempData["Error"] = "Return reason is required.";
                return RedirectToAction(nameof(Index));
            }

            var vehicle = (await _mediator.Send(new GetVehiclesQuery { SubdealerId = userId.Value }))
                .FirstOrDefault(v => v.VehicleId == vehicleId);
            if (vehicle == null || !vehicle.CanRequestReturn)
            {
                TempData["Error"] = vehicle?.IsDelivered == true
                    ? "Delivered vehicles cannot be returned."
                    : "This vehicle cannot be returned in its current status.";
                return RedirectToAction(nameof(Index));
            }

            if (!vehicle.PurchaseOrderId.HasValue)
            {
                TempData["Error"] = "Vehicle is not linked to a purchase order.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _unitOfWork.PurchaseOrders.GetByIdAsync(vehicle.PurchaseOrderId.Value);
            if (order == null)
            {
                TempData["Error"] = "Purchase order not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var returnId = await _mediator.Send(new CreateReturnRequestCommand
                {
                    AccountId = order.AccountId,
                    OrderId = order.OrderId,
                    VehicleId = vehicleId,
                    ReturnReason = returnReason.Trim(),
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Return request #{returnId} submitted. Admin will review your refund.";
                return RedirectToAction("MyReturns", "Returns");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error raising return: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.VehiclesView)]
        public async Task<IActionResult> MarkDelivered(int vehicleId, DateTime deliveryDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            try
            {
                await _mediator.Send(new MarkVehicleDeliveredCommand
                {
                    VehicleId = vehicleId,
                    DeliveryDate = deliveryDate,
                    MarkedBy = userId.Value
                });
                TempData["Success"] = "Vehicle marked as delivered.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [AuthorizeRole(1)]
        public async Task<IActionResult> AdminEdit(int id)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);
            if (vehicle == null) { TempData["Error"] = "Vehicle not found."; return RedirectToAction(nameof(Index)); }

            var booking = (await _unitOfWork.VehicleBookings.GetAllAsync())
                .FirstOrDefault(b => b.VehicleId == id);

            ViewBag.Models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            ViewBag.VehicleStatuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle);
            ViewBag.BookingStatuses = (await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle))
                .Where(s => s.StatusValue >= UnifiedVehicleStatus.BookedToCustomer);
            ViewBag.Booking = booking;
            ViewBag.Subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true });
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);
            return View(vehicle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> AdminEdit(
            int vehicleId, int modelId, int colorId, string chassisNumber, int status,
            decimal currentPrice, int? subdealerId, DateTime? deliveryDate,
            string? motorNo, string? batteryNo, string? chargerNo,
            string? controllerNo, string? converterNo, int? bookingStatus, string correctionReason)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(correctionReason) || correctionReason.Trim().Length < 5)
            {
                TempData["Error"] = "Correction reason is required (min 5 characters).";
                return this.RedirectEncrypted(nameof(AdminEdit), new { id = vehicleId });
            }

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                TempData["Error"] = "Vehicle not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _mediator.Send(new AdminCorrectVehicleCommand
                {
                    VehicleId = vehicleId,
                    ModelId = modelId,
                    ColorId = colorId,
                    ChassisNumber = chassisNumber,
                    Status = status,
                    CurrentPrice = currentPrice,
                    SubdealerId = subdealerId,
                    DeliveryDate = deliveryDate,
                    MotorNo = motorNo,
                    BatteryNo = batteryNo,
                    ChargerNo = chargerNo,
                    ControllerNo = controllerNo,
                    ConverterNo = converterNo,
                    BookingStatus = bookingStatus,
                    CorrectionReason = correctionReason.Trim(),
                    CorrectedBy = userId.Value,
                    CorrectedByName = SessionHelper.GetFullName(HttpContext.Session) ?? SessionHelper.GetUsername(HttpContext.Session) ?? "Admin"
                });

                TempData["Success"] = "Vehicle corrected. Change history recorded for subdealer view.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return this.RedirectEncrypted(nameof(AdminEdit), new { id = vehicleId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1)]
        public async Task<IActionResult> AdminDelete(int vehicleId, string deleteReason)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(deleteReason) || deleteReason.Trim().Length < 5)
            {
                TempData["Error"] = "Delete reason is required (min 5 characters).";
                return this.RedirectEncrypted(nameof(AdminEdit), new { id = vehicleId });
            }

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                TempData["Error"] = "Vehicle not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var ok = await _mediator.Send(new AdminDeleteVehicleCommand
                {
                    VehicleId = vehicleId,
                    DeleteReason = deleteReason.Trim(),
                    DeletedBy = userId.Value,
                    DeletedByName = SessionHelper.GetFullName(HttpContext.Session) ?? SessionHelper.GetUsername(HttpContext.Session) ?? "Admin"
                });

                TempData[ok ? "Success" : "Error"] = ok
                    ? "Vehicle deleted. Refund and audit entries were recorded where applicable."
                    : "Vehicle not found.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return this.RedirectEncrypted(nameof(AdminEdit), new { id = vehicleId });
            }
        }
    }
}
