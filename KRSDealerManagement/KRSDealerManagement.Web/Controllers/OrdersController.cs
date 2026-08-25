using Microsoft.AspNetCore.Mvc;
using MediatR;
using KRSDealerManagement.Application.Commands;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Web.Helpers;
using KRSDealerManagement.Web.Filters;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Web.Models;

namespace KRSDealerManagement.Web.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IStatusLookupService _statuses;
        private readonly IVehiclePriceService _priceService;

        public OrdersController(IMediator mediator, IStatusLookupService statuses, IVehiclePriceService priceService)
        {
            _mediator = mediator;
            _statuses = statuses;
            _priceService = priceService;
        }

        // ─── SUBDEALER SCREENS ───────────────────────────────────

        // GET: Orders/Create  (Subdealer creates purchase order)
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.PurchaseOrderCreate)]
        public async Task<IActionResult> Create()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            var prices = await _mediator.Send(new GetVehiclePricesQuery
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year
            });

            ViewBag.Models = models;
            ViewBag.Account = account;
            ViewBag.Prices = prices;
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);

            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.PurchaseOrderCreate)]
        public async Task<IActionResult> Create(string subdealerNotes,
            [FromForm] List<int> modelIds, [FromForm] List<int> colorIds,
            [FromForm] List<int> quantities, [FromForm] List<decimal> unitPrices)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, userId.Value);
            if (account == null)
            {
                TempData["Error"] = "No account found for your profile. Please contact administrator.";
                return RedirectToAction(nameof(Create));
            }

            if (!modelIds.Any())
            {
                TempData["Error"] = "Please add at least one vehicle to the order.";
                return RedirectToAction(nameof(Create));
            }

            // Build order items
            var items = modelIds.Select((modelId, i) => new OrderItem
            {
                ModelId = modelId,
                ColorId = colorIds[i],
                Quantity = quantities[i],
                UnitPrice = unitPrices[i]
            }).Where(item => item.Quantity > 0 && item.UnitPrice > 0).ToList();

            if (!items.Any())
            {
                TempData["Error"] = "Please add at least one valid vehicle to the order.";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                var orderId = await _mediator.Send(new CreatePurchaseOrderCommand
                {
                    AccountId = account.AccountId,
                    SubdealerId = userId.Value,
                    Items = items,
                    SubdealerNotes = subdealerNotes?.Trim(),
                    CreatedBy = userId.Value
                });

                TempData["Success"] = $"Purchase order created successfully! Order ID: {orderId}. Amount has been reserved from your account.";
                return RedirectToAction(nameof(MyOrders));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating order: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: Orders/MyOrders  (Subdealer views own orders)
        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.PurchaseOrderView)]
        public async Task<IActionResult> MyOrders(int? status, DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.MyOrders);
            var orders = await _mediator.Send(new GetPurchaseOrdersQuery
            {
                SubdealerId = userId.Value,
                Status = status,
                FromDate = from,
                ToDate = to,
                ColumnFilters = columnFilters
            });

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(orders, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            var itemsByOrder = new Dictionary<int, List<PurchaseOrderItemDto>>();
            foreach (var o in pageItems)
            {
                var items = (await _mediator.Send(new GetPurchaseOrderItemsQuery { OrderId = o.OrderId })).ToList();
                itemsByOrder[o.OrderId] = items;
            }
            ViewBag.ItemsByOrder = itemsByOrder;

            ViewBag.SelectedStatus = status;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle);
            return View(pageItems);
        }

        [AuthorizeRole(2)]
        [AuthorizeMenu(MenuKeys.PurchaseOrderView)]
        public async Task<IActionResult> ExportMyOrders(int? status, DateTime? fromDate, DateTime? toDate)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var orders = (await _mediator.Send(new GetPurchaseOrdersQuery
            {
                SubdealerId = userId.Value,
                Status = status,
                FromDate = from,
                ToDate = to
            })).ToList();

            var headers = new[] { "Order #", "Qty", "Amount", "Status", "Pending", "Approved", "Created", "Last Allocation", "Notes" };
            var rows = orders.Select(o => (IReadOnlyList<object?>)new List<object?>
            {
                o.OrderNumber, o.TotalQuantity, o.TotalAmount, o.GetStatusDisplay(),
                o.PendingItemCount, o.ApprovedItemCount, o.CreatedDate, o.LastAllocatedDate, o.SubdealerNotes ?? ""
            });
            return ExcelExportHelper.ToFileResult(this, $"my_orders_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "My Orders");
        }

        // GET: Orders/Details/5
        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> Details(int id)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var userRole = SessionHelper.GetUserRole(HttpContext.Session);
            var isSubdealer = userRole == 2;

            var orderQuery = new GetPurchaseOrdersQuery();
            if (isSubdealer && userId.HasValue)
                orderQuery.SubdealerId = userId.Value;
            else if (SessionHelper.IsStaff(HttpContext.Session))
                orderQuery.DealershipId = SessionHelper.GetDealershipScope(HttpContext.Session);

            var orders = await _mediator.Send(orderQuery);
            var order = orders.FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(isSubdealer ? nameof(MyOrders) : nameof(Index));
            }

            var items = (await _mediator.Send(new GetPurchaseOrderItemsQuery { OrderId = id })).ToList();
            ViewBag.Items = items;

            if (!isSubdealer)
            {
                var returns = await _mediator.Send(new GetReturnRequestsQuery());
                ViewBag.ReturnRequests = returns.Where(r => r.OrderId == id).ToList();
            }

            return View(order);
        }

        // GET: Orders/Allocate/5 — dealer allocates vehicles with serial numbers
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Allocate(int id)
        {
            var orders = await _mediator.Send(new GetPurchaseOrdersQuery());
            var order = orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            var items = await _mediator.Send(new GetPurchaseOrderItemsQuery { OrderId = id });
            ViewBag.Items = items;
            return View(order);
        }

        // POST: Orders/Allocate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Allocate(int orderId, string remarks,
            [FromForm] List<int> orderItemIds,
            [FromForm] List<string> actionFlags,
            [FromForm] List<string> chassisNumbers,
            [FromForm] List<string> motorNos,
            [FromForm] List<string> batteryNos,
            [FromForm] List<string> chargerNos,
            [FromForm] List<string> controllerNos,
            [FromForm] List<string> converterNos)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (orderItemIds == null || !orderItemIds.Any())
            {
                TempData["Error"] = "No vehicles selected.";
                return this.RedirectEncrypted(nameof(Allocate), new { id = orderId });
            }

            var items = new List<AllocateOrderItemDto>();
            for (int i = 0; i < orderItemIds.Count; i++)
            {
                var action = actionFlags != null && i < actionFlags.Count
                    ? (actionFlags[i] ?? "pending").Trim().ToLowerInvariant()
                    : "pending";
                if (action is not ("approve" or "reject"))
                    continue;

                items.Add(new AllocateOrderItemDto
                {
                    OrderItemId = orderItemIds[i],
                    Approve = action == "approve",
                    ChassisNumber = chassisNumbers != null && i < chassisNumbers.Count ? chassisNumbers[i] : null,
                    MotorNo = motorNos != null && i < motorNos.Count ? motorNos[i] : null,
                    BatteryNo = batteryNos != null && i < batteryNos.Count ? batteryNos[i] : null,
                    ChargerNo = chargerNos != null && i < chargerNos.Count ? chargerNos[i] : null,
                    ControllerNo = controllerNos != null && i < controllerNos.Count ? controllerNos[i] : null,
                    ConverterNo = converterNos != null && i < converterNos.Count ? converterNos[i] : null
                });
            }

            if (!items.Any())
            {
                TempData["Error"] = "Set at least one row to Approve or Reject (Pending rows are skipped).";
                return this.RedirectEncrypted(nameof(Allocate), new { id = orderId });
            }

            try
            {
                var ok = await _mediator.Send(new AllocatePurchaseOrderItemsCommand
                {
                    OrderId = orderId,
                    ApprovedBy = userId.Value,
                    Remarks = remarks?.Trim(),
                    Items = items
                });

                TempData[ok ? "Success" : "Error"] = ok
                    ? "Vehicles allocated successfully."
                    : "Could not allocate vehicles.";
                return this.RedirectEncrypted(nameof(Details), new { id = orderId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
                return this.RedirectEncrypted(nameof(Allocate), new { id = orderId });
            }
        }

        // ─── ADMIN/DEALER SCREENS ─────────────────────────────────

        // GET: Orders/Index  (System admin / branch manager)
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Index(int? status, int? subdealerId, string searchTerm, DateTime? fromDate, DateTime? toDate, int? page, int? pageSize)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var columnFilters = GridViewHelper.SetupGridFilters(this, GridIds.Orders);
            var orders = await _mediator.Send(new GetPurchaseOrdersQuery
            {
                Status = status,
                SubdealerId = subdealerId,
                SearchTerm = searchTerm,
                DealershipId = scope,
                FromDate = from,
                ToDate = to,
                ColumnFilters = columnFilters
            });

            var (pageItems, pageInfo) = ListPagingHelper.Paginate(orders, page, pageSize);
            ListPagingHelper.ApplyToViewBag(ViewBag, pageInfo);

            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            ViewBag.Subdealers = subdealers;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedSubdealerId = subdealerId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.FromDate = from.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to.ToString("yyyy-MM-dd");
            ViewBag.FilteredTotal = pageInfo.TotalItems;
            ViewBag.PendingCount = orders.Count(o => o.Status == UnifiedVehicleStatus.Submitted);
            ViewBag.Statuses = await _statuses.GetActiveByCategoryAsync(StatusCategories.Vehicle);

            return View(pageItems);
        }

        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Export(int? status, int? subdealerId, string searchTerm, DateTime? fromDate, DateTime? toDate)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var (from, to) = ListPagingHelper.ResolveDateRange(fromDate, toDate);
            var orders = (await _mediator.Send(new GetPurchaseOrdersQuery
            {
                Status = status,
                SubdealerId = subdealerId,
                SearchTerm = searchTerm,
                DealershipId = scope,
                FromDate = from,
                ToDate = to
            })).ToList();

            var headers = new[] { "Order #", "Subdealer", "Qty", "Amount", "Status", "Pending", "Approved", "Created", "Last Allocation" };
            var rows = orders.Select(o => (IReadOnlyList<object?>)new List<object?>
            {
                o.OrderNumber, o.SubdealerName, o.TotalQuantity, o.TotalAmount, o.GetStatusDisplay(),
                o.PendingItemCount, o.ApprovedItemCount, o.CreatedDate, o.LastAllocatedDate
            });
            return ExcelExportHelper.ToFileResult(this, $"orders_{DateTime.Now:yyyyMMdd}.xlsx", headers, rows, "Orders");
        }

        // GET: Orders/CreateForSubdealer  (Staff creates auto-approved order for subdealer)
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> CreateForSubdealer()
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });

            ViewBag.Subdealers = subdealers;
            ViewBag.Models = models;
            ViewBag.CanViewBalances = SessionHelper.HasMenuAccess(HttpContext.Session, StaffMenuAccess.Balances);
            await ModelColorViewHelper.SetModelColorMapAsync(this, _mediator);

            return View();
        }

        // POST: Orders/CreateForSubdealer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> CreateForSubdealer(
            int subdealerId,
            string? adminNotes,
            [FromForm] List<int> modelIds, [FromForm] List<int> colorIds,
            [FromForm] List<decimal> unitPrices,
            [FromForm] List<string> chassisNumbers, [FromForm] List<string> motorNos,
            [FromForm] List<string> batteryNos, [FromForm] List<string> chargerNos,
            [FromForm] List<string> controllerNos, [FromForm] List<string> converterNos)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var scopedSubdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            if (!scopedSubdealers.Any(s => s.UserId == subdealerId))
            {
                TempData["Error"] = "Selected subdealer is not in your dealership scope.";
                return RedirectToAction(nameof(CreateForSubdealer));
            }

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, subdealerId);
            if (account == null)
            {
                TempData["Error"] = "No active account found for the selected subdealer.";
                return RedirectToAction(nameof(CreateForSubdealer));
            }

            if (!modelIds.Any())
            {
                TempData["Error"] = "Please add at least one vehicle to the order.";
                return RedirectToAction(nameof(CreateForSubdealer));
            }

            var items = modelIds.Select((modelId, i) => new OrderItem
            {
                ModelId = modelId,
                ColorId = colorIds[i],
                Quantity = 1,
                UnitPrice = unitPrices[i],
                ChassisNumber = chassisNumbers.Count > i ? chassisNumbers[i]?.Trim() : null,
                MotorNo = motorNos.Count > i ? motorNos[i]?.Trim() : null,
                BatteryNo = batteryNos.Count > i ? batteryNos[i]?.Trim() : null,
                ChargerNo = chargerNos.Count > i ? chargerNos[i]?.Trim() : null,
                ControllerNo = controllerNos.Count > i ? controllerNos[i]?.Trim() : null,
                ConverterNo = converterNos.Count > i ? converterNos[i]?.Trim() : null
            }).Where(item => item.UnitPrice > 0).ToList();

            if (!items.Any())
            {
                TempData["Error"] = "Please add at least one valid vehicle to the order.";
                return RedirectToAction(nameof(CreateForSubdealer));
            }

            var missingSerials = items.Any(item =>
                string.IsNullOrWhiteSpace(item.ChassisNumber)
                || string.IsNullOrWhiteSpace(item.MotorNo)
                || string.IsNullOrWhiteSpace(item.BatteryNo)
                || string.IsNullOrWhiteSpace(item.ChargerNo)
                || string.IsNullOrWhiteSpace(item.ControllerNo)
                || string.IsNullOrWhiteSpace(item.ConverterNo));

            if (missingSerials)
            {
                TempData["Error"] = "Chassis, motor, battery, charger, controller, and converter numbers are required for each vehicle.";
                return RedirectToAction(nameof(CreateForSubdealer));
            }

            try
            {
                var orderId = await _mediator.Send(new CreatePurchaseOrderCommand
                {
                    AccountId = account.AccountId,
                    SubdealerId = subdealerId,
                    Items = items,
                    AdminNotes = adminNotes?.Trim(),
                    CreatedBy = userId.Value,
                    AutoApprove = true
                });

                TempData["Success"] = $"Purchase order #{orderId} created and auto-approved. Amount deducted from subdealer balance.";
                return this.RedirectEncrypted(nameof(Details), new { id = orderId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating order: {ex.Message}";
                return RedirectToAction(nameof(CreateForSubdealer));
            }
        }

        // GET: Orders/GetSubdealerAccount (AJAX — finance/admin balance lookup for staff PO form)
        [AuthorizeRole(1, 3)]
        [AuthorizeMenu(StaffMenuAccess.Balances)]
        public async Task<IActionResult> GetSubdealerAccount(int subdealerId)
        {
            var scope = SessionHelper.GetDealershipScope(HttpContext.Session);
            var scopedSubdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = scope });
            if (!scopedSubdealers.Any(s => s.UserId == subdealerId))
                return Json(new { success = false, message = "Subdealer not in your scope." });

            var account = await AccountHelper.GetPrimaryAccountAsync(_mediator, subdealerId);
            if (account == null)
                return Json(new { success = false, message = "No active account found for this subdealer." });

            return Json(new
            {
                success = true,
                accountId = account.AccountId,
                subdealerName = account.SubdealerName,
                availableBalance = account.AvailableBalance,
                currentBalance = account.CurrentBalance,
                reservedAmount = account.ReservedAmount
            });
        }

        // Legacy whole-order approve — redirect to per-vehicle Allocate
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public IActionResult Approve(int id, decimal amount, string remarks)
        {
            return this.RedirectEncrypted(nameof(Allocate), new { id });
        }

        // Legacy whole-order reject — reject all pending line items
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole(1, 4)]
        public async Task<IActionResult> Reject(int id, decimal amount, string remarks)
        {
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["Error"] = "Remarks are required when rejecting an order.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _mediator.Send(new RejectPurchaseOrderItemsCommand
                {
                    OrderId = id,
                    RejectedBy = userId.Value,
                    Remarks = remarks.Trim()
                });

                TempData[result ? "Success" : "Error"] = result
                    ? $"Order #{id}: pending vehicles rejected and reserved amount released."
                    : "Order not found or no pending vehicles.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/GetPrice (AJAX - get price for model+color+month)
        [AuthorizeRole(1, 2, 4)]
        public async Task<IActionResult> GetPrice(int modelId, int colorId, DateTime? asOfDate)
        {
            var date = (asOfDate ?? DateTime.Today).Date;
            var price = await _priceService.GetPriceAsOfAsync(modelId, colorId, date);
            if (!price.HasValue)
                return Json(new { success = false, message = $"No price found for this model/color effective on {date:yyyy-MM-dd}." });

            var models = await _mediator.Send(new GetVehicleModelsQuery { IsActive = true });
            var colors = await _mediator.Send(new GetVehicleColorsQuery { IsActive = true });
            var modelName = models.FirstOrDefault(m => m.ModelId == modelId)?.ModelName ?? "";
            var colorName = colors.FirstOrDefault(c => c.ColorId == colorId)?.ColorName ?? "";

            return Json(new { success = true, price = price.Value, modelName, colorName, asOfDate = date.ToString("yyyy-MM-dd") });
        }
    }
}
