using KRSDealerManagement.Application.DTOs;
using KRSDealerManagement.Application.Helpers;
using KRSDealerManagement.Application.Queries;
using KRSDealerManagement.Application.Services;
using KRSDealerManagement.Domain.Entities;
using KRSDealerManagement.Shared.Constants;

namespace KRSDealerManagement.Web.Helpers
{
    public static class GridScreenFilterHelper
    {
        public static IEnumerable<SubdealerAccountDto> ApplyAccounts(IEnumerable<SubdealerAccountDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.Accounts, rows, filters, new Dictionary<string, Func<SubdealerAccountDto, string?>>(StringComparer.OrdinalIgnoreCase)
            {
                ["subdealer"] = a => a.SubdealerName,
                ["current"] = a => a.CurrentBalance.ToString("N2"),
                ["reserved"] = a => a.ReservedAmount.ToString("N2"),
                ["available"] = a => a.AvailableBalance.ToString("N2"),
                ["status"] = a => a.IsActive ? "Active" : "Inactive"
            });

        public static IEnumerable<AccountTransactionDto> ApplyAccountStatement(
            IEnumerable<AccountTransactionDto> rows,
            IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.AccountStatement, rows, filters,
                new Dictionary<string, Func<AccountTransactionDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["type"] = t => t.CategoryLabel,
                    ["description"] = t => FormatDescription(t),
                    ["customer"] = t => t.CustomerName,
                    ["payType"] = t => t.PaymentType,
                    ["finance"] = t => t.FinanceName,
                    ["vin"] = t => t.VinNumber ?? t.ChassisNumber,
                    ["requestedAmt"] = t => t.RequestedAmount?.ToString("N2"),
                    ["approvedAmt"] = t => t.ApprovedPaymentAmount?.ToString("N2"),
                    ["debit"] = t => t.IsDebit() ? t.Amount.ToString("N2") : null,
                    ["credit"] = t => t.IsCredit() ? t.Amount.ToString("N2") : null,
                    ["balance"] = t => t.BalanceAfterTransaction.ToString("N2")
                },
                new Dictionary<string, Func<AccountTransactionDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["txnDate"] = t => t.CreatedDate
                });

        private static string? FormatDescription(AccountTransactionDto t)
            => string.IsNullOrWhiteSpace(t.Reason) ? null : t.Reason.Replace("\n", " ");

        public static IEnumerable<ReturnRequestDto> ApplyReturns(IEnumerable<ReturnRequestDto> rows, IReadOnlyDictionary<string, string>? filters, bool myReturns = false)
        {
            if (myReturns)
            {
                return GridRowFilterApplier.Apply(GridScreenIds.MyReturns, rows, filters,
                    new Dictionary<string, Func<ReturnRequestDto, string?>>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["request"] = r => r.ReturnRequestId.ToString(),
                        ["order"] = r => r.OrderNumber,
                        ["chassis"] = r => r.VehicleChassisNumber,
                        ["refund"] = r => r.RefundAmount.ToString("N2"),
                        ["status"] = r => r.GetStatusDisplay(),
                        ["reason"] = r => r.ReturnReason,
                        ["remarks"] = r => r.AdminRemarks
                    },
                    new Dictionary<string, Func<ReturnRequestDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["requested"] = r => r.CreatedDate,
                        ["processed"] = r => r.ProcessedDate,
                        ["credited"] = r => r.RefundCreditedDate
                    });
            }

            return GridRowFilterApplier.Apply(GridScreenIds.Returns, rows, filters,
                new Dictionary<string, Func<ReturnRequestDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["subdealer"] = r => r.SubdealerName ?? r.AccountName,
                    ["order"] = r => r.OrderNumber,
                    ["vehicle"] = r => r.VehicleChassisNumber,
                    ["refund"] = r => r.RefundAmount.ToString("N2"),
                    ["reason"] = r => r.ReturnReason,
                    ["status"] = r => r.GetStatusDisplay()
                },
                new Dictionary<string, Func<ReturnRequestDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["requested"] = r => r.CreatedDate,
                    ["processed"] = r => r.ProcessedDate
                });
        }

        public static IEnumerable<CommissionDto> ApplyCommissionApprovals(IEnumerable<CommissionDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.CommissionApprovals, rows, filters,
                new Dictionary<string, Func<CommissionDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["subdealer"] = c => c.SubdealerName,
                    ["chassis"] = c => c.VehicleChassisNumber,
                    ["period"] = c => $"{c.Year}-{c.Month:D2}",
                    ["amount"] = c => c.CommissionAmount.ToString("N2"),
                    ["status"] = c => c.GetStatusDisplay(),
                    ["remarks"] = c => c.Notes
                },
                new Dictionary<string, Func<CommissionDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["submitted"] = c => c.CreatedDate,
                    ["approved"] = c => c.ApprovedDate,
                    ["rejected"] = c => c.RejectedDate
                });

        public static IEnumerable<CommissionRateDto> ApplyCommissionRates(IEnumerable<CommissionRateDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.CommissionRates, rows, filters,
                new Dictionary<string, Func<CommissionRateDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["model"] = r => r.ModelName,
                    ["amount"] = r => r.CommissionAmount.ToString("N2"),
                    ["status"] = r => r.IsActive() ? "Active" : "Inactive",
                    ["notes"] = r => r.Notes
                },
                new Dictionary<string, Func<CommissionRateDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["from"] = r => r.EffectiveFrom,
                    ["to"] = r => r.EffectiveTo,
                    ["created"] = r => r.CreatedDate
                });

        public static IEnumerable<DealershipDto> ApplyDealerships(IEnumerable<DealershipDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.Dealerships, rows, filters,
                new Dictionary<string, Func<DealershipDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["code"] = d => d.DealershipCode,
                    ["name"] = d => d.DealershipName,
                    ["location"] = d => d.Location,
                    ["phone"] = d => d.ContactPhone,
                    ["subdealers"] = d => d.SubDealerCount.ToString(),
                    ["status"] = d => d.IsActive ? "Active" : "Inactive"
                });

        public static IEnumerable<VehiclePriceHistoryDto> ApplyPrices(IEnumerable<VehiclePriceHistoryDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.Prices, rows, filters,
                new Dictionary<string, Func<VehiclePriceHistoryDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["model"] = p => p.ModelName,
                    ["color"] = p => p.ColorName,
                    ["period"] = p => $"{p.Month}/{p.Year}",
                    ["price"] = p => p.Price.ToString("N2"),
                    ["notes"] = p => p.Notes
                },
                new Dictionary<string, Func<VehiclePriceHistoryDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["from"] = p => p.EffectiveFrom,
                    ["to"] = p => p.EffectiveTo,
                    ["updated"] = p => p.ModifiedDate
                });

        public static IEnumerable<VehicleModelDto> ApplyVehicleModels(IEnumerable<VehicleModelDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.VehicleModels, rows, filters,
                new Dictionary<string, Func<VehicleModelDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = m => m.ModelName,
                    ["description"] = m => m.Description,
                    ["status"] = m => m.IsActive ? "Active" : "Inactive"
                },
                new Dictionary<string, Func<VehicleModelDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["created"] = m => m.CreatedDate
                });

        public static IEnumerable<VehicleColorDto> ApplyVehicleColors(IEnumerable<VehicleColorDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.VehicleColors, rows, filters,
                new Dictionary<string, Func<VehicleColorDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["color"] = c => c.ColorName,
                    ["name"] = c => c.ColorName,
                    ["hex"] = c => c.HexCode,
                    ["status"] = c => c.IsActive ? "Active" : "Inactive"
                },
                new Dictionary<string, Func<VehicleColorDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["created"] = c => c.CreatedDate
                });

        public static IEnumerable<StaffUserDto> ApplyStaffUsers(IEnumerable<StaffUserDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.StaffUsers, rows, filters,
                new Dictionary<string, Func<StaffUserDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = u => u.FullName,
                    ["role"] = u => u.RoleName,
                    ["dealership"] = u => u.DealershipName,
                    ["username"] = u => u.Username,
                    ["phone"] = u => u.PhoneNumber,
                    ["status"] = u => u.IsActive ? "Active" : "Inactive"
                },
                new Dictionary<string, Func<StaffUserDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["created"] = u => u.CreatedDate
                });

        public static IEnumerable<DocumentTypeMaster> ApplyDocumentTypes(IEnumerable<DocumentTypeMaster> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.DocumentTypes, rows, filters,
                new Dictionary<string, Func<DocumentTypeMaster, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["type"] = d => d.TypeName,
                    ["status"] = d => d.IsActive ? "Active" : "Inactive"
                });

        public static IEnumerable<FinanceNameMaster> ApplyFinanceNames(IEnumerable<FinanceNameMaster> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.FinanceNames, rows, filters,
                new Dictionary<string, Func<FinanceNameMaster, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = f => f.FinanceName,
                    ["status"] = f => f.IsActive ? "Active" : "Inactive"
                },
                new Dictionary<string, Func<FinanceNameMaster, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["created"] = f => f.CreatedDate
                });

        public static IEnumerable<PaymentType> ApplyPaymentTypes(IEnumerable<PaymentType> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.PaymentTypes, rows, filters,
                new Dictionary<string, Func<PaymentType, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["code"] = p => p.TypeCode,
                    ["name"] = p => p.TypeName,
                    ["finance"] = p => p.RequiresFinanceDetails ? "Yes" : "No",
                    ["sort"] = p => p.SortOrder.ToString(),
                    ["status"] = p => p.IsActive ? "Active" : "Inactive"
                },
                new Dictionary<string, Func<PaymentType, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["created"] = p => p.CreatedDate
                });

        public static IEnumerable<RtoLocationMaster> ApplyRtoLocations(
            IEnumerable<RtoLocationMaster> rows,
            IReadOnlyDictionary<string, string>? filters,
            IReadOnlyDictionary<int, string>? districtNames = null)
        {
            districtNames ??= new Dictionary<int, string>();
            return GridRowFilterApplier.Apply(GridScreenIds.RtoLocations, rows, filters,
                new Dictionary<string, Func<RtoLocationMaster, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["district"] = r => districtNames.TryGetValue(r.RtoDistrictId, out var name)
                        ? name
                        : $"#{r.RtoDistrictId}",
                    ["location"] = r => r.LocationName,
                    ["status"] = r => r.IsActive ? "Active" : "Inactive"
                });
        }

        public static IEnumerable<VehicleBookingGridRowDto> ApplyVehicleBookings(IEnumerable<VehicleBookingGridRowDto> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.VehicleBookings, rows, filters,
                new Dictionary<string, Func<VehicleBookingGridRowDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["id"] = r => r.Booking.VehicleBookingId.ToString(),
                    ["chassis"] = r => r.Chassis,
                    ["subdealer"] = r => r.Subdealer,
                    ["customer"] = r => r.Booking.CustomerName,
                    ["mobile"] = r => r.Booking.CustomerMobile,
                    ["status"] = r => r.StatusName,
                    ["invoiceDoc"] = r => string.IsNullOrWhiteSpace(r.Booking.InvoicePath) ? "No" : "Yes",
                    ["insuranceDoc"] = r => string.IsNullOrWhiteSpace(r.Booking.InsurancePath) ? "No" : "Yes"
                },
                new Dictionary<string, Func<VehicleBookingGridRowDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["submitted"] = r => r.Booking.SubmittedDate,
                    ["paperReceived"] = r => r.Booking.PaperReceivedDate,
                    ["invoiceDate"] = r => r.Booking.InvoiceDate,
                    ["insuranceDate"] = r => r.Booking.InsuranceDate,
                    ["registration"] = r => r.Booking.RegistrationDate
                });

        public static IEnumerable<ShowroomStockRowDto> ApplyShowroomStock(
            IEnumerable<ShowroomStockRowDto> rows,
            IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.ShowroomStock, rows, filters,
                new Dictionary<string, Func<ShowroomStockRowDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["location"] = r => r.DealershipLocation,
                    ["subdealer"] = r => r.SubdealerName,
                    ["chassis"] = r => r.ChassisNumber,
                    ["model"] = r => r.ModelName,
                    ["color"] = r => r.ColorName,
                    ["order"] = r => r.OrderNumber,
                    ["days"] = r => r.DaysInStock.ToString(),
                    ["price"] = r => r.CurrentPrice.ToString("N2")
                },
                new Dictionary<string, Func<ShowroomStockRowDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["allocated"] = r => r.AllocatedDate
                });

        public static IEnumerable<VehicleMasterDto> ApplyDealerStock(
            IEnumerable<VehicleMasterDto> rows,
            IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.DealerStock, rows, filters,
                new Dictionary<string, Func<VehicleMasterDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dealer"] = r => r.DealershipName,
                    ["chassis"] = r => r.ChassisNumber,
                    ["model"] = r => r.ModelName,
                    ["color"] = r => r.ColorName,
                    ["motor"] = r => r.MotorNo,
                    ["battery"] = r => r.BatteryNo,
                    ["status"] = r => r.IsAllocated ? "Allocated" : "Available",
                    ["invoiceNo"] = r => r.AmpereInvoiceNo,
                    ["allocatedTo"] = r => r.AllocatedToSubdealerName
                },
                new Dictionary<string, Func<VehicleMasterDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["received"] = r => r.ReceivedDate,
                    ["invoice"] = r => r.AmpereInvoiceDate
                });

        public static IEnumerable<StatusLookup> ApplyStatusLookups(IEnumerable<StatusLookup> rows, IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.StatusLookups, rows, filters,
                new Dictionary<string, Func<StatusLookup, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["category"] = s => s.Category,
                    ["value"] = s => s.StatusValue.ToString(),
                    ["code"] = s => s.StatusCode,
                    ["name"] = s => s.StatusName,
                    ["badge"] = s => s.BadgeClass,
                    ["sort"] = s => s.SortOrder.ToString(),
                    ["status"] = s => s.IsActive ? "Active" : "Inactive"
                });

        public static IEnumerable<Application.DTOs.WarrantyClaimDto> ApplyWarrantyClaims(
            IEnumerable<Application.DTOs.WarrantyClaimDto> rows,
            IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.WarrantyClaims, rows, filters,
                new Dictionary<string, Func<Application.DTOs.WarrantyClaimDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["claimNo"] = c => c.ClaimNumber,
                    ["type"] = c => c.ClaimType,
                    ["chassis"] = c => c.ChassisNo,
                    ["customer"] = c => c.CustomerName,
                    ["part"] = c => c.PartName,
                    ["subdealer"] = c => c.AccountName,
                    ["location"] = c => c.DealershipName,
                    ["status"] = c => c.StatusName
                },
                new Dictionary<string, Func<Application.DTOs.WarrantyClaimDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["submitted"] = c => c.SubmittedDate
                });

        public static IEnumerable<Application.DTOs.WarrantyClaimDto> ApplyMyWarrantyClaims(
            IEnumerable<Application.DTOs.WarrantyClaimDto> rows,
            IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.MyWarrantyClaims, rows, filters,
                new Dictionary<string, Func<Application.DTOs.WarrantyClaimDto, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["claimNo"] = c => c.ClaimNumber,
                    ["type"] = c => c.ClaimType,
                    ["chassis"] = c => c.ChassisNo,
                    ["part"] = c => c.PartName,
                    ["status"] = c => c.StatusName
                },
                new Dictionary<string, Func<Application.DTOs.WarrantyClaimDto, DateTime?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["submitted"] = c => c.SubmittedDate
                });

        public static IEnumerable<Domain.Entities.WarrantyPartMaster> ApplyWarrantyParts(
            IEnumerable<Domain.Entities.WarrantyPartMaster> rows,
            IReadOnlyDictionary<string, string>? filters)
            => GridRowFilterApplier.Apply(GridScreenIds.WarrantyParts, rows, filters,
                new Dictionary<string, Func<Domain.Entities.WarrantyPartMaster, string?>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = p => p.PartName,
                    ["code"] = p => p.PartCode,
                    ["status"] = p => p.IsActive ? "Active" : "Inactive"
                });
    }
}
