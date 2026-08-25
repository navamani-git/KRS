using MediatR;
using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Domain.Repositories;
using KRSDealerManagement.Shared.Constants;
using KRSDealerManagement.Shared.Helpers;

namespace KRSDealerManagement.Application.Handlers.Queries
{
    public class GetGridDistinctValuesQueryHandler : IRequestHandler<GetGridDistinctValuesQuery, IReadOnlyList<string>>
    {
        private readonly IMediator _mediator;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStatusLookupService _statuses;

        public GetGridDistinctValuesQueryHandler(IMediator mediator, IUnitOfWork unitOfWork, IStatusLookupService statuses)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _statuses = statuses;
        }

        public async Task<IReadOnlyList<string>> Handle(GetGridDistinctValuesQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.GridId) || string.IsNullOrWhiteSpace(request.Column))
                return Array.Empty<string>();

            var grid = request.GridId.Trim().ToLowerInvariant();
            var column = request.Column.Trim().ToLowerInvariant();

            return grid switch
            {
                GridScreenIds.Subdealers => await DistinctFrom(await _mediator.Send(new GetSubdealersQuery
                {
                    DealershipId = request.DealershipId,
                    IsActive = null
                }), column, request, SubdealerProjections),

                GridScreenIds.Vehicles => await DistinctFrom(await _mediator.Send(new GetVehiclesQuery
                {
                    SubdealerId = request.SubdealerId,
                    DealershipId = request.DealershipId,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    SearchTerm = request.SearchTerm
                }), column, request, VehicleProjections),

                GridScreenIds.Payments or GridScreenIds.MyPayments => await DistinctFrom(await _mediator.Send(new GetPaymentsQuery
                {
                    SubdealerId = grid == GridScreenIds.MyPayments ? request.UserId : request.SubdealerId,
                    Status = request.Status,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                }), column, request, PaymentProjections),

                GridScreenIds.Orders => await DistinctFrom(await _mediator.Send(new GetPurchaseOrdersQuery
                {
                    DealershipId = request.DealershipId,
                    SubdealerId = request.SubdealerId,
                    Status = request.Status,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    SearchTerm = request.SearchTerm
                }), column, request, OrderProjections),

                GridScreenIds.MyOrders => await DistinctFrom(await _mediator.Send(new GetPurchaseOrdersQuery
                {
                    SubdealerId = request.UserId,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                }), column, request, MyOrderProjections),

                GridScreenIds.Returns or GridScreenIds.MyReturns => await DistinctFrom(await _mediator.Send(new GetReturnRequestsQuery
                {
                    SubdealerId = grid == GridScreenIds.MyReturns ? request.UserId : request.SubdealerId
                }), column, request, grid == GridScreenIds.MyReturns ? MyReturnProjections : ReturnProjections),

                GridScreenIds.CommissionApprovals => await DistinctFrom(await _mediator.Send(new GetCommissionsQuery()), column, request, CommissionApprovalProjections),

                GridScreenIds.CommissionRates => await DistinctFrom(await _mediator.Send(new GetCommissionRatesQuery
                {
                    ActiveOnly = null
                }), column, request, CommissionRateProjections),

                GridScreenIds.Dealerships => await DistinctFrom(await _mediator.Send(new GetDealershipsQuery()), column, request, DealershipProjections),

                GridScreenIds.Prices => await DistinctFrom(await _mediator.Send(new GetVehiclePricesQuery()), column, request, PriceProjections),

                GridScreenIds.VehicleModels => await DistinctFrom(await _mediator.Send(new GetVehicleModelsQuery()), column, request, VehicleModelProjections),

                GridScreenIds.VehicleColors => await DistinctFrom(await _mediator.Send(new GetVehicleColorsQuery()), column, request, VehicleColorProjections),

                GridScreenIds.StaffUsers => await DistinctFrom(await _mediator.Send(new GetStaffUsersQuery
                {
                    DealershipId = request.DealershipId
                }), column, request, StaffUserProjections),

                GridScreenIds.Accounts => await DistinctAccounts(column, request),

                GridScreenIds.AccountStatement => await DistinctAccountStatement(column, request),

                GridScreenIds.VehicleBookings => await DistinctVehicleBookings(column, request),

                GridScreenIds.DocumentTypes => DistinctSync((await _unitOfWork.DocumentTypes.GetAllAsync()).Cast<object>(), column, request, DocumentTypeProjections),

                GridScreenIds.FinanceNames => DistinctSync((await _unitOfWork.FinanceNames.GetAllAsync()).Cast<object>(), column, request, FinanceNameProjections),

                GridScreenIds.PaymentTypes => DistinctSync((await _unitOfWork.PaymentTypes.GetAllAsync()).Cast<object>(), column, request, PaymentTypeProjections),

                GridScreenIds.RtoLocations => DistinctSync((await _unitOfWork.RtoLocations.GetAllAsync()).Cast<object>(), column, request, RtoLocationProjections),

                GridScreenIds.StatusLookups => await DistinctStatusLookups(column, request),

                _ => Array.Empty<string>()
            };
        }

        private async Task<IReadOnlyList<string>> DistinctAccounts(string column, GetGridDistinctValuesQuery request)
        {
            var subdealers = await _mediator.Send(new GetSubdealersQuery { IsActive = true, DealershipId = request.DealershipId });
            var accounts = new List<SubdealerAccountDto>();
            foreach (var s in subdealers)
            {
                if (request.SubdealerId.HasValue && s.UserId != request.SubdealerId.Value) continue;
                accounts.AddRange(await _mediator.Send(new GetSubdealerAccountsQuery { SubdealerId = s.UserId }));
            }
            return DistinctSync(accounts.Cast<object>(), column, request, AccountProjections);
        }

        private async Task<IReadOnlyList<string>> DistinctAccountStatement(string column, GetGridDistinctValuesQuery request)
        {
            if (!request.AccountId.HasValue)
                return Array.Empty<string>();

            var rows = await _mediator.Send(new GetAccountTransactionsQuery
            {
                AccountId = request.AccountId.Value,
                FromDate = request.FromDate,
                ToDate = request.ToDate
            });

            return DistinctSync(rows.Cast<object>(), column, request, AccountStatementProjections);
        }

        private async Task<IReadOnlyList<string>> DistinctVehicleBookings(string column, GetGridDistinctValuesQuery request)
        {
            var bookings = (await _unitOfWork.VehicleBookings.GetAllAsync()).ToList();
            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync()).ToDictionary(v => v.VehicleId);
            var users = (await _unitOfWork.Users.GetAllAsync()).ToDictionary(u => u.UserId);
            var scopedIds = await GetBookingScopedSubdealerIdsAsync(request.DealershipId);

            var rows = new List<VehicleBookingGridRowDto>();
            foreach (var b in bookings.Where(b => scopedIds.Contains(b.SubdealerId))
                         .Where(b => !request.SubdealerId.HasValue || b.SubdealerId == request.SubdealerId.Value))
            {
                vehicles.TryGetValue(b.VehicleId, out var v);
                users.TryGetValue(b.SubdealerId, out var u);
                var vehicleStatus = v?.Status ?? b.BookingStatus;
                var statusName = await _statuses.GetNameAsync(StatusCategories.Vehicle, vehicleStatus);
                rows.Add(new VehicleBookingGridRowDto
                {
                    Booking = b,
                    Chassis = v?.ChassisNumber ?? "-",
                    Subdealer = u?.GetFullName() ?? "Unknown",
                    StatusName = statusName,
                    VehicleStatus = vehicleStatus
                });
            }

            IEnumerable<VehicleBookingGridRowDto> list = rows;
            if (request.Status.HasValue)
            {
                list = list.Where(x => BookingStageFilter.MatchesStage(
                    x.VehicleStatus,
                    request.Status.Value,
                    x.Booking.PaperReceivedDate,
                    x.Booking.InvoiceDate,
                    x.Booking.InsuranceDate,
                    x.Booking.AgentDate,
                    x.Booking.RegistrationDate,
                    x.Booking.SubsidyId));
            }
            else if (request.BookingPhaseOnly)
            {
                list = list.Where(x => BookingStageFilter.IsBookingPhase(
                    BookingStageFilter.ResolveEffectiveStage(
                        x.VehicleStatus,
                        x.Booking.PaperReceivedDate,
                        x.Booking.InvoiceDate,
                        x.Booking.InsuranceDate,
                        x.Booking.AgentDate,
                        x.Booking.RegistrationDate,
                        x.Booking.SubsidyId)));
            }

            return DistinctSync(list.Cast<object>(), column, request, VehicleBookingProjections);
        }

        private async Task<HashSet<int>> GetBookingScopedSubdealerIdsAsync(int? dealershipId)
        {
            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToList();
            var subRole = roles.FirstOrDefault(r =>
                r.RoleCode.Equals(RoleCodes.Subdealer, StringComparison.OrdinalIgnoreCase));

            var assignments = (await _unitOfWork.UserOrgRoles.GetAllAsync())
                .Where(a => a.IsActive && (subRole == null || a.RoleId == subRole.RoleId));

            if (dealershipId.HasValue)
                assignments = assignments.Where(a => a.DealershipId == dealershipId.Value);

            return assignments.Select(a => a.UserId).ToHashSet();
        }

        private async Task<IReadOnlyList<string>> DistinctStatusLookups(string column, GetGridDistinctValuesQuery request)
        {
            var all = await _statuses.GetAllByCategoryAsync();
            return DistinctSync(all.Cast<object>(), column, request, StatusLookupProjections);
        }

        private static Task<IReadOnlyList<string>> DistinctFrom<T>(
            IEnumerable<T> rows,
            string column,
            GetGridDistinctValuesQuery request,
            IReadOnlyDictionary<string, Func<T, string?>> map)
        {
            if (!map.TryGetValue(column, out var selector))
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

            return Task.FromResult(Extract(rows.Select(selector), request));
        }

        private static IReadOnlyList<string> DistinctSync<T>(
            IEnumerable<object> rows,
            string column,
            GetGridDistinctValuesQuery request,
            IReadOnlyDictionary<string, Func<T, string?>> map)
        {
            if (!map.TryGetValue(column, out var selector))
                return Array.Empty<string>();

            var typed = rows.OfType<T>();
            return Extract(typed.Select(selector), request);
        }

        private static IReadOnlyList<string> Extract(IEnumerable<string?> values, GetGridDistinctValuesQuery request)
        {
            var q = values
                .Where(v => !string.IsNullOrWhiteSpace(v) && v!.Trim() != "-")
                .Select(v => v!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var s = request.Search.Trim();
                q = q.Where(v => v.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            return q.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).Take(request.Limit).ToList();
        }

        private static readonly Dictionary<string, Func<UserDto, string?>> SubdealerProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = s => s.GetFullName(),
            ["location"] = s => s.LastName,
            ["phone"] = s => s.PhoneNumber,
            ["status"] = s => s.IsActive ? "Active" : "Inactive",
            ["created"] = s => s.CreatedDate.ToString("yyyy-MM-dd")
        };

        private static readonly Dictionary<string, Func<VehicleDto, string?>> VehicleProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["subdealer"] = v => v.SubdealerName,
            ["orderDate"] = v => v.OrderDate?.ToString("yyyy-MM-dd"),
            ["orderNumber"] = v => v.OrderNumber,
            ["allocated"] = v => v.AllocatedDate?.ToString("yyyy-MM-dd"),
            ["chassis"] = v => v.ChassisNumber,
            ["model"] = v => v.ModelName,
            ["color"] = v => v.ColorName,
            ["source"] = v => v.CreatedByDealer ? "Dealer" : "Subdealer",
            ["price"] = v => v.CurrentPrice.ToString("N2"),
            ["delivery"] = v => v.GetDeliveryStatusDisplay(),
            ["status"] = v => v.GetStatusDisplay(),
            ["motor"] = v => v.MotorNo,
            ["battery"] = v => v.BatteryNo
        };

        private static readonly Dictionary<string, Func<PaymentDto, string?>> PaymentProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["subdealer"] = p => p.SubdealerName,
            ["amount"] = p => p.Amount.ToString("N2"),
            ["type"] = p => p.GetPaymentTypeDisplay(),
            ["customer"] = p => p.CustomerName,
            ["finance"] = p => p.FinanceName,
            ["vin"] = p => p.VinNumber,
            ["status"] = p => p.GetStatusDisplay(),
            ["paymentDate"] = p => p.PaymentDate.ToString("yyyy-MM-dd"),
            ["submitted"] = p => p.CreatedDate.ToString("yyyy-MM-dd"),
            ["approved"] = p => p.ProcessedDate?.ToString("yyyy-MM-dd"),
            ["received"] = p => p.ActualReceivedDate?.ToString("yyyy-MM-dd"),
            ["receivedAmt"] = p => p.ActualReceivedAmount?.ToString("N2")
        };

        private static readonly Dictionary<string, Func<PurchaseOrderDto, string?>> OrderProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["orderNumber"] = o => o.OrderNumber,
            ["subdealer"] = o => o.SubdealerName,
            ["created"] = o => o.CreatedDate.ToString("yyyy-MM-dd"),
            ["allocated"] = o => o.LastAllocatedDate?.ToString("yyyy-MM-dd"),
            ["qty"] = o => o.TotalQuantity.ToString(),
            ["pending"] = o => o.PendingItemCount.ToString(),
            ["amount"] = o => o.TotalAmount.ToString("N2"),
            ["status"] = o => o.GetStatusDisplay(),
            ["notes"] = o => o.AdminNotes ?? o.SubdealerNotes
        };

        private static readonly Dictionary<string, Func<PurchaseOrderDto, string?>> MyOrderProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["orderNumber"] = o => o.OrderNumber,
            ["created"] = o => o.CreatedDate.ToString("yyyy-MM-dd"),
            ["allocated"] = o => o.LastAllocatedDate?.ToString("yyyy-MM-dd"),
            ["qty"] = o => o.TotalQuantity.ToString(),
            ["amount"] = o => o.TotalAmount.ToString("N2"),
            ["status"] = o => o.GetStatusDisplay(),
            ["notes"] = o => o.AdminNotes ?? o.SubdealerNotes
        };

        private static readonly Dictionary<string, Func<ReturnRequestDto, string?>> ReturnProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["account"] = r => r.AccountName,
            ["order"] = r => r.OrderNumber,
            ["vehicle"] = r => r.VehicleChassisNumber,
            ["refund"] = r => r.RefundAmount.ToString("N2"),
            ["reason"] = r => r.ReturnReason,
            ["status"] = r => r.GetStatusDisplay(),
            ["requested"] = r => r.CreatedDate.ToString("yyyy-MM-dd"),
            ["processed"] = r => r.ProcessedDate?.ToString("yyyy-MM-dd")
        };

        private static readonly Dictionary<string, Func<ReturnRequestDto, string?>> MyReturnProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["order"] = r => r.OrderNumber,
            ["chassis"] = r => r.VehicleChassisNumber,
            ["account"] = r => r.AccountName,
            ["refund"] = r => r.RefundAmount.ToString("N2"),
            ["status"] = r => r.GetStatusDisplay(),
            ["reason"] = r => r.ReturnReason,
            ["requested"] = r => r.CreatedDate.ToString("yyyy-MM-dd"),
            ["processed"] = r => r.ProcessedDate?.ToString("yyyy-MM-dd"),
            ["credited"] = r => r.RefundCreditedDate?.ToString("yyyy-MM-dd"),
            ["remarks"] = r => r.AdminRemarks
        };

        private static readonly Dictionary<string, Func<CommissionDto, string?>> CommissionApprovalProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["subdealer"] = c => c.SubdealerName,
            ["chassis"] = c => c.VehicleChassisNumber,
            ["period"] = c => $"{c.Year}-{c.Month:D2}",
            ["amount"] = c => c.CommissionAmount.ToString("N2"),
            ["status"] = c => c.GetStatusDisplay(),
            ["submitted"] = c => c.CreatedDate.ToString("yyyy-MM-dd"),
            ["approved"] = c => c.ApprovedDate?.ToString("yyyy-MM-dd"),
            ["rejected"] = c => c.RejectedDate?.ToString("yyyy-MM-dd"),
            ["remarks"] = c => c.Notes
        };

        private static readonly Dictionary<string, Func<CommissionRateDto, string?>> CommissionRateProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["model"] = r => r.ModelName,
            ["amount"] = r => r.CommissionAmount.ToString("N2"),
            ["from"] = r => r.EffectiveFrom.ToString("yyyy-MM-dd"),
            ["to"] = r => r.EffectiveTo.ToString("yyyy-MM-dd"),
            ["status"] = r => r.IsActive() ? "Active" : "Inactive",
            ["notes"] = r => r.Notes
        };

        private static readonly Dictionary<string, Func<DealershipDto, string?>> DealershipProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = d => d.DealershipCode,
            ["name"] = d => d.DealershipName,
            ["location"] = d => d.Location,
            ["phone"] = d => d.ContactPhone,
            ["status"] = d => d.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<VehiclePriceHistoryDto, string?>> PriceProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["model"] = p => p.ModelName,
            ["color"] = p => p.ColorName,
            ["period"] = p => $"{p.Month}/{p.Year}",
            ["from"] = p => p.EffectiveFrom.ToString("yyyy-MM-dd"),
            ["price"] = p => p.Price.ToString("N2"),
            ["notes"] = p => p.Notes
        };

        private static readonly Dictionary<string, Func<VehicleModelDto, string?>> VehicleModelProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = m => m.ModelName,
            ["description"] = m => m.Description,
            ["status"] = m => m.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<VehicleColorDto, string?>> VehicleColorProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["color"] = c => c.ColorName,
            ["name"] = c => c.ColorName,
            ["hex"] = c => c.HexCode,
            ["status"] = c => c.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<StaffUserDto, string?>> StaffUserProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = u => u.FullName,
            ["role"] = u => u.RoleName,
            ["dealership"] = u => u.DealershipName,
            ["username"] = u => u.Username,
            ["phone"] = u => u.PhoneNumber,
            ["status"] = u => u.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<SubdealerAccountDto, string?>> AccountProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["subdealer"] = a => a.SubdealerName,
            ["current"] = a => a.CurrentBalance.ToString("N2"),
            ["reserved"] = a => a.ReservedAmount.ToString("N2"),
            ["available"] = a => a.AvailableBalance.ToString("N2"),
            ["status"] = a => a.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<AccountTransactionDto, string?>> AccountStatementProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = t => t.CategoryLabel,
            ["description"] = t => string.IsNullOrWhiteSpace(t.ChassisNumber) ? t.Reason : $"{t.Reason} {t.ChassisNumber}".Trim(),
            ["customer"] = t => t.CustomerName,
            ["payType"] = t => t.PaymentType,
            ["finance"] = t => t.FinanceName,
            ["vin"] = t => t.VinNumber ?? t.ChassisNumber,
            ["requestedAmt"] = t => t.RequestedAmount?.ToString("N2"),
            ["approvedAmt"] = t => t.ApprovedPaymentAmount?.ToString("N2"),
            ["debit"] = t => t.IsDebit() ? t.Amount.ToString("N2") : null,
            ["credit"] = t => t.IsCredit() ? t.Amount.ToString("N2") : null,
            ["balance"] = t => t.BalanceAfterTransaction.ToString("N2"),
            ["txnDate"] = t => t.CreatedDate.ToString("yyyy-MM-dd")
        };

        private static readonly Dictionary<string, Func<VehicleBookingGridRowDto, string?>> VehicleBookingProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = r => r.Booking.VehicleBookingId.ToString(),
            ["chassis"] = r => r.Chassis,
            ["subdealer"] = r => r.Subdealer,
            ["customer"] = r => r.Booking.CustomerName,
            ["mobile"] = r => r.Booking.CustomerMobile,
            ["status"] = r => r.StatusName,
            ["submitted"] = r => r.Booking.SubmittedDate.ToString("yyyy-MM-dd")
        };

        private static readonly Dictionary<string, Func<DocumentTypeMaster, string?>> DocumentTypeProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = d => d.TypeName,
            ["status"] = d => d.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<FinanceNameMaster, string?>> FinanceNameProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = f => f.FinanceName,
            ["status"] = f => f.IsActive ? "Active" : "Inactive",
            ["created"] = f => f.CreatedDate.ToString("yyyy-MM-dd")
        };

        private static readonly Dictionary<string, Func<PaymentType, string?>> PaymentTypeProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = p => p.TypeCode,
            ["name"] = p => p.TypeName,
            ["status"] = p => p.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<RtoLocationMaster, string?>> RtoLocationProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["location"] = r => r.LocationName,
            ["status"] = r => r.IsActive ? "Active" : "Inactive"
        };

        private static readonly Dictionary<string, Func<StatusLookup, string?>> StatusLookupProjections = new(StringComparer.OrdinalIgnoreCase)
        {
            ["category"] = s => s.Category,
            ["value"] = s => s.StatusValue.ToString(),
            ["code"] = s => s.StatusCode,
            ["name"] = s => s.StatusName,
            ["badge"] = s => s.BadgeClass,
            ["status"] = s => s.IsActive ? "Active" : "Inactive"
        };
    }
}
